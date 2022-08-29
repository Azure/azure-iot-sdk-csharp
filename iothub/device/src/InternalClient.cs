// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.Azure.Devices.Client.Utilities;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Contains methods that a device can use to send messages to and receive messages from the service,
    /// respond to direct method invocations from the service, and send and receive twin property updates.
    /// </summary>
    internal class InternalClient : IDisposable
    {
        private readonly SemaphoreSlim _methodsSemaphore = new(1, 1);
        private readonly SemaphoreSlim _deviceReceiveMessageSemaphore = new(1, 1);
        private readonly SemaphoreSlim _moduleReceiveMessageSemaphore = new(1, 1);
        private readonly SemaphoreSlim _twinDesiredPropertySemaphore = new(1, 1);
        private readonly HttpTransportHandler _fileUploadHttpTransportHandler;
        private readonly IotHubClientOptions _clientOptions;

        // Connection status change information
        private volatile Action<ConnectionStatusInfo> _connectionStatusChangeHandler;

        internal ConnectionStatusInfo _connectionStatusInfo { get; private set; } = new ConnectionStatusInfo();

        // Method callback information
        private bool _isDeviceMethodEnabled;

        private volatile Tuple<Func<MethodRequest, object, Task<MethodResponse>>, object> _deviceDefaultMethodCallback;
        private readonly Dictionary<string, Tuple<Func<MethodRequest, object, Task<MethodResponse>>, object>> _deviceMethods = new();

        // Twin property update request callback information
        private bool _twinPatchSubscribedWithService;

        private object _twinPatchCallbackContext;
        internal Func<TwinCollection, object, Task> _desiredPropertyUpdateCallback;

        // Cloud-to-device message callback information
        private volatile Tuple<Func<Message, object, Task>, object> _deviceReceiveMessageCallback;

        // Cloud-to-module message callback information
        private volatile Tuple<Func<Message, object, Task<MessageResponse>>, object> _defaultEventCallback;

        private volatile Dictionary<string, Tuple<Func<Message, object, Task<MessageResponse>>, object>> _receiveEventEndpoints;

        // Diagnostic information

        // Count of messages sent by the device/ module. This is used for sending diagnostic information.
        private int _currentMessageCount;

        private int _diagnosticSamplingPercentage;

        protected internal InternalClient(
            IotHubConnectionCredentials iotHubConnectionCredentials,
            IotHubClientOptions iotHubClientOptions,
            IDeviceClientPipelineBuilder pipelineBuilder)
        {
            Argument.AssertNotNull(iotHubClientOptions, nameof(iotHubClientOptions));

            if (Logging.IsEnabled)
                Logging.Enter(this, iotHubClientOptions.TransportSettings, pipelineBuilder, nameof(InternalClient) + "_ctor");

            IotHubConnectionCredentials = iotHubConnectionCredentials;
            _clientOptions = iotHubClientOptions;

            if (!iotHubClientOptions.ModelId.IsNullOrWhiteSpace()
                && iotHubClientOptions.TransportSettings is IotHubClientHttpSettings)
            {
                throw new InvalidOperationException("Plug and Play is not supported over the HTTP transport.");
            }

            var pipelineContext = new PipelineContext
            {
                IotHubConnectionCredentials = iotHubConnectionCredentials,
                ProductInfo = iotHubClientOptions.ProductInfo,
                IotHubClientTransportSettings = iotHubClientOptions.TransportSettings,
                ModelId = iotHubClientOptions.ModelId,
                MethodCallback = OnMethodCalledAsync,
                DesiredPropertyUpdateCallback = OnDesiredStatePatchReceived,
                ConnectionStatusChangeHandler = OnConnectionStatusChanged,
                ModuleEventCallback = OnModuleEventMessageReceivedAsync,
                DeviceEventCallback = OnDeviceMessageReceivedAsync,
            };

            pipelineBuilder ??= BuildPipeline();
            IDelegatingHandler innerHandler = pipelineBuilder.Build(pipelineContext);

            if (Logging.IsEnabled)
                Logging.Associate(this, innerHandler, nameof(InternalClient));

            InnerHandler = innerHandler;

            if (Logging.IsEnabled)
                Logging.Associate(this, iotHubClientOptions.TransportSettings, nameof(InternalClient));

            _fileUploadHttpTransportHandler = new HttpTransportHandler(pipelineContext, iotHubClientOptions.FileUploadTransportSettings);

            if (Logging.IsEnabled)
                Logging.Exit(this, iotHubClientOptions.TransportSettings, pipelineBuilder, nameof(InternalClient) + "_ctor");
        }

        private static IDeviceClientPipelineBuilder BuildPipeline()
        {
            var transporthandlerFactory = new TransportHandlerFactory();
            IDeviceClientPipelineBuilder pipelineBuilder = new DeviceClientPipelineBuilder()
                .With((ctx, innerHandler) => new RetryDelegatingHandler(ctx, innerHandler))
                .With((ctx, innerHandler) => new ErrorDelegatingHandler(ctx, innerHandler))
                .With((ctx, innerHandler) => new TransportDelegatingHandler(ctx, innerHandler))
                .With((ctx, innerHandler) => transporthandlerFactory.Create(ctx));

            return pipelineBuilder;
        }

        /// <summary>
        /// Diagnostic sampling percentage value, [0-100];
        /// A value of 0 means no message will carry on diagnostics info.
        /// </summary>
        public int DiagnosticSamplingPercentage
        {
            get => _diagnosticSamplingPercentage;
            set
            {
                if (value > 100 || value < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(DiagnosticSamplingPercentage),
                        value,
                        "The range of diagnostic sampling percentage should between [0,100].");
                }

                if (IsE2eDiagnosticSupportedProtocol())
                {
                    _diagnosticSamplingPercentage = value;
                }
            }
        }

        internal X509Certificate2 Certificate { get; set; }

        internal IDelegatingHandler InnerHandler { get; set; }

        internal IotHubConnectionCredentials IotHubConnectionCredentials { get; private set; }

        /// <summary>
        /// Sets a new delegate for the connection status changed callback. If a delegate is already associated,
        /// it will be replaced with the new delegate.
        /// </summary>
        /// <param name="statusChangeHandler">The name of the method to associate with the delegate.</param>
        public void SetConnectionStatusChangeHandler(Action<ConnectionStatusInfo> statusChangeHandler)
        {
            if (Logging.IsEnabled)
                Logging.Info(this, statusChangeHandler, nameof(SetConnectionStatusChangeHandler));

            _connectionStatusChangeHandler = statusChangeHandler;
        }

        /// <summary>
        /// Set a callback that will be called whenever the client receives a status update
        /// (desired or reported) from the service.
        /// Set callback value to null to clear.
        /// </summary>
        /// <remarks>
        /// This has the side-effect of subscribing to the PATCH topic on the service.
        /// </remarks>
        /// <param name="callback">Callback to call after the status update has been received and applied</param>
        /// <param name="userContext">Context object that will be passed into callback</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        public async Task SetDesiredPropertyUpdateCallbackAsync(
            Func<TwinCollection, object, Task> callback,
            object userContext,
            CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, callback, userContext, nameof(SetDesiredPropertyUpdateCallbackAsync));

            cancellationToken.ThrowIfCancellationRequested();

            // Wait to acquire the _twinSemaphore. This ensures that concurrently invoked SetDesiredPropertyUpdateCallbackAsync calls are invoked in a thread-safe manner.
            await _twinDesiredPropertySemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (callback != null && !_twinPatchSubscribedWithService)
                {
                    await InnerHandler.EnableTwinPatchAsync(cancellationToken).ConfigureAwait(false);
                    _twinPatchSubscribedWithService = true;
                }
                else if (callback == null && _twinPatchSubscribedWithService)
                {
                    await InnerHandler.DisableTwinPatchAsync(cancellationToken).ConfigureAwait(false);
                }

                _desiredPropertyUpdateCallback = callback;
                _twinPatchCallbackContext = userContext;
            }
            catch (IotHubClientException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
            finally
            {
                _twinDesiredPropertySemaphore.Release();

                if (Logging.IsEnabled)
                    Logging.Exit(this, callback, userContext, nameof(SetDesiredPropertyUpdateCallbackAsync));
            }
        }

        /// <summary>
        /// Sets a new delegate for the named method. If a delegate is already associated with
        /// the named method, it will be replaced with the new delegate.
        /// A method handler can be unset by passing a null MethodCallback.
        /// </summary>
        /// <param name="methodName">The name of the method to associate with the delegate.</param>
        /// <param name="methodHandler">The delegate to be used when a method with the given name is called by the cloud service.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice
        /// of cancellation.</param>
        public async Task SetMethodHandlerAsync(
            string methodName,
            Func<MethodRequest, object, Task<MethodResponse>> methodHandler,
            object userContext,
            CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, methodName, methodHandler, userContext, nameof(SetMethodHandlerAsync));

            Argument.AssertNotNullOrWhiteSpace(methodName, nameof(methodName));
            cancellationToken.ThrowIfCancellationRequested();

            await _methodsSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (methodHandler != null)
                {
                    await HandleMethodEnableAsync(cancellationToken).ConfigureAwait(false);
                    _deviceMethods[methodName] = new Tuple<Func<MethodRequest, object, Task<MethodResponse>>, object>(methodHandler, userContext);
                }
                else
                {
                    _deviceMethods.Remove(methodName);
                    await HandleMethodDisableAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (IotHubClientException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
            finally
            {
                _methodsSemaphore.Release();

                if (Logging.IsEnabled)
                    Logging.Exit(this, methodName, methodHandler, userContext, nameof(SetMethodHandlerAsync));
            }
        }

        /// <summary>
        /// Sets a new delegate that is called for a method that doesn't have a delegate registered for its name.
        /// If a default delegate is already registered it will replace with the new delegate.
        /// A method handler can be unset by passing a null MethodCallback.
        /// </summary>
        /// <param name="methodHandler">The delegate to be used when a method is called by the cloud service and there is no
        /// delegate registered for that method name.</param>
        /// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice
        /// of cancellation.</param>
        public async Task SetMethodDefaultHandlerAsync(
            Func<MethodRequest, object, Task<MethodResponse>> methodHandler,
            object userContext,
            CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, methodHandler, userContext, nameof(SetMethodDefaultHandlerAsync));

            cancellationToken.ThrowIfCancellationRequested();

            await _methodsSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (methodHandler != null)
                {
                    await HandleMethodEnableAsync(cancellationToken).ConfigureAwait(false);

                    _deviceDefaultMethodCallback = new Tuple<Func<MethodRequest, object, Task<MethodResponse>>, object>(methodHandler, userContext);
                }
                else
                {
                    _deviceDefaultMethodCallback = null;

                    await HandleMethodDisableAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (IotHubClientException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
            finally
            {
                _methodsSemaphore.Release();

                if (Logging.IsEnabled)
                    Logging.Exit(this, methodHandler, userContext, nameof(SetMethodDefaultHandlerAsync));
            }
        }

        /// <summary>
        /// Sets the retry policy used in the operation retries.
        /// </summary>
        /// <param name="retryPolicy">
        /// The retry policy. The default is <c>new ExponentialBackoff(int.MaxValue, TimeSpan.FromMilliseconds(100),
        /// TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(100));</c>
        /// </param>
        public void SetRetryPolicy(IRetryPolicy retryPolicy)
        {
            RetryDelegatingHandler retryDelegatingHandler = GetDelegateHandler<RetryDelegatingHandler>();
            if (retryDelegatingHandler == null)
            {
                throw new NotSupportedException();
            }

            retryDelegatingHandler.SetRetryPolicy(retryPolicy);
        }

        /// <summary>
        /// Explicitly open the client instance.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        public async Task OpenAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await InnerHandler.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (IotHubClientException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        /// <summary>
        /// Close the client instance
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        public async Task CloseAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await InnerHandler.CloseAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (IotHubClientException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        /// <summary>
        /// Sends an event to device hub
        /// </summary>
        /// <returns>The message containing the event</returns>
        public async Task SendEventAsync(Message message, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(message, nameof(message));
            cancellationToken.ThrowIfCancellationRequested();

            if (_clientOptions?.SdkAssignsMessageId == SdkAssignsMessageId.WhenUnset && message.MessageId == null)
            {
                message.MessageId = Guid.NewGuid().ToString();
            }

            IotHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, _diagnosticSamplingPercentage, ref _currentMessageCount);
            // The asynchronous operation shall retry until time specified in OperationTimeoutInMilliseconds property
            // expire or unrecoverable error(authentication or quota exceed) occurs.
            try
            {
                await InnerHandler.SendEventAsync(message, cancellationToken).ConfigureAwait(false);
            }
            catch (IotHubClientException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        /// <summary>
        /// Sends a batch of events to device hub
        /// </summary>
        /// <returns>The task containing the event</returns>
        public async Task SendEventBatchAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrEmpty(messages, nameof(messages));
            cancellationToken.ThrowIfCancellationRequested();

            if (_clientOptions?.SdkAssignsMessageId == SdkAssignsMessageId.WhenUnset)
            {
                foreach (Message message in messages)
                {
                    message.MessageId ??= Guid.NewGuid().ToString();
                }
            }

            // The asynchronous operation shall retry until time specified in OperationTimeoutInMilliseconds property
            // expire or unrecoverable error (authentication or quota exceed) occurs.
            try
            {
                await InnerHandler.SendEventAsync(messages, cancellationToken).ConfigureAwait(false);
            }
            catch (IotHubClientException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        /// <summary>
        /// Retrieve the device twin properties for the current device.
        /// For the complete device twin object, use Microsoft.Azure.Devices.RegistryManager.GetTwinAsync(string deviceId).
        /// </summary>
        /// <returns>The device twin object for the current device</returns>
        public async Task<Twin> GetTwinAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // `GetTwinAsync` shall call `SendTwinGetAsync` on the transport to get the twin status.
            try
            {
                return await InnerHandler.SendTwinGetAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (IotHubClientException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        /// <summary>
        /// Push reported property changes up to the service.
        /// </summary>
        /// <param name="reportedProperties">Reported properties to push</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        public async Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(reportedProperties, nameof(reportedProperties));
            cancellationToken.ThrowIfCancellationRequested();

            // `UpdateReportedPropertiesAsync` shall call `SendTwinPatchAsync` on the transport to update the reported properties.
            try
            {
                await InnerHandler.SendTwinPatchAsync(reportedProperties, cancellationToken).ConfigureAwait(false);
            }
            catch (IotHubClientException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        /// <summary>
        /// Deletes a received message from the device queue
        /// </summary>
        /// <param name="lockToken"></param>
        /// <param name="cancellationToken">A token to cancel the operation. </param>
        /// <returns>The lock identifier for the previously received message</returns>
        public async Task CompleteMessageAsync(string lockToken, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(lockToken, nameof(lockToken));
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await InnerHandler.CompleteMessageAsync(lockToken, cancellationToken).ConfigureAwait(false);
            }
            catch (IotHubClientException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        /// <summary>
        /// Deletes a received message from the device queue
        /// </summary>
        /// <param name="message">The message to complete</param>
        /// <param name="cancellationToken">A token to cancel the operation. </param>
        /// <returns>The previously received message</returns>
        public async Task CompleteMessageAsync(Message message, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(message, nameof(message));
            cancellationToken.ThrowIfCancellationRequested();

            // The asynchronous operation shall retry until time specified in OperationTimeoutInMilliseconds
            // property expire or unrecoverable error(authentication, quota exceed) occurs.
            try
            {
                await CompleteMessageAsync(message.LockToken, cancellationToken).ConfigureAwait(false);
            }
            catch (IotHubClientException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        /// <summary>
        /// Puts a received message back onto the device queue
        /// </summary>
        /// <returns>The previously received message</returns>
        public async Task AbandonMessageAsync(string lockToken, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(lockToken, nameof(lockToken));
            cancellationToken.ThrowIfCancellationRequested();

            // The asynchronous operation shall retry until time specified in OperationTimeoutInMilliseconds property
            // expire or unrecoverable error(authentication, quota exceed) occurs.
            try
            {
                await InnerHandler.AbandonMessageAsync(lockToken, cancellationToken).ConfigureAwait(false);
            }
            catch (IotHubClientException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        /// <summary>
        /// Puts a received message back onto the device queue
        /// </summary>
        /// <returns>The lock identifier for the previously received message</returns>
        public async Task AbandonMessageAsync(Message message, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(message, nameof(message));
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await AbandonMessageAsync(message.LockToken, cancellationToken).ConfigureAwait(false);
            }
            catch (IotHubClientException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        /// <summary>
        /// Deletes a received message from the device queue and indicates to the server that the message could not be processed.
        /// </summary>
        /// <returns>The previously received message</returns>
        public async Task RejectMessageAsync(string lockToken, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(lockToken, nameof(lockToken));
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await InnerHandler.RejectMessageAsync(lockToken, cancellationToken).ConfigureAwait(false);
            }
            catch (IotHubClientException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        /// <summary>
        /// Deletes a received message from the device queue and indicates to the server that the message could not be processed.
        /// </summary>
        /// <returns>The lock identifier for the previously received message</returns>
        public async Task RejectMessageAsync(Message message, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(message, nameof(message));
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await RejectMessageAsync(message.LockToken, cancellationToken).ConfigureAwait(false);
            }
            catch (IotHubClientException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        /// <summary>
        /// The delegate for handling direct methods received from service.
        /// </summary>
        internal async Task OnMethodCalledAsync(MethodRequestInternal methodRequestInternal)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, methodRequestInternal?.Name, methodRequestInternal, nameof(OnMethodCalledAsync));

            if (methodRequestInternal == null)
            {
                return;
            }

            Tuple<Func<MethodRequest, object, Task<MethodResponse>>, object> callbackContextPair = null;
            MethodResponseInternal methodResponseInternal = null;

            await _methodsSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!_deviceMethods.TryGetValue(methodRequestInternal.Name, out callbackContextPair))
                {
                    callbackContextPair = _deviceDefaultMethodCallback;
                }
            }
            catch (Exception ex) when (!Fx.IsFatal(ex))
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, ex, nameof(OnMethodCalledAsync));

                methodResponseInternal = new MethodResponseInternal(methodRequestInternal.RequestId, (int)MethodResponseStatusCode.BadRequest);

                await SendMethodResponseAsync(methodResponseInternal).ConfigureAwait(false);

                if (Logging.IsEnabled)
                    Logging.Error(this, ex, nameof(OnMethodCalledAsync));

                return;
            }
            finally
            {
                _methodsSemaphore.Release();
            }

            if (callbackContextPair == null)
            {
                methodResponseInternal = new MethodResponseInternal(
                    methodRequestInternal.RequestId,
                    (int)MethodResponseStatusCode.MethodNotImplemented);
            }
            else
            {
                try
                {
                    Func<MethodRequest, object, Task<MethodResponse>> userSuppliedCallback = callbackContextPair.Item1;
                    object userSuppliedContext = callbackContextPair.Item2;

                    MethodResponse rv = await userSuppliedCallback
                        .Invoke(new MethodRequest(methodRequestInternal.Name, methodRequestInternal.Payload), userSuppliedContext)
                        .ConfigureAwait(false);

                    methodResponseInternal = new MethodResponseInternal(methodRequestInternal.RequestId, rv.Status, rv.Result);
                }
                catch (Exception ex)
                {
                    if (Logging.IsEnabled)
                        Logging.Error(this, ex, nameof(OnMethodCalledAsync));

                    methodResponseInternal = new MethodResponseInternal(methodRequestInternal.RequestId, (int)MethodResponseStatusCode.UserCodeException);
                }
            }

            await SendMethodResponseAsync(methodResponseInternal).ConfigureAwait(false);

            if (Logging.IsEnabled)
                Logging.Exit(this, methodRequestInternal.Name, methodRequestInternal, nameof(OnMethodCalledAsync));
        }

        internal void OnDesiredStatePatchReceived(TwinCollection patch)
        {
            if (_desiredPropertyUpdateCallback == null)
            {
                return;
            }

            if (Logging.IsEnabled)
                Logging.Info(this, patch.ToJson(), nameof(OnDesiredStatePatchReceived));

            _ = _desiredPropertyUpdateCallback.Invoke(patch, _twinPatchCallbackContext);
        }

        private async Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken = default)
        {
            try
            {
                await InnerHandler.SendMethodResponseAsync(methodResponse, cancellationToken).ConfigureAwait(false);
            }
            catch (IotHubClientException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        private async Task HandleMethodEnableAsync(CancellationToken cancellationToken = default)
        {
            // If currently enabled, then skip
            if (_isDeviceMethodEnabled)
            {
                return;
            }

            await InnerHandler.EnableMethodsAsync(cancellationToken).ConfigureAwait(false);
            _isDeviceMethodEnabled = true;
        }

        #region Device Specific API

        /// <summary>
        /// Receive a message from the device queue using the default timeout.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive
        /// notice of cancellation.</param>
        /// <returns>The receive message or null if there was no message until the default timeout</returns>
        public async Task<Message> ReceiveMessageAsync(CancellationToken cancellationToken = default)
        {
            // The asynchronous operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or
            // unrecoverable (authentication, quota exceed) error occurs.
            try
            {
                return await InnerHandler.ReceiveMessageAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (IotHubClientException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        /// <summary>
        /// Sets a new delegate for receiving a message from the device queue using the default timeout.
        /// If a delegate is already registered it will be replaced with the new delegate.
        /// If a null delegate is passed, it will disable triggering any callback on receiving messages from the service.
        /// </summary>
        /// <param name="messageHandler">The delegate to be used when a could to device message is received by the client.</param>
        /// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The task containing the event</returns>
        public async Task SetReceiveMessageHandlerAsync(
            Func<Message, object, Task> messageHandler,
            object userContext,
            CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, messageHandler, userContext, nameof(SetReceiveMessageHandlerAsync));

            cancellationToken.ThrowIfCancellationRequested();

            // Wait to acquire the _deviceReceiveMessageSemaphore. This ensures that concurrently invoked
            // SetReceiveMessageHandlerAsync calls
            // are invoked in a thread-safe manner.
            await _deviceReceiveMessageSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // If a ReceiveMessageCallback is already set on the DeviceClient, calling SetReceiveMessageHandlerAsync
                // again will cause the delegate to be overwritten.
                if (messageHandler != null)
                {
                    // If this is the first time the delegate is being registered, then the telemetry downlink will be enabled.
                    await EnableReceiveMessageAsync(cancellationToken).ConfigureAwait(false);
                    _deviceReceiveMessageCallback = new Tuple<Func<Message, object, Task>, object>(messageHandler, userContext);
                }
                else
                {
                    // If a null delegate is passed, it will disable the callback triggered on receiving messages from the service.
                    _deviceReceiveMessageCallback = null;
                    await DisableReceiveMessageAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (IotHubClientException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
            finally
            {
                _deviceReceiveMessageSemaphore.Release();

                if (_deviceReceiveMessageCallback != null)
                {
                    // Any previously received C2D messages will also need to be delivered.
                    await InnerHandler.EnsurePendingMessagesAreDeliveredAsync(cancellationToken).ConfigureAwait(false);
                }

                if (Logging.IsEnabled)
                    Logging.Exit(this, messageHandler, userContext, nameof(SetReceiveMessageHandlerAsync));
            }
        }

        public Task<FileUploadSasUriResponse> GetFileUploadSasUriAsync(
            FileUploadSasUriRequest request,
            CancellationToken cancellationToken = default)
        {
            return _fileUploadHttpTransportHandler.GetFileUploadSasUriAsync(request, cancellationToken);
        }

        public Task CompleteFileUploadAsync(
            FileUploadCompletionNotification notification,
            CancellationToken cancellationToken = default)
        {
            return _fileUploadHttpTransportHandler.CompleteFileUploadAsync(notification, cancellationToken);
        }

        // Enable telemetry downlink for devices
        private Task EnableReceiveMessageAsync(CancellationToken cancellationToken = default)
        {
            // The telemetry downlink needs to be enabled only for the first time that the _receiveMessageCallback delegate is set.
            return _deviceReceiveMessageCallback == null
                ? InnerHandler.EnableReceiveMessageAsync(cancellationToken)
                : Task.CompletedTask;
        }

        // Disable telemetry downlink for devices
        private Task DisableReceiveMessageAsync(CancellationToken cancellationToken = default)
        {
            // The telemetry downlink should be disabled only after _receiveMessageCallback delegate has been removed.
            return _deviceReceiveMessageCallback == null
                ? InnerHandler.DisableReceiveMessageAsync(cancellationToken)
                : Task.CompletedTask;
        }

        /// <summary>
        /// The delegate for handling c2d messages received
        /// </summary>
        /// <param name="message">The message received</param>
        private async Task OnDeviceMessageReceivedAsync(Message message)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, message, nameof(OnDeviceMessageReceivedAsync));

            if (message == null)
            {
                return;
            }

            // Grab this semaphore so that there is no chance that the _deviceReceiveMessageCallback instance is set in between the read of the
            // item1 and the read of the item2
            await _deviceReceiveMessageSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                Func<Message, object, Task> callback = _deviceReceiveMessageCallback?.Item1;
                object callbackContext = _deviceReceiveMessageCallback?.Item2;

                if (callback != null)
                {
                    _ = callback.Invoke(message, callbackContext);
                }
            }
            finally
            {
                _deviceReceiveMessageSemaphore.Release();
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, message, nameof(OnDeviceMessageReceivedAsync));
        }

        private async Task HandleMethodDisableAsync(CancellationToken cancellationToken = default)
        {
            // Don't disable if it is already disabled or if there are registered device methods
            if (!_isDeviceMethodEnabled
                || _deviceDefaultMethodCallback != null
                || _deviceMethods.Any())
            {
                return;
            }

            await InnerHandler.DisableMethodsAsync(cancellationToken).ConfigureAwait(false);
            _isDeviceMethodEnabled = false;
        }

        #endregion Device Specific API

        #region Module Specific API

        /// <summary>
        /// Sends an event (message) to the hub
        /// </summary>
        /// <param name="outputName">The output target for sending the given message</param>
        /// <param name="message">The message to send</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>The message containing the event</returns>
        public async Task SendEventAsync(string outputName, Message message, CancellationToken cancellationToken = default)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, outputName, message, nameof(SendEventAsync));

                ValidateModuleTransportHandler("SendEventAsync for a named output");

                Argument.AssertNotNullOrWhiteSpace(outputName, nameof(outputName));
                Argument.AssertNotNull(message, nameof(message));

                message.SystemProperties.Add(MessageSystemPropertyNames.OutputName, outputName);

                await InnerHandler.SendEventAsync(message, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, outputName, message, nameof(SendEventAsync));
            }
        }

        /// <summary>
        /// Sends a batch of events to device hub
        /// </summary>
        /// <param name="outputName">The output target for sending the given message</param>
        /// <param name="messages">A list of one or more messages to send</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The task containing the event</returns>
        public async Task SendEventBatchAsync(string outputName, IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, outputName, messages, nameof(SendEventBatchAsync));

                ValidateModuleTransportHandler("SendEventBatchAsync for a named output");

                Argument.AssertNotNullOrWhiteSpace(outputName, nameof(outputName));
                var messagesList = messages?.ToList();
                Argument.AssertNotNullOrEmpty(messagesList, nameof(messages));

                messagesList.ForEach(m => m.SystemProperties.Add(MessageSystemPropertyNames.OutputName, outputName));

                await InnerHandler.SendEventAsync(messagesList, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, outputName, messages, nameof(SendEventBatchAsync));
            }
        }

        /// <summary>
        /// Sets a new delegate for the particular input. If a delegate is already associated with
        /// the input, it will be replaced with the new delegate.
        /// Set messageHandler value to null to clear.
        /// </summary>
        /// <param name="inputName">The name of the input to associate with the delegate.</param>
        /// <param name="messageHandler">The delegate to be used when a message is sent to the particular inputName.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// <param name="isAnEdgeModule">Parameter to correctly select a device module path. This is set by the
        /// <see cref="IotHubModuleClient"/> when a <see cref="Edge.EdgeModuleClientFactory"/> creates the module.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The task containing the event</returns>
        public async Task SetInputMessageHandlerAsync(
            string inputName,
            Func<Message, object, Task<MessageResponse>> messageHandler,
            object userContext,
            bool isAnEdgeModule,
            CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, inputName, messageHandler, userContext, nameof(SetInputMessageHandlerAsync));

            ValidateModuleTransportHandler("SetInputMessageHandlerAsync for a named output");

            cancellationToken.ThrowIfCancellationRequested();
            await _moduleReceiveMessageSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (messageHandler != null)
                {
                    // When using a device module we need to enable the 'deviceBound' message link
                    await EnableEventReceiveAsync(isAnEdgeModule, cancellationToken).ConfigureAwait(false);

                    if (_receiveEventEndpoints == null)
                    {
                        _receiveEventEndpoints = new Dictionary<string, Tuple<Func<Message, object, Task<MessageResponse>>, object>>();
                    }

                    _receiveEventEndpoints[inputName] = new Tuple<Func<Message, object, Task<MessageResponse>>, object>(messageHandler, userContext);
                }
                else
                {
                    if (_receiveEventEndpoints != null)
                    {
                        _receiveEventEndpoints.Remove(inputName);
                        if (_receiveEventEndpoints.Count == 0)
                        {
                            _receiveEventEndpoints = null;
                        }
                    }

                    await DisableEventReceiveAsync(isAnEdgeModule, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                _moduleReceiveMessageSemaphore.Release();

                if (Logging.IsEnabled)
                    Logging.Exit(this, inputName, messageHandler, userContext, nameof(SetInputMessageHandlerAsync));
            }
        }

        /// <summary>
        /// Sets a new default delegate which applies to all endpoints. If a delegate is already associated with
        /// the input, it will be called, else the default delegate will be called. If a default delegate was set previously,
        /// it will be overwritten.
        /// Set messageHandler value to null to clear.
        /// </summary>
        /// <param name="messageHandler">The delegate to be called when a message is sent to any input.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// <param name="isAnEdgeModule">Parameter to correctly select a device module path. This is set by the
        /// <see cref="IotHubModuleClient"/> when a <see cref="Edge.EdgeModuleClientFactory"/> creates the module.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The task containing the event</returns>
        public async Task SetMessageHandlerAsync(
            Func<Message, object, Task<MessageResponse>> messageHandler,
            object userContext,
            bool isAnEdgeModule,
            CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, messageHandler, userContext, nameof(SetMessageHandlerAsync));

            cancellationToken.ThrowIfCancellationRequested();
            await _moduleReceiveMessageSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (messageHandler != null)
                {
                    await EnableEventReceiveAsync(isAnEdgeModule, cancellationToken).ConfigureAwait(false);
                    _defaultEventCallback = new Tuple<Func<Message, object, Task<MessageResponse>>, object>(messageHandler, userContext);
                }
                else
                {
                    _defaultEventCallback = null;
                    await DisableEventReceiveAsync(isAnEdgeModule, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                _moduleReceiveMessageSemaphore.Release();

                if (Logging.IsEnabled)
                    Logging.Exit(this, messageHandler, userContext, nameof(SetMessageHandlerAsync));
            }
        }

        /// <summary>
        /// The delegate for handling event messages received
        /// </summary>
        /// <param name="input">The input on which a message is received</param>
        /// <param name="message">The message received</param>
        internal async Task OnModuleEventMessageReceivedAsync(string input, Message message)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, input, message, nameof(OnModuleEventMessageReceivedAsync));

            if (message == null)
            {
                return;
            }

            try
            {
                Tuple<Func<Message, object, Task<MessageResponse>>, object> callback = null;
                await _moduleReceiveMessageSemaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (_receiveEventEndpoints == null
                        || string.IsNullOrWhiteSpace(input)
                        || !_receiveEventEndpoints.TryGetValue(input, out callback))
                    {
                        callback = _defaultEventCallback;
                    }
                }
                finally
                {
                    _moduleReceiveMessageSemaphore.Release();
                }

                MessageResponse response = MessageResponse.Completed;
                if (callback?.Item1 != null)
                {
                    Func<Message, object, Task<MessageResponse>> userSuppliedCallback = callback.Item1;
                    object userContext = callback.Item2;

                    response = await userSuppliedCallback
                        .Invoke(message, userContext)
                        .ConfigureAwait(false);
                }

                if (Logging.IsEnabled)
                    Logging.Info(this, $"{nameof(MessageResponse)} = {response}", nameof(OnModuleEventMessageReceivedAsync));

                try
                {
                    switch (response)
                    {
                        case MessageResponse.Completed:
                            await CompleteMessageAsync(message).ConfigureAwait(false);
                            break;

                        case MessageResponse.Abandoned:
                            await AbandonMessageAsync(message).ConfigureAwait(false);
                            break;

                        default:
                            break;
                    }
                }
                catch (Exception ex) when (Logging.IsEnabled)
                {
                    Logging.Error(this, ex, nameof(OnModuleEventMessageReceivedAsync));
                    throw;
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, input, message, nameof(OnModuleEventMessageReceivedAsync));
            }
        }

        // Enable telemetry downlink for modules
        private Task EnableEventReceiveAsync(bool isAnEdgeModule, CancellationToken cancellationToken = default)
        {
            // The telemetry downlink needs to be enabled only for the first time that the _defaultEventCallback delegate is set.
            return _receiveEventEndpoints == null && _defaultEventCallback == null
                ? InnerHandler.EnableEventReceiveAsync(isAnEdgeModule, cancellationToken)
                : Task.CompletedTask;
        }

        // Disable telemetry downlink for modules
        private Task DisableEventReceiveAsync(bool isAnEdgeModule, CancellationToken cancellationToken = default)
        {
            // The telemetry downlink should be disabled only after _defaultEventCallback delegate has been removed.
            return _receiveEventEndpoints == null && _defaultEventCallback == null
                ? InnerHandler.DisableEventReceiveAsync(isAnEdgeModule, cancellationToken)
                : Task.CompletedTask;
        }

        private void ValidateModuleTransportHandler(string apiName)
        {
            if (IotHubConnectionCredentials.ModuleId.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException("{0} is available for Modules only.".FormatInvariant(apiName));
            }
        }

        #endregion Module Specific API

        public void Dispose()
        {
            InnerHandler?.Dispose();
            _methodsSemaphore?.Dispose();
            _moduleReceiveMessageSemaphore?.Dispose();
            _fileUploadHttpTransportHandler?.Dispose();
            _deviceReceiveMessageSemaphore?.Dispose();
            _twinDesiredPropertySemaphore?.Dispose();
        }

        /// <summary>
        /// The delegate for handling disrupted connection/links in the transport layer.
        /// </summary>
        internal void OnConnectionStatusChanged(ConnectionStatusInfo connectionStatusInfo)
        {
            var status = connectionStatusInfo.Status;
            var reason = connectionStatusInfo.ChangeReason;

            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, status, reason, nameof(OnConnectionStatusChanged));

                if (_connectionStatusInfo.Status != status
                    || _connectionStatusInfo.ChangeReason != reason)
                {
                    _connectionStatusInfo = new ConnectionStatusInfo(status, reason);
                    _connectionStatusChangeHandler?.Invoke(_connectionStatusInfo);
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, status, reason, nameof(OnConnectionStatusChanged));
            }
        }

        private T GetDelegateHandler<T>() where T : DefaultDelegatingHandler
        {
            var handler = InnerHandler as DefaultDelegatingHandler;
            bool isFound = false;

            while (!isFound || handler == null)
            {
                if (handler is T)
                {
                    isFound = true;
                }
                else
                {
                    handler = handler.NextHandler as DefaultDelegatingHandler;
                }
            }

            return !isFound ? default : (T)handler;
        }

        internal bool IsE2eDiagnosticSupportedProtocol()
        {
            if (_clientOptions.TransportSettings is IotHubClientAmqpSettings
                || _clientOptions.TransportSettings is IotHubClientMqttSettings)
            {
                return true;
            }

            throw new NotSupportedException($"The {_clientOptions.TransportSettings.GetType().Name} transport doesn't support E2E diagnostic.");
        }
    }
}
