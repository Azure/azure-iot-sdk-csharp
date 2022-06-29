// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using Microsoft.Azure.Devices.Client.Exceptions;
using System.ComponentModel;
using System.Text;
using Newtonsoft.Json;

#if NET451

using System.Net;

#else

using System.Net.Http;

#endif

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Delegate for connection status changed.
    /// </summary>
    /// <remarks>
    /// This can be set for both <see cref="DeviceClient"/> and <see cref="ModuleClient"/>.
    /// </remarks>
    /// <param name="status">The updated connection status</param>
    /// <param name="reason">The reason for the connection status change</param>
    public delegate void ConnectionStatusChangesHandler(ConnectionStatus status, ConnectionStatusChangeReason reason);

    /// <summary>
    /// Delegate for method call. This will be called every time we receive a method call that was registered.
    /// </summary>
    /// <remarks>
    /// This can be set for both <see cref="DeviceClient"/> and <see cref="ModuleClient"/>.
    /// </remarks>
    /// <param name="methodRequest">Class with details about method.</param>
    /// <param name="userContext">Context object passed in when the callback was registered.</param>
    /// <returns>MethodResponse</returns>
    public delegate Task<MethodResponse> MethodCallback(MethodRequest methodRequest, object userContext);

    /// <summary>
    /// Delegate for desired property update callbacks. This will be called every time we receive a patch from the service.
    /// </summary>
    /// <remarks>
    /// This can be set for both <see cref="DeviceClient"/> and <see cref="ModuleClient"/>.
    /// </remarks>
    /// <param name="desiredProperties">Properties that were contained in the update that was received from the service</param>
    /// <param name="userContext">Context object passed in when the callback was registered</param>
    public delegate Task DesiredPropertyUpdateCallback(TwinCollection desiredProperties, object userContext);

    /// <summary>
    /// Delegate that gets called when a message is received on a <see cref="DeviceClient"/>.
    /// </summary>
    /// <remarks>
    /// This is set using <see cref="DeviceClient.SetReceiveMessageHandlerAsync(ReceiveMessageCallback, object, CancellationToken)"/>.
    /// </remarks>
    /// <param name="message">The received message.</param>
    /// <param name="userContext">Context object passed in when the callback was registered.</param>
    public delegate Task ReceiveMessageCallback(Message message, object userContext);

    /// <summary>
    /// Delegate that gets called when a message is received on a <see cref="ModuleClient"/>.
    /// </summary>
    /// <remarks>
    /// This is set using <see cref="ModuleClient.SetInputMessageHandlerAsync(string, MessageHandler, object, CancellationToken)"/>
    /// and <see cref="ModuleClient.SetMessageHandlerAsync(MessageHandler, object, CancellationToken)"/>.
    /// </remarks>
    /// <param name="message">The received message.</param>
    /// <param name="userContext">Context object passed in when the callback was registered.</param>
    /// <returns>MessageResponse</returns>
    public delegate Task<MessageResponse> MessageHandler(Message message, object userContext);

    /// <summary>
    /// Status of handling a message.
    /// </summary>
    public enum MessageResponse
    {
        /// <summary>
        /// No acknowledgment of receipt will be sent.
        /// </summary>
        None,

        /// <summary>
        /// Event will be completed, removing it from the queue.
        /// </summary>
        Completed,

        /// <summary>
        /// Event will be abandoned.
        /// </summary>
        Abandoned,
    };

    /// <summary>
    /// Contains methods that a device can use to send messages to and receive messages from the service,
    /// respond to direct method invocations from the service, and send and receive twin property updates.
    /// </summary>
    internal partial class InternalClient : IDisposable
    {
        private readonly SemaphoreSlim _methodsSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _deviceReceiveMessageSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _moduleReceiveMessageSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _twinDesiredPropertySemaphore = new SemaphoreSlim(1, 1);
        private readonly ProductInfo _productInfo = new ProductInfo();
        private readonly HttpTransportHandler _fileUploadHttpTransportHandler;
        private readonly ITransportSettings[] _transportSettings;
        private readonly ClientOptions _clientOptions;

        // Stores message input names supported by the client module and their associated delegate.
        private volatile Dictionary<string, Tuple<MessageHandler, object>> _receiveEventEndpoints;

        private volatile Tuple<MessageHandler, object> _defaultEventCallback;

        // Stores methods supported by the client device and their associated delegate.

        private bool _isDeviceMethodEnabled;
        private readonly Dictionary<string, Tuple<MethodCallback, object>> _deviceMethods =
            new Dictionary<string, Tuple<MethodCallback, object>>();

        private volatile Tuple<MethodCallback, object> _deviceDefaultMethodCallback;

        private volatile ConnectionStatusChangesHandler _connectionStatusChangesHandler;

        // Count of messages sent by the device/ module. This is used for sending diagnostic information.
        private int _currentMessageCount;

        private int _diagnosticSamplingPercentage;

        private ConnectionStatus _lastConnectionStatus = ConnectionStatus.Disconnected;
        private ConnectionStatusChangeReason _lastConnectionStatusChangeReason = ConnectionStatusChangeReason.Client_Close;

        private volatile Tuple<ReceiveMessageCallback, object> _deviceReceiveMessageCallback;

        private bool _twinPatchSubscribedWithService;
        private object _twinPatchCallbackContext;

        // Callback to call whenever the twin's desired state is updated by the service.
        internal DesiredPropertyUpdateCallback _desiredPropertyUpdateCallback;

        internal delegate Task OnMethodCalledDelegate(MethodRequestInternal methodRequestInternal);

        internal delegate Task OnDeviceMessageReceivedDelegate(Message message);

        internal delegate Task OnModuleEventMessageReceivedDelegate(string input, Message message);

        public InternalClient(
            IotHubConnectionString iotHubConnectionString,
            ITransportSettings[] transportSettings,
            IDeviceClientPipelineBuilder pipelineBuilder,
            ClientOptions options)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, transportSettings, pipelineBuilder, nameof(InternalClient) + "_ctor");

            TlsVersions.Instance.SetLegacyAcceptableVersions();

            _transportSettings = transportSettings;
            _clientOptions = options;
            IotHubConnectionString = iotHubConnectionString;

            var pipelineContext = new PipelineContext()
            {
                TransportSettingsArray = transportSettings,
                IotHubConnectionString = iotHubConnectionString,
                MethodCallback = OnMethodCalledAsync,
                DesiredPropertyUpdateCallback = OnReportedStatePatchReceived,
                ConnectionStatusChangesHandler = OnConnectionStatusChanged,
                ModuleEventCallback = OnModuleEventMessageReceivedAsync,
                DeviceEventCallback = OnDeviceMessageReceivedAsync,
                ProductInfo = _productInfo,
                ClientOptions = options
            };

            IDelegatingHandler innerHandler = pipelineBuilder.Build(pipelineContext);

            if (Logging.IsEnabled)
                Logging.Associate(this, innerHandler, nameof(InternalClient));

            InnerHandler = innerHandler;

            if (Logging.IsEnabled)
                Logging.Associate(this, transportSettings, nameof(InternalClient));

            _fileUploadHttpTransportHandler = new HttpTransportHandler(pipelineContext, IotHubConnectionString, options.FileUploadTransportSettings);

            if (Logging.IsEnabled)
                Logging.Exit(this, transportSettings, pipelineBuilder, nameof(InternalClient) + "_ctor");
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
                        DiagnosticSamplingPercentage,
                        "The range of diagnostic sampling percentage should between [0,100].");
                }

                if (IsE2eDiagnosticSupportedProtocol())
                {
                    _diagnosticSamplingPercentage = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the timeout used in the operation retries.
        /// </summary>
        public uint OperationTimeoutInMilliseconds { get; set; } = DeviceClient.DefaultOperationTimeoutInMilliseconds;

        /// <summary>
        /// Stores custom product information that will be appended to the user agent string that is sent to IoT hub.
        /// </summary>
        public string ProductInfo
        {
            // We store InternalClient.ProductInfo as a string property of an object (rather than directly as a string)
            // so that updates will propagate down to the transport layer throughout the lifetime of the InternalClient
            // object instance.
            get => _productInfo.Extra;
            set => _productInfo.Extra = value;
        }

        /// <summary>
        /// Stores the retry strategy used in the operation retries.
        /// </summary>
        [Obsolete("This method has been deprecated.  Please use Microsoft.Azure.Devices.Client.SetRetryPolicy(IRetryPolicy retryPolicy) instead.")]
        public RetryPolicyType RetryPolicy { get; set; }

        internal X509Certificate2 Certificate { get; set; }

        internal IDelegatingHandler InnerHandler { get; set; }

        internal IotHubConnectionString IotHubConnectionString { get; private set; }

        /// <summary>
        /// Sets the retry policy used in the operation retries.
        /// </summary>
        /// <param name="retryPolicy">The retry policy. The default is new ExponentialBackoff(int.MaxValue, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(100));</param>
        public void SetRetryPolicy(IRetryPolicy retryPolicy)
        {
            RetryDelegatingHandler retryDelegatingHandler = GetDelegateHandler<RetryDelegatingHandler>();
            if (retryDelegatingHandler == null)
            {
                throw new NotSupportedException();
            }

            retryDelegatingHandler.SetRetryPolicy(retryPolicy);
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
                    handler = handler.InnerHandler as DefaultDelegatingHandler;
                }
            }

            return !isFound ? default : (T)handler;
        }

        /// <summary>
        /// Explicitly open the InternalClient instance.
        /// </summary>
        public async Task OpenAsync()
        {
            try
            {
                using CancellationTokenSource cts = CancellationTokenSourceFactory();
                await OpenAsync(cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsCausedByTimeoutOrCancellation(ex))
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// Explicitly open the InternalClient instance.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        public Task OpenAsync(CancellationToken cancellationToken)
        {
            try
            {
                return InnerHandler.OpenAsync(cancellationToken);
            }
            catch (IotHubCommunicationException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        /// <summary>
        /// Close the InternalClient instance
        /// </summary>
        /// <returns>A task to await</returns>
        public async Task CloseAsync()
        {
            try
            {
                using CancellationTokenSource cts = CancellationTokenSourceFactory();
                await CloseAsync(cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsCausedByTimeoutOrCancellation(ex))
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// Close the InternalClient instance
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        public Task CloseAsync(CancellationToken cancellationToken)
        {
            try
            {
                return InnerHandler.CloseAsync(cancellationToken);
            }
            catch (IotHubCommunicationException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        /// <summary>
        /// Sets a new delegate for the connection status changed callback. If a delegate is already associated,
        /// it will be replaced with the new delegate.
        /// </summary>
        /// <param name="statusChangesHandler">The name of the method to associate with the delegate.</param>
        public void SetConnectionStatusChangesHandler(ConnectionStatusChangesHandler statusChangesHandler)
        {
            if (Logging.IsEnabled)
                Logging.Info(this, statusChangesHandler, nameof(SetConnectionStatusChangesHandler));

            _connectionStatusChangesHandler = statusChangesHandler;
        }

        /// <summary>
        /// The delegate for handling disrupted connection/links in the transport layer.
        /// </summary>
        internal void OnConnectionStatusChanged(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, status, reason, nameof(OnConnectionStatusChanged));

                if (_connectionStatusChangesHandler != null
                    && (_lastConnectionStatus != status
                        || _lastConnectionStatusChangeReason != reason))
                {
                    _connectionStatusChangesHandler(status, reason);
                }
            }
            finally
            {
                _lastConnectionStatus = status;
                _lastConnectionStatusChangeReason = reason;
                if (Logging.IsEnabled)
                    Logging.Exit(this, status, reason, nameof(OnConnectionStatusChanged));
            }
        }

        /// <summary>
        /// Deletes a received message from the device queue
        /// </summary>
        /// <returns>The lock identifier for the previously received message</returns>
        public async Task CompleteAsync(string lockToken)
        {
            try
            {
                using CancellationTokenSource cts = CancellationTokenSourceFactory();
                await CompleteAsync(lockToken, cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsCausedByTimeoutOrCancellation(ex))
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// Deletes a received message from the device queue
        /// </summary>
        /// <param name="lockToken"></param>
        /// <param name="cancellationToken">A token to cancel the operation. </param>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task CompleteAsync(string lockToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(lockToken))
            {
                throw new ArgumentNullException(nameof(lockToken));
            }

            try
            {
                return InnerHandler.CompleteAsync(lockToken, cancellationToken);
            }
            catch (IotHubCommunicationException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        /// <summary>
        /// Deletes a received message from the device queue
        /// </summary>
        /// <returns>The previously received message</returns>
        public Task CompleteAsync(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return CompleteAsync(message.LockToken);
        }

        /// <summary>
        /// Deletes a received message from the device queue
        /// </summary>
        /// <param name="message">The message to complete</param>
        /// <param name="cancellationToken">A token to cancel the operation. </param>
        /// <returns>The previously received message</returns>
        public Task CompleteAsync(Message message, CancellationToken cancellationToken)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            // The asynchronous operation shall retry until time specified in OperationTimeoutInMilliseconds
            // property expire or unrecoverable error(authentication, quota exceed) occurs.
            try
            {
                return CompleteAsync(message.LockToken, cancellationToken);
            }
            catch (IotHubCommunicationException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        /// <summary>
        /// Puts a received message back onto the device queue
        /// </summary>
        /// <returns>The previously received message</returns>
        public async Task AbandonAsync(string lockToken)
        {
            try
            {
                using CancellationTokenSource cts = CancellationTokenSourceFactory();
                await AbandonAsync(lockToken, cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsCausedByTimeoutOrCancellation(ex))
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// Puts a received message back onto the device queue
        /// </summary>
        /// <returns>The previously received message</returns>
        public Task AbandonAsync(string lockToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(lockToken))
            {
                throw new ArgumentNullException(nameof(lockToken));
            }

            // The asynchronous operation shall retry until time specified in OperationTimeoutInMilliseconds property
            // expire or unrecoverable error(authentication, quota exceed) occurs.
            try
            {
                return InnerHandler.AbandonAsync(lockToken, cancellationToken);
            }
            catch (IotHubCommunicationException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        /// <summary>
        /// Puts a received message back onto the device queue
        /// </summary>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task AbandonAsync(Message message)
        {
            return message == null
                ? throw new ArgumentNullException(nameof(message))
                : AbandonAsync(message.LockToken);
        }

        /// <summary>
        /// Puts a received message back onto the device queue
        /// </summary>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task AbandonAsync(Message message, CancellationToken cancellationToken)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            try
            {
                return AbandonAsync(message.LockToken, cancellationToken);
            }
            catch (IotHubCommunicationException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        /// <summary>
        /// Deletes a received message from the device queue and indicates to the server that the message could not be processed.
        /// </summary>
        /// <returns>The previously received message</returns>
        public async Task RejectAsync(string lockToken)
        {
            try
            {
                using CancellationTokenSource cts = CancellationTokenSourceFactory();
                await RejectAsync(lockToken, cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsCausedByTimeoutOrCancellation(ex))
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// Deletes a received message from the device queue and indicates to the server that the message could not be processed.
        /// </summary>
        /// <returns>The previously received message</returns>
        public Task RejectAsync(string lockToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(lockToken))
            {
                throw new ArgumentNullException(nameof(lockToken));
            }

            try
            {
                return InnerHandler.RejectAsync(lockToken, cancellationToken);
            }
            catch (IotHubCommunicationException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        /// <summary>
        /// Deletes a received message from the device queue and indicates to the server that the message could not be processed.
        /// </summary>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task RejectAsync(Message message)
        {
            return message == null
                ? throw new ArgumentNullException(nameof(message))
                : RejectAsync(message.LockToken);
        }

        /// <summary>
        /// Deletes a received message from the device queue and indicates to the server that the message could not be processed.
        /// </summary>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task RejectAsync(Message message, CancellationToken cancellationToken)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            try
            {
                return RejectAsync(message.LockToken, cancellationToken);
            }
            catch (IotHubCommunicationException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        /// <summary>
        /// Sends an event to device hub
        /// </summary>
        /// <returns>The message containing the event</returns>
        public async Task SendEventAsync(MessageBase message)
        {
            try
            {
                using CancellationTokenSource cts = CancellationTokenSourceFactory();
                await SendEventAsync(message, cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsCausedByTimeoutOrCancellation(ex))
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// Sends an event to device hub
        /// </summary>
        /// <returns>The message containing the event</returns>
        public Task SendEventAsync(MessageBase message, CancellationToken cancellationToken)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (_clientOptions?.SdkAssignsMessageId == SdkAssignsMessageId.WhenUnset && message.MessageId == null)
            {
                message.MessageId = Guid.NewGuid().ToString();
            }

            IotHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, _diagnosticSamplingPercentage, ref _currentMessageCount);
            // The asynchronous operation shall retry until time specified in OperationTimeoutInMilliseconds property
            // expire or unrecoverable error(authentication or quota exceed) occurs.
            try
            {
                return InnerHandler.SendEventAsync(message, cancellationToken);
            }
            catch (IotHubCommunicationException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        /// <summary>
        /// Sends a batch of events to device hub
        /// </summary>
        /// <returns>The task containing the event</returns>
        public async Task SendEventBatchAsync(IEnumerable<Message> messages)
        {
            try
            {
                using CancellationTokenSource cts = CancellationTokenSourceFactory();
                await SendEventBatchAsync(messages, cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsCausedByTimeoutOrCancellation(ex))
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// Sends a batch of events to device hub
        /// </summary>
        /// <returns>The task containing the event</returns>
        public Task SendEventBatchAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            if (messages == null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            if (_clientOptions?.SdkAssignsMessageId == SdkAssignsMessageId.WhenUnset)
            {
                foreach (Message message in messages)
                {
                    if (message.MessageId == null)
                    {
                        message.MessageId = Guid.NewGuid().ToString();
                    }
                }
            }

            // The asynchronous operation shall retry until time specified in OperationTimeoutInMilliseconds property
            // expire or unrecoverable error(authentication or quota exceed) occurs.
            try
            {
                return InnerHandler.SendEventAsync(messages, cancellationToken);
            }
            catch (IotHubCommunicationException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
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
        public async Task SetMethodHandlerAsync(string methodName, MethodCallback methodHandler, object userContext)
        {
            try
            {
                using CancellationTokenSource cts = CancellationTokenSourceFactory();
                await SetMethodHandlerAsync(methodName, methodHandler, userContext, cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsCausedByTimeoutOrCancellation(ex))
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
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
        public async Task SetMethodHandlerAsync(string methodName, MethodCallback methodHandler, object userContext, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, methodName, methodHandler, userContext, nameof(SetMethodHandlerAsync));

            cancellationToken.ThrowIfCancellationRequested();
            await _methodsSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (methodHandler != null)
                {
                    await HandleMethodEnableAsync(cancellationToken).ConfigureAwait(false);
                    _deviceMethods[methodName] = new Tuple<MethodCallback, object>(methodHandler, userContext);
                }
                else
                {
                    _deviceMethods.Remove(methodName);
                    await HandleMethodDisableAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (IotHubCommunicationException ex) when (ex.InnerException is OperationCanceledException)
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
        public async Task SetMethodDefaultHandlerAsync(MethodCallback methodHandler, object userContext)
        {
            try
            {
                using CancellationTokenSource cts = CancellationTokenSourceFactory();
                await SetMethodDefaultHandlerAsync(methodHandler, userContext, cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsCausedByTimeoutOrCancellation(ex))
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
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
        public async Task SetMethodDefaultHandlerAsync(MethodCallback methodHandler, object userContext, CancellationToken cancellationToken)
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

                    _deviceDefaultMethodCallback = new Tuple<MethodCallback, object>(methodHandler, userContext);
                }
                else
                {
                    _deviceDefaultMethodCallback = null;

                    await HandleMethodDisableAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (IotHubCommunicationException ex) when (ex.InnerException is OperationCanceledException)
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
        /// Sets a new delegate for the named method. If a delegate is already associated with
        /// the named method, it will be replaced with the new delegate.
        /// </summary>
        /// <param name="methodName">The name of the method to associate with the delegate.</param>
        /// <param name="methodHandler">The delegate to be used when a method with the given name is called by the cloud service.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>

        [Obsolete("Please use SetMethodHandlerAsync.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetMethodHandler(string methodName, MethodCallback methodHandler, object userContext)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, methodName, methodHandler, userContext, nameof(SetMethodHandler));

            try
            {
                // Dangerous: can cause deadlocks.
                SetMethodHandlerAsync(methodName, methodHandler, userContext).GetAwaiter().GetResult();
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, methodName, methodHandler, userContext, nameof(SetMethodHandler));
            }
        }

        /// <summary>
        /// The delegate for handling direct methods received from service.
        /// </summary>
        internal async Task OnMethodCalledAsync(MethodRequestInternal methodRequestInternal)
        {
            Tuple<MethodCallback, object> callbackContextPair = null;

            if (Logging.IsEnabled)
                Logging.Enter(this, methodRequestInternal?.Name, methodRequestInternal, nameof(OnMethodCalledAsync));

            if (methodRequestInternal == null)
            {
                return;
            }

            MethodResponseInternal methodResponseInternal = null;
            byte[] requestData = methodRequestInternal.GetBytes();

            await _methodsSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                Utils.ValidateDataIsEmptyOrJson(requestData);
                if (!_deviceMethods.TryGetValue(methodRequestInternal.Name, out callbackContextPair))
                {
                    callbackContextPair = _deviceDefaultMethodCallback;
                }
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, ex, nameof(OnMethodCalledAsync));

                methodResponseInternal = new MethodResponseInternal(methodRequestInternal.RequestId, (int)MethodResponseStatusCode.BadRequest);

                await SendMethodResponseAsync(methodResponseInternal, methodRequestInternal.CancellationToken).ConfigureAwait(false);

                if (Logging.IsEnabled)
                    Logging.Error(this, ex, nameof(OnMethodCalledAsync));

                return;
            }
            finally
            {
                try
                {
                    methodResponseInternal?.Dispose();
                }
                finally
                {
                    // Need to release this semaphore even if the above dispose call fails
                    _methodsSemaphore.Release();
                }
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
                    MethodResponse rv = await callbackContextPair
                        .Item1(new MethodRequest(methodRequestInternal.Name, requestData), callbackContextPair.Item2)
                        .ConfigureAwait(false);

                    methodResponseInternal = rv.Result == null
                        ? new MethodResponseInternal(methodRequestInternal.RequestId, rv.Status)
                        : new MethodResponseInternal(rv.Result, methodRequestInternal.RequestId, rv.Status);
                }
                catch (Exception ex)
                {
                    if (Logging.IsEnabled)
                        Logging.Error(this, ex, nameof(OnMethodCalledAsync));

                    methodResponseInternal = new MethodResponseInternal(methodRequestInternal.RequestId, (int)MethodResponseStatusCode.UserCodeException);
                }
            }

            try
            {
                await SendMethodResponseAsync(methodResponseInternal, methodRequestInternal.CancellationToken).ConfigureAwait(false);
            }
            finally
            {
                methodResponseInternal?.Dispose();
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, methodRequestInternal.Name, methodRequestInternal, nameof(OnMethodCalledAsync));
        }

        internal Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken)
        {
            try
            {
                return InnerHandler.SendMethodResponseAsync(methodResponse, cancellationToken);
            }
            catch (IotHubCommunicationException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        private async Task HandleMethodEnableAsync(CancellationToken cancellationToken)
        {
            // If currently enabled, then skip
            if (_isDeviceMethodEnabled)
            {
                return;
            }

            await InnerHandler.EnableMethodsAsync(cancellationToken).ConfigureAwait(false);
            _isDeviceMethodEnabled = true;
        }

        /// <summary>
        /// Sets a callback that will be called whenever the client receives a state update
        /// (desired or reported) from the service.
        /// Set callback value to null to clear.
        /// </summary>
        /// <remarks>
        /// This has the side-effect of subscribing to the PATCH topic on the service.
        /// </remarks>
        /// <param name="callback">Callback to call after the state update has been received and applied</param>
        /// <param name="userContext">Context object that will be passed into callback</param>
        public async Task SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback callback, object userContext)
        {
            try
            {
                using CancellationTokenSource cts = CancellationTokenSourceFactory();
                await SetDesiredPropertyUpdateCallbackAsync(callback, userContext, cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsCausedByTimeoutOrCancellation(ex))
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// Set a callback that will be called whenever the client receives a state update
        /// (desired or reported) from the service.
        /// Set callback value to null to clear.
        /// </summary>
        /// <remarks>
        /// This has the side-effect of subscribing to the PATCH topic on the service.
        /// </remarks>
        /// <param name="callback">Callback to call after the state update has been received and applied</param>
        /// <param name="userContext">Context object that will be passed into callback</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        public async Task SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback callback, object userContext, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, callback, userContext, nameof(SetDesiredPropertyUpdateCallbackAsync));

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
            catch (IotHubCommunicationException ex) when (ex.InnerException is OperationCanceledException)
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
        /// Set a callback that will be called whenever the client receives a state update
        /// (desired or reported) from the service.  This has the side-effect of subscribing
        /// to the PATCH topic on the service.
        /// </summary>
        /// <param name="callback">Callback to call after the state update has been received and applied</param>
        /// <param name="userContext">Context object that will be passed into callback</param>
        [Obsolete("Please use SetDesiredPropertyUpdateCallbackAsync.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task SetDesiredPropertyUpdateCallback(DesiredPropertyUpdateCallback callback, object userContext)
        {
            // Obsoleted due to incorrect naming:
            return SetDesiredPropertyUpdateCallbackAsync(callback, userContext);
        }

        /// <summary>
        /// Retrieve the device twin properties for the current device.
        /// For the complete device twin object, use Microsoft.Azure.Devices.RegistryManager.GetTwinAsync(string deviceId).
        /// </summary>
        /// <returns>The device twin object for the current device</returns>
        public async Task<Twin> GetTwinAsync()
        {
            try
            {
                using CancellationTokenSource cts = CancellationTokenSourceFactory();
                return await GetTwinAsync(cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsCausedByTimeoutOrCancellation(ex))
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// Retrieve the device twin properties for the current device.
        /// For the complete device twin object, use Microsoft.Azure.Devices.RegistryManager.GetTwinAsync(string deviceId).
        /// </summary>
        /// <returns>The device twin object for the current device</returns>
        public async Task<Twin> GetTwinAsync(CancellationToken cancellationToken)
        {
            TwinProperties twinProperties = await InnerHandler
                    .GetClientTwinPropertiesAsync<TwinProperties>(cancellationToken).ConfigureAwait(false);
            return new Twin
            {
                Properties = twinProperties,
            };
        }

        /// <summary>
        /// Push reported property changes up to the service.
        /// </summary>
        /// <param name="reportedProperties">Reported properties to push</param>
        public async Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties)
        {
            try
            {
                using CancellationTokenSource cts = CancellationTokenSourceFactory();
                await UpdateReportedPropertiesAsync(reportedProperties, cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsCausedByTimeoutOrCancellation(ex))
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// Push reported property changes up to the service.
        /// </summary>
        /// <param name="reportedProperties">Reported properties to push</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        public async Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties, CancellationToken cancellationToken)
        {
            // `UpdateReportedPropertiesAsync` shall throw an `ArgumentNull` exception if `reportedProperties` is null.
            if (reportedProperties == null)
            {
                throw new ArgumentNullException(nameof(reportedProperties));
            }

            string body = JsonConvert.SerializeObject(reportedProperties);
            using Stream bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(body));

            await InnerHandler.SendClientTwinPropertyPatchAsync(bodyStream, cancellationToken).ConfigureAwait(false);
        }

        internal void OnReportedStatePatchReceived(TwinCollection patch)
        {
            if (_desiredPropertyUpdateCallback == null)
            {
                return;
            }

            if (Logging.IsEnabled)
                Logging.Info(this, patch.ToJson(), nameof(OnReportedStatePatchReceived));

            _desiredPropertyUpdateCallback(patch, _twinPatchCallbackContext);
        }

        #region Device Specific API

        /// <summary>
        /// Receive a message from the device queue using the default timeout.
        /// </summary>
        /// <returns>The receive message or null if there was no message until the default timeout</returns>
        public async Task<Message> ReceiveAsync()
        {
            try
            {
                using CancellationTokenSource cts = CancellationTokenSourceFactory();
                return await ReceiveAsync(cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsCausedByTimeoutOrCancellation(ex))
            {
                // Exception adaptation for non-CancellationToken public API.
                return null;
            }
        }

        /// <summary>
        /// Receive a message from the device queue with the specified timeout
        /// </summary>
        /// <returns>The receive message or null if there was no message until the specified time has elapsed</returns>
        public async Task<Message> ReceiveAsync(TimeSpan timeout)
        {
            try
            {
                return await InnerHandler.ReceiveAsync(new TimeoutHelper(timeout)).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsCausedByTimeoutOrCancellation(ex))
            {
                return null;
            }
        }

        /// <summary>
        /// Receive a message from the device queue using the default timeout.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive
        /// notice of cancellation.</param>
        /// <returns>The receive message or null if there was no message until the default timeout</returns>
        public Task<Message> ReceiveAsync(CancellationToken cancellationToken)
        {
            // The asynchronous operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or
            // unrecoverable (authentication, quota exceed) error occurs.
            try
            {
                return InnerHandler.ReceiveAsync(cancellationToken);
            }
            catch (IotHubCommunicationException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        private async Task HandleMethodDisableAsync(CancellationToken cancellationToken)
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
            ReceiveMessageCallback messageHandler,
            object userContext,
            CancellationToken cancellationToken)
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
                    _deviceReceiveMessageCallback = new Tuple<ReceiveMessageCallback, object>(messageHandler, userContext);
                }
                else
                {
                    // If a null delegate is passed, it will disable the callback triggered on receiving messages from the service.
                    _deviceReceiveMessageCallback = null;
                    await DisableReceiveMessageAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (IotHubCommunicationException ex) when (ex.InnerException is OperationCanceledException)
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

        // Enable telemetry downlink for devices
        private Task EnableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            // The telemetry downlink needs to be enabled only for the first time that the _receiveMessageCallback delegate is set.
            return _deviceReceiveMessageCallback == null
                ? InnerHandler.EnableReceiveMessageAsync(cancellationToken)
                : TaskHelpers.CompletedTask;
        }

        // Disable telemetry downlink for devices
        private Task DisableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            // The telemetry downlink should be disabled only after _receiveMessageCallback delegate has been removed.
            return _deviceReceiveMessageCallback == null
                ? InnerHandler.DisableReceiveMessageAsync(cancellationToken)
                : TaskHelpers.CompletedTask;
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
                ReceiveMessageCallback callback = _deviceReceiveMessageCallback?.Item1;
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

        internal Task<FileUploadSasUriResponse> GetFileUploadSasUriAsync(
            FileUploadSasUriRequest request,
            CancellationToken cancellationToken = default)
        {
            return _fileUploadHttpTransportHandler.GetFileUploadSasUriAsync(request, cancellationToken);
        }

        internal Task CompleteFileUploadAsync(
            FileUploadCompletionNotification notification,
            CancellationToken cancellationToken = default)
        {
            return _fileUploadHttpTransportHandler.CompleteFileUploadAsync(notification, cancellationToken);
        }

        /// <summary>
        /// Uploads a stream to a block blob in a storage account associated with the IoT hub for that device.
        /// If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="blobName"></param>
        /// <param name="source"></param>
        /// <returns>AsncTask</returns>
        [Obsolete("This API has been split into three APIs: GetFileUploadSasUri, uploading to blob directly using the Azure Storage SDK, and CompleteFileUploadAsync")]
        public Task UploadToBlobAsync(string blobName, Stream source)
        {
            return UploadToBlobAsync(blobName, source, CancellationToken.None);
        }

        /// <summary>
        /// Uploads a stream to a block blob in a storage account associated with the IoTHub for that device.
        /// If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="blobName"></param>
        /// <param name="source"></param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to
        /// receive notice of cancellation.</param>
        /// <returns>AsncTask</returns>
        [Obsolete("This API has been split into three APIs: GetFileUploadSasUri, uploading to blob directly using the Azure Storage SDK, and CompleteFileUploadAsync")]
        public Task UploadToBlobAsync(string blobName, Stream source, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, blobName, source, nameof(UploadToBlobAsync));

                if (string.IsNullOrEmpty(blobName))
                {
                    throw Fx.Exception.ArgumentNull(nameof(blobName));
                }
                if (source == null)
                {
                    throw Fx.Exception.ArgumentNull(nameof(source));
                }
                if (blobName.Length > 1024)
                {
                    throw Fx.Exception.Argument(nameof(blobName), "Length cannot exceed 1024 characters");
                }
                if (blobName.Split('/').Length > 254)
                {
                    throw Fx.Exception.Argument(nameof(blobName), "Path segment count cannot exceed 254");
                }

                return _fileUploadHttpTransportHandler.UploadToBlobAsync(blobName, source, cancellationToken);
            }
            catch (IotHubCommunicationException ex) when (ex.InnerException is OperationCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, blobName, nameof(UploadToBlobAsync));
            }
        }

        #endregion Device Specific API

        #region Module Specific API

        /// <summary>
        /// Sends an event (message) to the hub
        /// </summary>
        /// <param name="outputName">The output target for sending the given message</param>
        /// <param name="message">The message to send</param>
        /// <returns>The message containing the event</returns>
        public async Task SendEventAsync(string outputName, Message message)
        {
            try
            {
                using CancellationTokenSource cts = CancellationTokenSourceFactory();
                await SendEventAsync(outputName, message, cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsCausedByTimeoutOrCancellation(ex))
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// Sends an event (message) to the hub
        /// </summary>
        /// <param name="outputName">The output target for sending the given message</param>
        /// <param name="message">The message to send</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>The message containing the event</returns>
        public Task SendEventAsync(string outputName, Message message, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, outputName, message, nameof(SendEventAsync));

                ValidateModuleTransportHandler("SendEventAsync for a named output");

                if (string.IsNullOrWhiteSpace(outputName))
                {
                    throw new ArgumentNullException(nameof(outputName));
                }

                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                message.SystemProperties.Add(MessageSystemPropertyNames.OutputName, outputName);

                return InnerHandler.SendEventAsync(message, cancellationToken);
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
        /// <returns>The task containing the event</returns>
        public async Task SendEventBatchAsync(string outputName, IEnumerable<Message> messages)
        {
            try
            {
                using CancellationTokenSource cts = CancellationTokenSourceFactory();
                await SendEventBatchAsync(outputName, messages, cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsCausedByTimeoutOrCancellation(ex))
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
            }
        }

        /// <summary>
        /// Sends a batch of events to device hub
        /// </summary>
        /// <param name="outputName">The output target for sending the given message</param>
        /// <param name="messages">A list of one or more messages to send</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The task containing the event</returns>
        public Task SendEventBatchAsync(string outputName, IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, outputName, messages, nameof(SendEventBatchAsync));

                ValidateModuleTransportHandler("SendEventBatchAsync for a named output");

                if (string.IsNullOrWhiteSpace(outputName))
                {
                    throw new ArgumentNullException(nameof(outputName));
                }

                var messagesList = messages?.ToList();
                if (messagesList == null || messagesList.Count == 0)
                {
                    throw new ArgumentNullException(nameof(messages));
                }

                messagesList.ForEach(m => m.SystemProperties.Add(MessageSystemPropertyNames.OutputName, outputName));

                return InnerHandler.SendEventAsync(messagesList, cancellationToken);
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
        /// <see cref="ModuleClient"/> when a <see cref="Edge.EdgeModuleClientFactory"/> creates the module.</param>
        /// <returns>The task containing the event</returns>
        public async Task SetInputMessageHandlerAsync(
            string inputName,
            MessageHandler messageHandler,
            object userContext,
            bool isAnEdgeModule)
        {
            try
            {
                using CancellationTokenSource cts = CancellationTokenSourceFactory();
                await SetInputMessageHandlerAsync(inputName, messageHandler, userContext, isAnEdgeModule, cts.Token)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (IsCausedByTimeoutOrCancellation(ex))
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
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
        /// <see cref="ModuleClient"/> when a <see cref="Edge.EdgeModuleClientFactory"/> creates the module.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The task containing the event</returns>
        public async Task SetInputMessageHandlerAsync(
            string inputName,
            MessageHandler messageHandler,
            object userContext,
            bool isAnEdgeModule,
            CancellationToken cancellationToken)
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
                        _receiveEventEndpoints = new Dictionary<string, Tuple<MessageHandler, object>>();
                    }

                    _receiveEventEndpoints[inputName] = new Tuple<MessageHandler, object>(messageHandler, userContext);
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
        /// <see cref="ModuleClient"/> when a <see cref="Edge.EdgeModuleClientFactory"/> creates the module.</param>
        /// <returns>The task containing the event</returns>
        public async Task SetMessageHandlerAsync(MessageHandler messageHandler, object userContext, bool isAnEdgeModule)
        {
            try
            {
                using CancellationTokenSource cts = CancellationTokenSourceFactory();
                await SetMessageHandlerAsync(messageHandler, userContext, isAnEdgeModule, cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsCausedByTimeoutOrCancellation(ex))
            {
                // Exception adaptation for non-CancellationToken public API.
                throw new TimeoutException("The operation timed out.", ex);
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
        /// <see cref="ModuleClient"/> when a <see cref="Edge.EdgeModuleClientFactory"/> creates the module.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The task containing the event</returns>
        public async Task SetMessageHandlerAsync(
            MessageHandler messageHandler,
            object userContext,
            bool isAnEdgeModule,
            CancellationToken cancellationToken)
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
                    _defaultEventCallback = new Tuple<MessageHandler, object>(messageHandler, userContext);
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
                Tuple<MessageHandler, object> callback = null;
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
                    response = await callback.Item1.Invoke(message, callback.Item2).ConfigureAwait(false);
                }

                if (Logging.IsEnabled)
                    Logging.Info(this, $"{nameof(MessageResponse)} = {response}", nameof(OnModuleEventMessageReceivedAsync));

                switch (response)
                {
                    case MessageResponse.Completed:
                        await CompleteAsync(message).ConfigureAwait(false);
                        break;

                    case MessageResponse.Abandoned:
                        await AbandonAsync(message).ConfigureAwait(false);
                        break;

                    default:
                        break;
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, input, message, nameof(OnModuleEventMessageReceivedAsync));
            }
        }

        // Enable telemetry downlink for modules
        private Task EnableEventReceiveAsync(bool isAnEdgeModule, CancellationToken cancellationToken)
        {
            // The telemetry downlink needs to be enabled only for the first time that the _defaultEventCallback delegate is set.
            return _receiveEventEndpoints == null && _defaultEventCallback == null
                ? InnerHandler.EnableEventReceiveAsync(isAnEdgeModule, cancellationToken)
                : TaskHelpers.CompletedTask;
        }

        // Disable telemetry downlink for modules
        private Task DisableEventReceiveAsync(bool isAnEdgeModule, CancellationToken cancellationToken)
        {
            // The telemetry downlink should be disabled only after _defaultEventCallback delegate has been removed.
            return _receiveEventEndpoints == null && _defaultEventCallback == null
                ? InnerHandler.DisableEventReceiveAsync(isAnEdgeModule, cancellationToken)
                : TaskHelpers.CompletedTask;
        }

        internal void ValidateModuleTransportHandler(string apiName)
        {
            if (string.IsNullOrEmpty(IotHubConnectionString.ModuleId))
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

        internal bool IsE2eDiagnosticSupportedProtocol()
        {
            foreach (ITransportSettings transportSetting in _transportSettings)
            {
                TransportType transportType = transportSetting.GetTransportType();
                if (!(transportType == TransportType.Amqp_WebSocket_Only
                    || transportType == TransportType.Amqp_Tcp_Only
                    || transportType == TransportType.Mqtt_WebSocket_Only
                    || transportType == TransportType.Mqtt_Tcp_Only))
                {
                    throw new NotSupportedException($"{transportType} protocol doesn't support E2E diagnostic.");
                }
            }
            return true;
        }

        private static bool IsCausedByTimeoutOrCancellation(Exception ex)
        {
            return ex is OperationCanceledException
                || ex is IotHubCommunicationException
                && (ex.InnerException is OperationCanceledException
                || ex.InnerException is TimeoutException);
        }

        private CancellationTokenSource CancellationTokenSourceFactory()
        {
            return OperationTimeoutInMilliseconds == 0
                ? new CancellationTokenSource()
                : new CancellationTokenSource(TimeSpan.FromMilliseconds(OperationTimeoutInMilliseconds));
        }
    }
}
