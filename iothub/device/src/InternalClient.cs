// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.Azure.Devices.Client.Utilities;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Contains methods that a client can use to send messages to and receive messages from the service,
    /// respond to direct method invocations from the service, and send and receive twin property updates.
    /// </summary>
    public class InternalClient : IDisposable
    {
        private readonly SemaphoreSlim _methodsSemaphore = new(1, 1);
        private readonly SemaphoreSlim _twinDesiredPropertySemaphore = new(1, 1);

        // Connection status change information
        private volatile Action<ConnectionStatusInfo> _connectionStatusChangeHandler;

        // Method callback information
        private bool _isDeviceMethodEnabled;

        private volatile Tuple<Func<DirectMethodRequest, object, Task<DirectMethodResponse>>, object> _deviceDefaultMethodCallback;
        private readonly Dictionary<string, Tuple<Func<DirectMethodRequest, object, Task<DirectMethodResponse>>, object>> _deviceMethods = new();

        // Twin property update request callback information
        private bool _twinPatchSubscribedWithService;

        private object _twinPatchCallbackContext;
        private Func<TwinCollection, object, Task> _desiredPropertyUpdateCallback;

        // Diagnostic information

        // Count of messages sent by the device/ module. This is used for sending diagnostic information.
        private int _currentMessageCount;

        private int _diagnosticSamplingPercentage;

        internal InternalClient(
            IotHubConnectionCredentials iotHubConnectionCredentials,
            IotHubClientOptions iotHubClientOptions)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, iotHubClientOptions?.TransportSettings, nameof(InternalClient) + "_ctor");

            // Make sure client options is initialized.
            if (iotHubClientOptions == default)
            {
                iotHubClientOptions = new();
            }

            IotHubConnectionCredentials = iotHubConnectionCredentials;
            ClientOptions = iotHubClientOptions;

            PipelineContext = new PipelineContext
            {
                IotHubConnectionCredentials = IotHubConnectionCredentials,
                ProductInfo = ClientOptions.ProductInfo,
                IotHubClientTransportSettings = ClientOptions.TransportSettings,
                ModelId = ClientOptions.ModelId,
                MethodCallback = OnMethodCalledAsync,
                DesiredPropertyUpdateCallback = OnDesiredStatePatchReceived,
                ConnectionStatusChangeHandler = OnConnectionStatusChanged,
            };

            if (Logging.IsEnabled)
                Logging.Exit(this, ClientOptions.TransportSettings, nameof(InternalClient) + "_ctor");
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

                _diagnosticSamplingPercentage = value;
            }
        }

        /// <summary>
        /// The latest connection status information since the last status change.
        /// </summary>
        public ConnectionStatusInfo ConnectionStatusInfo { get; private set; } = new();

        internal PipelineContext PipelineContext { get; private set; }

        internal IotHubClientOptions ClientOptions { get; private set; }

        internal IotHubConnectionCredentials IotHubConnectionCredentials { get; private set; }

        internal IDelegatingHandler InnerHandler { get; set; }

        /// <summary>
        /// Sets the retry policy used in the operation retries.
        /// The change will take effect after any in-progress operations.
        /// </summary>
        /// <param name="retryPolicy">The retry policy. The default is
        /// <c>new ExponentialBackoff(int.MaxValue, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(100));</c></param>
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
        /// Open the client instance. Must be done before any operation can begin.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
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
        /// Sends an event to IoT hub. The client instance must be opened already.
        /// </summary>
        /// <remarks>
        /// In case of a transient issue, retrying the operation should work. In case of a non-transient issue, inspect
        /// the error details and take steps accordingly.
        /// Please note that the list of exceptions is not exhaustive.
        /// </remarks>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="ArgumentNullException">Thrown when a required parameter is null.</exception>
        /// <exception cref="IotHubClientException">Thrown and <see cref="IotHubClientException.StatusCode"/> is set to <see cref="IotHubStatusCode.NetworkErrors"/>
        /// when the operation has been canceled. The inner exception will be <see cref="OperationCanceledException"/>.</exception>
        /// <exception cref="IotHubClientException">Thrown and <see cref="IotHubClientException.StatusCode"/> is set to <see cref="IotHubStatusCode.NetworkErrors"/>
        /// if the client encounters a transient retryable exception. </exception>
        /// <exception cref="InvalidOperationException">Thrown if the client instance is not opened already.</exception>
        /// <exception cref="SocketException">Thrown if a socket error occurs.</exception>
        /// <exception cref="WebSocketException">Thrown if an error occurs when performing an operation on a WebSocket connection.</exception>
        /// <exception cref="IOException">Thrown if an I/O error occurs.</exception>
        /// <exception cref="ClosedChannelException">Thrown if the MQTT transport layer closes unexpectedly.</exception>
        /// <exception cref="IotHubClientException">Thrown if an error occurs when communicating with IoT hub service.
        /// If <see cref="IotHubClientException.IsTransient"/> is set to <c>true</c> then it is a transient exception and should be retried,
        /// but if <c>false</c> then it is a non-transient exception and should probably not be retried.</exception>
        public async Task SendEventAsync(Message message, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(message, nameof(message));
            cancellationToken.ThrowIfCancellationRequested();

            if (ClientOptions?.SdkAssignsMessageId == SdkAssignsMessageId.WhenUnset && message.MessageId == null)
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
        /// Sends a batch of events to IoT hub. Use AMQP or HTTPs for a true batch operation. MQTT will just send the messages
        /// one after the other. The client instance must be opened already.
        /// </summary>
        /// <remarks>
        /// For more information on IoT Edge module routing for <see cref="IotHubModuleClient"/> see <see href="https://docs.microsoft.com/azure/iot-edge/module-composition?view=iotedge-2018-06#declare-routes"/>.
        /// </remarks>
        /// <param name="messages">An <see cref="IEnumerable{Message}"/> set of message objects.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="InvalidOperationException">Thrown if the client instance is not opened already.</exception>
        /// <exception cref="IotHubClientException">Thrown and <see cref="IotHubClientException.StatusCode"/> is set to <see cref="IotHubStatusCode.NetworkErrors"/>
        /// when the operation has been canceled. The inner exception will be <see cref="OperationCanceledException"/>.</exception>
        public async Task SendEventBatchAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrEmpty(messages, nameof(messages));
            cancellationToken.ThrowIfCancellationRequested();

            if (ClientOptions?.SdkAssignsMessageId == SdkAssignsMessageId.WhenUnset)
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
        /// Deletes a received message from the client queue.
        /// </summary>
        /// <param name="lockToken">The message lockToken.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="IotHubClientException">Thrown and <see cref="IotHubClientException.StatusCode"/> is set to <see cref="IotHubStatusCode.NetworkErrors"/>
        /// when the operation has been canceled. The inner exception will be <see cref="OperationCanceledException"/>.</exception>
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
        /// Deletes the received message from the service's cloud to device message queue for this client.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="IotHubClientException">Thrown and <see cref="IotHubClientException.StatusCode"/> is set to <see cref="IotHubStatusCode.NetworkErrors"/>
        /// when the operation has been canceled. The inner exception will be <see cref="OperationCanceledException"/>.</exception>
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
        /// Puts a received message back onto the client queue.
        /// </summary>
        /// <remarks>
        /// Messages cannot be rejected or abandoned over the MQTT protocol. For more details, see
        /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d#the-cloud-to-device-message-life-cycle"/>.
        /// </remarks>
        /// <param name="lockToken">The message lockToken.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="IotHubClientException">Thrown and <see cref="IotHubClientException.StatusCode"/> is set to <see cref="IotHubStatusCode.NetworkErrors"/>
        /// when the operation has been canceled. The inner exception will be <see cref="OperationCanceledException"/>.</exception>
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
        /// Puts a received message back onto the client queue.
        /// </summary>
        /// <remarks>
        /// Messages cannot be rejected or abandoned over the MQTT protocol. For more details, see
        /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d#the-cloud-to-device-message-life-cycle"/>.
        /// </remarks>
        /// <param name="message">The message to abandon.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="IotHubClientException">Thrown and <see cref="IotHubClientException.StatusCode"/> is set to <see cref="IotHubStatusCode.NetworkErrors"/>
        /// when the operation has been canceled. The inner exception will be <see cref="OperationCanceledException"/>.</exception>
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
        /// Deletes a received message from the client queue and indicates to the server that the message could not be processed.
        /// </summary>
        /// <remarks>
        /// Messages cannot be rejected or abandoned over the MQTT protocol. For more details, see
        /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d#the-cloud-to-device-message-life-cycle"/>.
        /// </remarks>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <param name="lockToken">The message lockToken.</param>
        /// <exception cref="IotHubClientException">Thrown and <see cref="IotHubClientException.StatusCode"/> is set to <see cref="IotHubStatusCode.NetworkErrors"/>
        /// when the operation has been canceled. The inner exception will be <see cref="OperationCanceledException"/>.</exception>
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
        /// Deletes the received message from the service's cloud to device message queue for this client and indicates to the server that the message could not be processed.
        /// </summary>
        /// <remarks>
        /// Messages cannot be rejected or abandoned over the MQTT protocol. For more details, see
        /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d#the-cloud-to-device-message-life-cycle"/>.
        /// </remarks>
        /// <param name="message">The message to reject.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="IotHubClientException">Thrown and <see cref="IotHubClientException.StatusCode"/> is set to <see cref="IotHubStatusCode.NetworkErrors"/>
        /// when the operation has been canceled. The inner exception will be <see cref="OperationCanceledException"/>.</exception>
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
        /// Sets a new delegate for the named method.
        /// </summary>
        /// <remarks>
        /// If a delegate is already associated with
        /// the named method, it will be replaced with the new delegate.
        /// A method handler can be unset by setting <paramref name="methodHandler"/> to null.
        /// </remarks>
        /// <param name="methodName">The name of the method to associate with the delegate.</param>
        /// <param name="methodHandler">The delegate to be used when a method with the given name is called by the cloud service.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public async Task SetMethodHandlerAsync(
            string methodName,
            Func<DirectMethodRequest, object, Task<DirectMethodResponse>> methodHandler,
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
                    _deviceMethods[methodName] = new Tuple<Func<DirectMethodRequest, object, Task<DirectMethodResponse>>, object>(methodHandler, userContext);
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
        /// </summary>
        /// <remarks>
        /// If a default delegate is already registered it will replace with the new delegate.
        /// A method handler can be unset by setting <paramref name="methodHandler"/> to null.
        /// </remarks>
        /// <param name="methodHandler">The delegate to be used when a method is called by the cloud service and there is
        /// no delegate registered for that method name.</param>
        /// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public async Task SetMethodDefaultHandlerAsync(
            Func<DirectMethodRequest, object, Task<DirectMethodResponse>> methodHandler,
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

                    _deviceDefaultMethodCallback = new Tuple<Func<DirectMethodRequest, object, Task<DirectMethodResponse>>, object>(methodHandler, userContext);
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
        /// Retrieve the twin properties for the current client. The client instance must be opened already.
        /// </summary>
        /// <remarks>
        /// This API gives you the client's view of the twin. For more information on twins in IoT hub, see <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-device-twins"/>.
        /// </remarks>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="InvalidOperationException">Thrown if the client instance is not opened already.</exception>
        /// <exception cref="IotHubClientException">Thrown and <see cref="IotHubClientException.StatusCode"/> is set to <see cref="IotHubStatusCode.NetworkErrors"/>
        /// when the operation has been canceled. The inner exception will be <see cref="OperationCanceledException"/>.</exception>
        /// <returns>The twin object for the current client.</returns>
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
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="IotHubClientException">Thrown and <see cref="IotHubClientException.StatusCode"/> is set to <see cref="IotHubStatusCode.NetworkErrors"/>
        /// when the operation has been canceled. The inner exception will be <see cref="OperationCanceledException"/>.</exception>
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
        /// Set a callback that will be called whenever the client receives a desired state update
        /// from the service. A desired property handler can be unset by setting <paramref name="callback"/> to null.
        /// </summary>
        /// <remarks>
        /// This has the side-effect of subscribing to the PATCH topic on the service.
        /// </remarks>
        /// <param name="callback">Callback to call after the state update has been received and applied.</param>
        /// <param name="userContext">Context object that will be passed into callback.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
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
        /// Close the client instance.
        /// </summary>
        /// <remarks>
        /// The instance can be re-opened after closing and before disposing.
        /// </remarks>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
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
        /// Releases the unmanaged resources used by the client and optionally disposes of the managed resources.
        /// </summary>
        /// <remarks>
        /// The method <see cref="CloseAsync(CancellationToken)"/> should be called before disposing.
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the client and allows for any derived class to override and
        /// provide custom implementation.
        /// </summary>
        /// <param name="disposing">Setting to true will release both managed and unmanaged resources. Setting to
        /// false will only release the unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                InnerHandler?.Dispose();
                _methodsSemaphore?.Dispose();
                _twinDesiredPropertySemaphore?.Dispose();
            }
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

                if (ConnectionStatusInfo.Status != status
                    || ConnectionStatusInfo.ChangeReason != reason)
                {
                    ConnectionStatusInfo = new ConnectionStatusInfo(status, reason);
                    _connectionStatusChangeHandler?.Invoke(ConnectionStatusInfo);
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, status, reason, nameof(OnConnectionStatusChanged));
            }
        }

        /// <summary>
        /// The delegate for handling direct methods received from service.
        /// </summary>
        internal async Task OnMethodCalledAsync(DirectMethodRequest directMethodRequest)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, directMethodRequest?.MethodName, directMethodRequest, nameof(OnMethodCalledAsync));

            if (directMethodRequest == null)
            {
                return;
            }

            Tuple<Func<DirectMethodRequest, object, Task<DirectMethodResponse>>, object> callbackContextPair = null;
            DirectMethodResponse directMethodResponse = null;

            await _methodsSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!_deviceMethods.TryGetValue(directMethodRequest.MethodName, out callbackContextPair))
                {
                    callbackContextPair = _deviceDefaultMethodCallback;
                }
            }
            catch (Exception ex) when (!Fx.IsFatal(ex))
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, ex, nameof(OnMethodCalledAsync));

                directMethodResponse = new DirectMethodResponse()
                {
                    Status = (int)DirectMethodResponseStatusCode.BadRequest,
                    RequestId = directMethodRequest.RequestId,
                };

                await SendDirectMethodResponseAsync(directMethodResponse).ConfigureAwait(false);

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
                directMethodResponse = new DirectMethodResponse()
                {
                    Status = (int)DirectMethodResponseStatusCode.MethodNotImplemented,
                    RequestId = directMethodRequest.RequestId,
                };
            }
            else
            {
                try
                {
                    Func<DirectMethodRequest, object, Task<DirectMethodResponse>> userSuppliedCallback = callbackContextPair.Item1;
                    object userSuppliedContext = callbackContextPair.Item2;

                    directMethodResponse = await userSuppliedCallback
                        .Invoke(directMethodRequest, userSuppliedContext)
                        .ConfigureAwait(false);

                    directMethodResponse.RequestId = directMethodRequest.RequestId;
                }
                catch (Exception ex)
                {
                    if (Logging.IsEnabled)
                        Logging.Error(this, ex, nameof(OnMethodCalledAsync));

                    directMethodResponse = new DirectMethodResponse()
                    {
                        Status = (int)DirectMethodResponseStatusCode.UserCodeException,
                        RequestId = directMethodRequest.RequestId,
                    };
                }
            }

            await SendDirectMethodResponseAsync(directMethodResponse).ConfigureAwait(false);

            if (Logging.IsEnabled)
                Logging.Exit(this, directMethodRequest.MethodName, directMethodRequest, nameof(OnMethodCalledAsync));
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

        private async Task SendDirectMethodResponseAsync(DirectMethodResponse directMethodResponse, CancellationToken cancellationToken = default)
        {
            try
            {
                await InnerHandler.SendMethodResponseAsync(directMethodResponse, cancellationToken).ConfigureAwait(false);
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

        private protected static ClientPipelineBuilder BuildPipeline()
        {
            var transporthandlerFactory = new TransportHandlerFactory();
            ClientPipelineBuilder pipelineBuilder = new ClientPipelineBuilder()
                .With((ctx, innerHandler) => new RetryDelegatingHandler(ctx, innerHandler))
                .With((ctx, innerHandler) => new ErrorDelegatingHandler(ctx, innerHandler))
                .With((ctx, innerHandler) => new TransportDelegatingHandler(ctx, innerHandler))
                .With((ctx, innerHandler) => transporthandlerFactory.Create(ctx));

            return pipelineBuilder;
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
    }
}
