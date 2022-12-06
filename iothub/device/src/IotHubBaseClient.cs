// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Transport;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Contains methods that a client can use to send messages to and receive messages from the service,
    /// respond to direct method invocations from the service, and send and receive twin property updates.
    /// </summary>
    public abstract class IotHubBaseClient : IAsyncDisposable
    {
        private readonly SemaphoreSlim _methodsSemaphore = new(1, 1);
        private readonly SemaphoreSlim _twinDesiredPropertySemaphore = new(1, 1);
        private readonly SemaphoreSlim _receiveMessageSemaphore = new(1, 1);

        private volatile Func<IncomingMessage, Task<MessageAcknowledgement>> _receiveMessageCallback;

        // Method callback information
        private bool _isDeviceMethodEnabled;

        private volatile Func<DirectMethodRequest, Task<DirectMethodResponse>> _deviceDefaultMethodCallback;

        // Twin property update request callback information
        private bool _twinPatchSubscribedWithService;

        private Func<DesiredProperties, Task> _desiredPropertyUpdateCallback;

        private protected readonly IotHubClientOptions _clientOptions;

        internal IotHubBaseClient(
            IotHubConnectionCredentials iotHubConnectionCredentials,
            IotHubClientOptions iotHubClientOptions)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, iotHubClientOptions?.TransportSettings, nameof(IotHubBaseClient) + "_ctor");

            _clientOptions = iotHubClientOptions != null
                ? iotHubClientOptions.Clone()
                : new();

            IotHubConnectionCredentials = iotHubConnectionCredentials;

            ClientPipelineBuilder pipelineBuilder = BuildPipeline();

            PipelineContext = new PipelineContext
            {
                IotHubConnectionCredentials = IotHubConnectionCredentials,
                ProductInfo = _clientOptions.UserAgentInfo,
                ModelId = _clientOptions.ModelId,
                PayloadConvention = _clientOptions.PayloadConvention,
                IotHubClientTransportSettings = _clientOptions.TransportSettings,
                MethodCallback = OnMethodCalledAsync,
                DesiredPropertyUpdateCallback = OnDesiredStatePatchReceived,
                ConnectionStatusChangeHandler = OnConnectionStatusChanged,
                MessageEventCallback = OnMessageReceivedAsync,
            };

            InnerHandler = pipelineBuilder.Build(PipelineContext, _clientOptions.RetryPolicy);

            if (Logging.IsEnabled)
                Logging.Exit(this, _clientOptions.TransportSettings, nameof(IotHubBaseClient) + "_ctor");
        }

        /// <summary>
        /// The latest connection status information since the last status change.
        /// </summary>
        public ConnectionStatusInfo ConnectionStatusInfo { get; protected private set; } = new();

        /// <summary>
        /// The callback to be executed each time connection status change notification is received.
        /// </summary>
        /// <remarks>
        /// All of requests will be processed as they arrive. If you put async code within this
        /// callback, you'll need to handle exceptions that could originate in there.
        /// </remarks>
        /// <example>
        /// <code language="csharp">
        /// deviceClient.ConnectionStatusChangeCallback = OnConnectionStatusChanged;
        /// //...
        ///
        /// public void OnConnectionStatusChanged(ConnectionStatusInfo connectionStatusInfo)
        /// {
        ///     // Add connection status changed logic as needed
        /// }
        /// </code>
        /// </example>
        public Action<ConnectionStatusInfo> ConnectionStatusChangeCallback { get; set; }

        internal IotHubConnectionCredentials IotHubConnectionCredentials { get; private set; }

        internal IDelegatingHandler InnerHandler { get; set; }

        private protected PipelineContext PipelineContext { get; private set; }

        /// <summary>
        /// Open the client instance. Must be done before any operation can begin.
        /// </summary>
        /// <remarks>
        /// This client can be re-opened after it has been closed, but cannot be re-opened after it has
        /// been disposed. Subscriptions to cloud to device messages/twin/methods do not persist when
        /// re-opening a client.
        /// </remarks>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public async Task OpenAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await InnerHandler.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a telemetry message to IoT hub.
        /// </summary>
        /// <remarks>
        /// The client instance must be opened already.
        /// <para>
        /// In case of a transient issue, retrying the operation should work. In case of a non-transient issue, inspect
        /// the error details and take steps accordingly.
        /// </para>
        /// </remarks>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is null.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the client instance is not opened already.</exception>
        /// <exception cref="IotHubClientException">Thrown if an error occurs when communicating with IoT hub service.</exception>
        public async Task SendTelemetryAsync(TelemetryMessage message, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(message, nameof(message));
            cancellationToken.ThrowIfCancellationRequested();

            if (_clientOptions?.SdkAssignsMessageId == SdkAssignsMessageId.WhenUnset && message.MessageId == null)
            {
                message.MessageId = Guid.NewGuid().ToString();
            }

            message.PayloadConvention = _clientOptions.PayloadConvention;
            message.ContentType = _clientOptions.PayloadConvention.PayloadSerializer.ContentType;
            message.ContentEncoding = _clientOptions.PayloadConvention.PayloadEncoder.ContentEncoding.WebName;

            try
            {
                await InnerHandler.SendTelemetryAsync(message, cancellationToken).ConfigureAwait(false);
            }
            catch (SocketException socketException)
            {
                throw new IotHubClientException(socketException.Message, IotHubClientErrorCode.NetworkErrors, socketException);
            }
            catch (WebSocketException webSocketException)
            {
                throw new IotHubClientException(webSocketException.Message, IotHubClientErrorCode.NetworkErrors, webSocketException);
            }
        }

        /// <summary>
        /// Sends a batch of telemetry message to IoT hub.
        /// </summary>
        /// <remarks>
        /// The client instance must be opened already.
        /// <para>
        /// This operation is supported only over AMQP.
        /// </para>
        /// <para>
        /// This operation is not supported over MQTT and will result in an <see cref="InvalidOperationException"/>.
        /// </para>
        /// <para>
        /// For more information on IoT Edge module routing for <see cref="IotHubModuleClient"/> see
        /// <see href="https://docs.microsoft.com/azure/iot-edge/module-composition?view=iotedge-2018-06#declare-routes"/>.
        /// </para>
        /// </remarks>
        /// <param name="messages">An <see cref="IEnumerable{Message}"/> set of message objects.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="InvalidOperationException">Thrown if the client instance is not opened already.</exception>
        /// <exception cref="InvalidOperationException">When this method is called when the client is configured to use MQTT.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public async Task SendTelemetryBatchAsync(IEnumerable<TelemetryMessage> messages, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrEmpty(messages, nameof(messages));
            cancellationToken.ThrowIfCancellationRequested();

            foreach (TelemetryMessage message in messages)
            {
                message.PayloadConvention = _clientOptions.PayloadConvention;
                message.ContentType = _clientOptions.PayloadConvention.PayloadSerializer.ContentType;
                message.ContentEncoding = _clientOptions.PayloadConvention.PayloadEncoder.ContentEncoding.WebName;

                if (_clientOptions?.SdkAssignsMessageId == SdkAssignsMessageId.WhenUnset)
                {
                    message.MessageId ??= Guid.NewGuid().ToString();
                }
            }

            await InnerHandler.SendTelemetryBatchAsync(messages, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sets a callback for receiving a message from the device or module queue using a cancellation token.
        /// This instance must be opened already.
        /// </summary>
        /// <remarks>
        /// Calling this API more than once will result in the callback set last overwriting any previously set callback.
        /// A method callback can be unset by setting <paramref name="messageCallback"/> to null.
        /// This user-supplied callback is awaited by the SDK. All of requests will be processed as they arrive.
        /// Exceptions thrown within the callback will be caught and logged by the SDK internally.
        /// </remarks>
        /// <param name="messageCallback">The callback to be invoked when a cloud-to-device message is received by the client.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="InvalidOperationException">Thrown if instance is not opened already.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public async Task SetIncomingMessageCallbackAsync(
            Func<IncomingMessage, Task<MessageAcknowledgement>> messageCallback,
            CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, messageCallback, nameof(SetIncomingMessageCallbackAsync));

            cancellationToken.ThrowIfCancellationRequested();

            // Wait to acquire the _deviceReceiveMessageSemaphore. This ensures that concurrently invoked
            // SetMessageCallbackAsync calls are invoked in a thread-safe manner.
            await _receiveMessageSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // If a callback is already set on the client, calling SetMessageCallbackAsync
                // again will cause the callback to be overwritten.
                if (messageCallback != null)
                {
                    // If this is the first time the callback is being registered, then the telemetry downlink will be enabled.
                    await EnableReceiveMessageAsync(cancellationToken).ConfigureAwait(false);
                    _receiveMessageCallback = new Func<IncomingMessage, Task<MessageAcknowledgement>>(messageCallback);
                }
                else
                {
                    // If a null callback is passed, it will disable the callback triggered on receiving messages from the service.
                    _receiveMessageCallback = null;
                    await DisableReceiveMessageAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                _receiveMessageSemaphore.Release();

                if (Logging.IsEnabled)
                    Logging.Exit(this, messageCallback, nameof(SetIncomingMessageCallbackAsync));
            }
        }

        /// <summary>
        /// Sets the callback for all direct method calls from the service.
        /// This instance must be opened already.
        /// </summary>
        /// <remarks>
        /// Calling this API more than once will result in the callback set last overwriting any previously set callback.
        /// A method callback can be unset by setting <paramref name="directMethodCallback"/> to null.
        /// This user-supplied callback is awaited by the SDK. All of requests will be processed as they arrive.
        /// Exceptions thrown within the callback will be caught and logged by the SDK internally.
        /// </remarks>
        /// <param name="directMethodCallback">The callback to be invoked when any method is invoked by the cloud service.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public async Task SetDirectMethodCallbackAsync(
            Func<DirectMethodRequest, Task<DirectMethodResponse>> directMethodCallback,
            CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, directMethodCallback, nameof(SetDirectMethodCallbackAsync));

            cancellationToken.ThrowIfCancellationRequested();

            await _methodsSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (directMethodCallback != null)
                {
                    await HandleMethodEnableAsync(cancellationToken).ConfigureAwait(false);
                    _deviceDefaultMethodCallback = directMethodCallback;
                }
                else
                {
                    _deviceDefaultMethodCallback = null;
                    await HandleMethodDisableAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                _methodsSemaphore.Release();

                if (Logging.IsEnabled)
                    Logging.Exit(this, directMethodCallback, nameof(SetDirectMethodCallbackAsync));
            }
        }

        /// <summary>
        /// Retrieve the twin properties for the current client.
        /// </summary>
        /// <remarks>
        /// The client instance must be opened already.
        /// <para>
        /// This API gives you the client's view of the twin. For more information on twins in IoT hub, see <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-device-twins"/>.
        /// </para>
        /// </remarks>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="InvalidOperationException">Thrown if the client instance is not opened already.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        /// <returns>The twin object for the current client.</returns>
        public async Task<TwinProperties> GetTwinPropertiesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return await InnerHandler.GetTwinAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Push reported property changes up to the service.
        /// </summary>
        /// <param name="reportedProperties">Reported properties to push</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The new version of the updated twin if the update was successful.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public async Task<long> UpdateReportedPropertiesAsync(ReportedProperties reportedProperties, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(reportedProperties, nameof(reportedProperties));
            cancellationToken.ThrowIfCancellationRequested();

            reportedProperties.PayloadConvention = _clientOptions.PayloadConvention;
            return await InnerHandler.UpdateReportedPropertiesAsync(reportedProperties, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Set a callback that will be called whenever the client receives a desired state update
        /// from the service. The client instance must be opened already.
        /// </summary>
        /// <remarks>
        /// Calling this API more than once will result in the callback set last overwriting any previously set callback.
        /// This user-supplied callback is "fire-and-forget" and the SDK doesn't wait on it. All of requests will be processed as they arrive.
        /// The users are responsible to handle exceptions within their callback implementation.
        /// A method callback can be unset by setting <paramref name="callback"/> to null.
        ///  <para>
        /// This has the side-effect of subscribing to the PATCH topic on the service.
        ///  </para>
        /// </remarks>
        /// <param name="callback">The callback to be invoked when a desired property update is received from the service.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public async Task SetDesiredPropertyUpdateCallbackAsync(
            Func<DesiredProperties, Task> callback,
            CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, callback, nameof(SetDesiredPropertyUpdateCallbackAsync));

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
            }
            finally
            {
                _twinDesiredPropertySemaphore.Release();

                if (Logging.IsEnabled)
                    Logging.Exit(this, callback, nameof(SetDesiredPropertyUpdateCallbackAsync));
            }
        }

        /// <summary>
        /// Close the client instance.
        /// </summary>
        /// <remarks>
        /// The instance can be re-opened after closing and before disposing. However, subscriptions
        /// to cloud to device messages/twin/methods do not persist when re-opening a client.
        /// </remarks>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public async Task CloseAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await InnerHandler.CloseAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, "Closing and disposing", nameof(DisposeAsync));

            try
            {
                await CloseAsync(CancellationToken.None).ConfigureAwait(false);
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            catch (Exception ex) when (Logging.IsEnabled)
            {
                Logging.Error(this, ex, nameof(DisposeAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, "Closing and disposing", nameof(DisposeAsync));
            }
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
        /// The callback for handling disrupted connection/links in the transport layer.
        /// </summary>
        internal void OnConnectionStatusChanged(ConnectionStatusInfo connectionStatusInfo)
        {
            ConnectionStatus status = connectionStatusInfo.Status;
            ConnectionStatusChangeReason reason = connectionStatusInfo.ChangeReason;

            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, status, reason, nameof(OnConnectionStatusChanged));

                if (ConnectionStatusInfo.Status != status
                    || ConnectionStatusInfo.ChangeReason != reason)
                {
                    ConnectionStatusInfo = new ConnectionStatusInfo(status, reason);
                    ConnectionStatusChangeCallback?.Invoke(ConnectionStatusInfo);
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, status, reason, nameof(OnConnectionStatusChanged));
            }
        }

        /// <summary>
        /// The callback for handling direct methods received from service.
        /// </summary>
        internal async Task OnMethodCalledAsync(DirectMethodRequest directMethodRequest)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, directMethodRequest?.MethodName, directMethodRequest, nameof(OnMethodCalledAsync));

            if (directMethodRequest == null
                || _deviceDefaultMethodCallback == null)
            {
                if (Logging.IsEnabled)
                    Logging.Error(
                        this,
                        $"Direct method {directMethodRequest?.RequestId} has request is null '{directMethodRequest == null}' and callback is null '{_deviceDefaultMethodCallback != null}'",
                        nameof(OnMethodCalledAsync));
                return;
            }

            try
            {
                DirectMethodResponse directMethodResponse = await _deviceDefaultMethodCallback
                    .Invoke(directMethodRequest)
                    .ConfigureAwait(false);

                directMethodResponse.RequestId = directMethodRequest.RequestId;
                directMethodResponse.PayloadConvention = _clientOptions.PayloadConvention;

                await SendDirectMethodResponseAsync(directMethodResponse).ConfigureAwait(false);
            }
            catch (Exception ex) when (Logging.IsEnabled)
            {
                Logging.Error(this, $"User code threw exception for request Id {directMethodRequest.RequestId}: {ex}", nameof(OnMethodCalledAsync));
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, directMethodRequest.MethodName, directMethodRequest, nameof(OnMethodCalledAsync));
            }
        }

        internal void OnDesiredStatePatchReceived(DesiredProperties patch)
        {
            if (_desiredPropertyUpdateCallback == null)
            {
                return;
            }

            if (Logging.IsEnabled)
                Logging.Info(this, patch.GetSerializedString(), nameof(OnDesiredStatePatchReceived));

            _ = _desiredPropertyUpdateCallback.Invoke(patch);
        }

        private async Task SendDirectMethodResponseAsync(DirectMethodResponse directMethodResponse, CancellationToken cancellationToken = default)
        {
            await InnerHandler.SendMethodResponseAsync(directMethodResponse, cancellationToken).ConfigureAwait(false);
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
            if (!_isDeviceMethodEnabled || _deviceDefaultMethodCallback != null)
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

        internal async Task<MessageAcknowledgement> OnMessageReceivedAsync(IncomingMessage message)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, message, nameof(OnMessageReceivedAsync));

            Debug.Assert(message != null, "Received a null message");

            // Grab this semaphore so that there is no chance that the _receiveMessageCallback instance is set in between the read of the
            // item1 and the read of the item2
            await _receiveMessageSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                Func<IncomingMessage, Task<MessageAcknowledgement>> callback = _receiveMessageCallback;

                if (callback != null)
                {
                    return await callback.Invoke(message).ConfigureAwait(false);
                }

                // The SDK should only receive messages when the user sets a listener, so this should never happen.
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Received a message when no listener was set. Abandoning message with message Id: {message.MessageId}.", nameof(OnMessageReceivedAsync));

                return MessageAcknowledgement.Abandon;
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Abandoning message with message Id: {message.MessageId} because user code threw exception: {ex}.", nameof(OnMessageReceivedAsync));

                return MessageAcknowledgement.Abandon;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, message, nameof(OnMessageReceivedAsync));

                _receiveMessageSemaphore.Release();
            }
        }

        private Task EnableReceiveMessageAsync(CancellationToken cancellationToken = default)
        {
            // The telemetry downlink needs to be enabled only for the first time that the _receiveMessageCallback callback is set.
            return _receiveMessageCallback == null
                ? InnerHandler.EnableReceiveMessageAsync(cancellationToken)
                : Task.CompletedTask;
        }

        private Task DisableReceiveMessageAsync(CancellationToken cancellationToken = default)
        {
            // The telemetry downlink should be disabled only after _receiveMessageCallback callback has been removed.
            return _receiveMessageCallback == null
                ? InnerHandler.DisableReceiveMessageAsync(cancellationToken)
                : Task.CompletedTask;
        }
    }
}
