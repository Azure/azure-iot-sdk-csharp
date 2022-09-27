// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Transport;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Contains methods that a device can use to send messages to and receive from the service.
    /// </summary>
    /// <threadsafety static="true" instance="true" />
    public class IotHubDeviceClient : IotHubBaseClient
    {
        // Cloud-to-device message callback information
        private readonly SemaphoreSlim _deviceReceiveMessageSemaphore = new(1, 1);

        private volatile Func<Message, Task<MessageAcknowledgement>> _deviceReceiveMessageCallback;

        // File upload operation
        private readonly HttpTransportHandler _fileUploadHttpTransportHandler;

        /// <summary>
        /// Creates a disposable <c>IotHubDeviceClient</c> from the specified connection string.
        /// </summary>
        /// <param name="connectionString">The connection string based on shared access key used in API calls which allows the device to communicate with IoT Hub.</param>
        /// <param name="options">The options that allow configuration of the device client instance during initialization.</param>
        /// <returns>A disposable client instance.</returns>
        /// <exception cref="ArgumentNullException">Either <paramref name="connectionString"/> is null,
        /// or the IoT hub host name or device Id in the connection string is null.</exception>
        /// <exception cref="ArgumentException">Either <paramref name="connectionString"/> is an empty string or consists only of white-space characters,
        /// or the IoT hub host name or device Id in the connection string are an empty string or consist only of white-space characters.</exception>
        /// <exception cref="ArgumentException">Neither shared access key nor shared access signature were presented for authentication.</exception>
        /// <exception cref="ArgumentException">A module Id was specified in the connection string. <see cref="IotHubModuleClient"/> should be used for modules.</exception>
        public IotHubDeviceClient(string connectionString, IotHubClientOptions options = default)
            : this(new IotHubConnectionCredentials(connectionString), options)
        {
        }

        /// <summary>
        /// Creates a disposable <c>IotHubDeviceClient</c> from the specified parameters.
        /// </summary>
        /// <param name="hostName">The fully-qualified DNS host name of IoT hub.</param>
        /// <param name="authenticationMethod">The authentication method that is used.</param>
        /// <param name="options">The options that allow configuration of the device client instance during initialization.</param>
        /// <returns>A disposable client instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="hostName"/>, device Id or <paramref name="authenticationMethod"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="hostName"/> or device Id are an empty string or consist only of white-space characters.</exception>
        /// <exception cref="ArgumentException">Neither shared access key, shared access signature or X509 certificates were presented for authentication.</exception>
        /// <exception cref="ArgumentException">Either shared access key or shared access signature were presented together with X509 certificates for authentication.</exception>
        /// <exception cref="ArgumentException"><see cref="DeviceAuthenticationWithX509Certificate.ChainCertificates"/> is used over a protocol other than MQTT over TCP or AMQP over TCP></exception>
        /// <exception cref="IotHubClientException"><see cref="DeviceAuthenticationWithX509Certificate.ChainCertificates"/> could not be installed.</exception>
        /// <exception cref="ArgumentException">A module Id was specified in the connection string. <see cref="IotHubModuleClient"/> should be used for modules.</exception>
        public IotHubDeviceClient(string hostName, IAuthenticationMethod authenticationMethod, IotHubClientOptions options = default)
            : this(new IotHubConnectionCredentials(authenticationMethod, hostName, options?.GatewayHostName), options)
        {
        }

        private IotHubDeviceClient(IotHubConnectionCredentials iotHubConnectionCredentials, IotHubClientOptions options)
            : base(iotHubConnectionCredentials, options)
        {
            // Validate
            if (iotHubConnectionCredentials.ModuleId != null)
            {
                throw new InvalidOperationException("A module Id was specified in the authentication credentials supplied - please use IotHubModuleClient for modules.");
            }

            // Validate certificates
            if (IotHubConnectionCredentials.AuthenticationMethod is DeviceAuthenticationWithX509Certificate x509CertificateAuth
                && x509CertificateAuth.ChainCertificates != null)
            {
                if (_clientOptions.TransportSettings.Protocol != IotHubClientTransportProtocol.Tcp)
                {
                    throw new ArgumentException("Certificate chains for devices are only supported on MQTT over TCP and AMQP over TCP.");
                }
            }

            _fileUploadHttpTransportHandler = new HttpTransportHandler(PipelineContext, _clientOptions.FileUploadTransportSettings);

            if (Logging.IsEnabled)
                Logging.CreateClient(
                    this,
                    $"HostName={IotHubConnectionCredentials.HostName};DeviceId={IotHubConnectionCredentials.DeviceId}",
                    _clientOptions);
        }

        /// <summary>
        /// Sets the listener for receiving a message from the device queue using a cancellation token.
        /// IotHubDeviceClient instance must be opened already.
        /// </summary>
        /// <remarks>
        /// Calling this API more than once will result in the listener set last overwriting any previously set listener.
        /// A cloud-to-device message handler can be unset by setting <paramref name="messageHandler"/> to null.
        /// </remarks>
        /// <param name="messageHandler">The listener to be used when a cloud-to-device message is received by the client.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="InvalidOperationException">Thrown if DeviceClient instance is not opened already.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public async Task SetReceiveMessageHandlerAsync(
            Func<Message, Task<MessageAcknowledgement>> messageHandler,
            CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, messageHandler, nameof(SetReceiveMessageHandlerAsync));

            cancellationToken.ThrowIfCancellationRequested();

            // Wait to acquire the _deviceReceiveMessageSemaphore. This ensures that concurrently invoked
            // SetReceiveMessageHandlerAsync calls are invoked in a thread-safe manner.
            await _deviceReceiveMessageSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // If a ReceiveMessageCallback is already set on the DeviceClient, calling SetReceiveMessageHandlerAsync
                // again will cause the delegate to be overwritten.
                if (messageHandler != null)
                {
                    // If this is the first time the delegate is being registered, then the telemetry downlink will be enabled.
                    await EnableReceiveMessageAsync(cancellationToken).ConfigureAwait(false);
                    _deviceReceiveMessageCallback = messageHandler;
                }
                else
                {
                    // If a null delegate is passed, it will disable the callback triggered on receiving messages from the service.
                    _deviceReceiveMessageCallback = null;
                    await DisableReceiveMessageAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                _deviceReceiveMessageSemaphore.Release();

                if (Logging.IsEnabled)
                    Logging.Exit(this, messageHandler, nameof(SetReceiveMessageHandlerAsync));
            }
        }

        /// <summary>
        /// Get a file upload SAS URI which the Azure Storage SDK can use to upload a file to blob for this device
        /// </summary>
        /// <remarks>
        /// See <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-file-upload#initialize-a-file-upload">this documentation for more details</see>.
        /// </remarks>
        /// <param name="request">The request details for getting the SAS URI, including the destination blob name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The file upload details to be used with the Azure Storage SDK in order to upload a file from this device.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task<FileUploadSasUriResponse> GetFileUploadSasUriAsync(
            FileUploadSasUriRequest request,
            CancellationToken cancellationToken = default)
        {
            return _fileUploadHttpTransportHandler.GetFileUploadSasUriAsync(request, cancellationToken);
        }

        /// <summary>
        /// Notify IoT hub that a device's file upload has finished.
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-file-upload#notify-iot-hub-of-a-completed-file-upload" />.
        /// <param name="notification">The notification details, including if the file upload succeeded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task CompleteFileUploadAsync(FileUploadCompletionNotification notification, CancellationToken cancellationToken = default)
        {
            return _fileUploadHttpTransportHandler.CompleteFileUploadAsync(notification, cancellationToken);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _deviceReceiveMessageSemaphore?.Dispose();
                _fileUploadHttpTransportHandler?.Dispose();
            }

            // Call the base class implementation.
            base.Dispose(disposing);
        }

        private protected override void AddToPipelineContext()
        {
            PipelineContext.DeviceEventCallback = OnDeviceMessageReceivedAsync;
        }

        // The delegate for handling c2d messages received
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
                if (_deviceReceiveMessageCallback != null)
                {
                    MessageAcknowledgement response = await _deviceReceiveMessageCallback.Invoke(message).ConfigureAwait(false);

                    try
                    {
                        switch (response)
                        {
                            case MessageAcknowledgement.Complete:
                                await InnerHandler.CompleteMessageAsync(message.LockToken, CancellationToken.None).ConfigureAwait(false);
                                break;

                            case MessageAcknowledgement.Abandon:
                                await InnerHandler.AbandonMessageAsync(message.LockToken, CancellationToken.None).ConfigureAwait(false);
                                break;

                            case MessageAcknowledgement.Reject:
                                await InnerHandler.RejectMessageAsync(message.LockToken, CancellationToken.None).ConfigureAwait(false);
                                break;
                        }
                    }
                    catch (Exception ex) when (Logging.IsEnabled)
                    {
                        Logging.Error(this, ex, nameof(OnDeviceMessageReceivedAsync));
                    }
                }
            }
            finally
            {
                _deviceReceiveMessageSemaphore.Release();
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, message, nameof(OnDeviceMessageReceivedAsync));
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
    }
}
