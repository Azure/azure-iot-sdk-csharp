// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Transport;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Contains methods that a device can use to send messages to and receive from the service.
    /// </summary>
    /// <threadsafety static="true" instance="true" />
    public class IotHubDeviceClient : IDisposable
    {
        /// <summary>
        /// Default operation timeout.
        /// </summary>
        public const uint DefaultOperationTimeoutInMilliseconds = 4 * 60 * 1000;

        /// <summary>
        /// Creates a disposable <c>IotHubDeviceClient</c> from the specified connection string.
        /// </summary>
        /// <param name="connectionString">The connection string based on shared access key used in API calls which allows the device to communicate with IoT Hub.</param>
        /// <param name="options">The options that allow configuration of the device client instance during initialization.</param>
        /// <returns>A disposable <c>IotHubDeviceClient</c> instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connectionString"/>, IoT hub host name or device Id is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="connectionString"/>, IoT hub host name or device Id are an empty string or consist only of white-space characters.</exception>
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
        /// <returns>A disposable <c>IotHubDeviceClient</c> instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="hostName"/>, device Id or <paramref name="authenticationMethod"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="hostName"/> or device Id are an empty string or consist only of white-space characters.</exception>
        /// <exception cref="ArgumentException">Neither shared access key, shared access signature or X509 certificates were presented for authentication.</exception>
        /// <exception cref="ArgumentException">Either shared access key or shared access signature were presented together with X509 certificates for authentication.</exception>
        /// <exception cref="ArgumentException"><see cref="DeviceAuthenticationWithX509Certificate"/> is used but <see cref="DeviceAuthenticationWithX509Certificate.Certificate"/> is null.</exception>
        /// <exception cref="ArgumentException"><see cref="DeviceAuthenticationWithX509Certificate.ChainCertificates"/> is used over a protocol other than MQTT over TCP or AMQP over TCP></exception>
        /// <exception cref="IotHubClientException"><see cref="DeviceAuthenticationWithX509Certificate.ChainCertificates"/> could not be installed.</exception>
        /// <exception cref="ArgumentException">A module Id was specified in the connection string. <see cref="IotHubModuleClient"/> should be used for modules.</exception>
        public IotHubDeviceClient(string hostName, IAuthenticationMethod authenticationMethod, IotHubClientOptions options = default)
            : this(new IotHubConnectionCredentials(authenticationMethod, hostName, options?.GatewayHostName), options)
        {
        }

        private IotHubDeviceClient(IotHubConnectionCredentials iotHubConnectionCredentials, IotHubClientOptions options)
        {
            if (iotHubConnectionCredentials.ModuleId != null)
            {
                throw new ArgumentException("A module Id was specified in the connection string - please use IotHubModuleClient for modules.");
            }

            // Validate certs.
            if (iotHubConnectionCredentials.AuthenticationMethod is DeviceAuthenticationWithX509Certificate x509CertificateAuth
                && x509CertificateAuth.ChainCertificates != null)
            {
                if (options.TransportSettings is not IotHubClientAmqpSettings
                        && options.TransportSettings is not IotHubClientMqttSettings
                        || options.TransportSettings.Protocol != IotHubClientTransportProtocol.Tcp)
                {
                    throw new ArgumentException("Certificate chains are only supported on MQTT over TCP and AMQP over TCP.");
                }
            }

            // Make sure client options is initialized.
            if (options == default)
            {
                options = new();
            }

            InternalClient = new InternalClient(iotHubConnectionCredentials, options, null);

            if (Logging.IsEnabled)
                Logging.CreateClient(
                    InternalClient,
                    $"HostName={InternalClient.IotHubConnectionCredentials.HostName};DeviceId={InternalClient.IotHubConnectionCredentials.DeviceId}",
                    options);
        }

        /// <summary>
        /// Diagnostic sampling percentage value, [0-100];
        /// 0 means no message will carry on diagnostics info
        /// </summary>
        public int DiagnosticSamplingPercentage
        {
            get => InternalClient.DiagnosticSamplingPercentage;
            set => InternalClient.DiagnosticSamplingPercentage = value;
        }

        internal IDelegatingHandler InnerHandler
        {
            get => InternalClient.InnerHandler;
            set => InternalClient.InnerHandler = value;
        }

        internal InternalClient InternalClient { get; private set; }

        /// <summary>
        /// Sets the retry policy used in the operation retries.
        /// The change will take effect after any in-progress operations.
        /// </summary>
        /// <param name="retryPolicy">The retry policy. The default is
        /// <c>new ExponentialBackoff(int.MaxValue, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(100));</c></param>
        public void SetRetryPolicy(IRetryPolicy retryPolicy)
        {
            InternalClient.SetRetryPolicy(retryPolicy);
        }

        /// <summary>
        /// Sets a new delegate for the connection status changed callback. If a delegate is already associated,
        /// it will be replaced with the new delegate. Note that this callback will never be called if the client is configured to use
        /// HTTP, as that protocol is stateless.
        /// </summary>
        /// <param name="statusChangeHandler">The name of the method to associate with the delegate.</param>
        public void SetConnectionStatusChangeHandler(Action<ConnectionStatusInfo> statusChangeHandler)
            => InternalClient.SetConnectionStatusChangeHandler(statusChangeHandler);

        /// <summary>
        /// The latest connection status information since the last status change.
        /// </summary>
        public ConnectionStatusInfo ConnectionStatusInfo => InternalClient._connectionStatusInfo;

        /// <summary>
        /// Open the DeviceClient instance. Must be done before any operation can begin.
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// </summary>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task OpenAsync(CancellationToken cancellationToken = default) => InternalClient.OpenAsync(cancellationToken);

        /// <summary>
        /// Sends an event to IoT hub. DeviceClient instance must be opened already.
        /// </summary>
        /// <remarks>
        /// In case of a transient issue, retrying the operation should work. In case of a non-transient issue, inspect
        /// the error details and take steps accordingly.
        /// Please note that the list of exceptions is not exhaustive.
        /// </remarks>
        /// <param name="message">The message to send. Should be disposed after sending.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="ArgumentNullException">Thrown when a required parameter is null.</exception>
        /// <exception cref="IotHubClientException">Thrown and <see cref="IotHubClientException.StatusCode"/> is set to <see cref="IotHubStatusCode.NetworkErrors"/>
        /// when the operation has been canceled. The inner exception will be <see cref="OperationCanceledException"/>.</exception>
        /// <exception cref="IotHubClientException">Thrown and <see cref="IotHubClientException.StatusCode"/> is set to <see cref="IotHubStatusCode.NetworkErrors"/>
        /// if the client encounters a transient retryable exception. </exception>
        /// <exception cref="InvalidOperationException">Thrown if DeviceClient instance is not opened already.</exception>
        /// <exception cref="SocketException">Thrown if a socket error occurs.</exception>
        /// <exception cref="WebSocketException">Thrown if an error occurs when performing an operation on a WebSocket connection.</exception>
        /// <exception cref="IOException">Thrown if an I/O error occurs.</exception>
        /// <exception cref="ClosedChannelException">Thrown if the MQTT transport layer closes unexpectedly.</exception>
        /// <exception cref="IotHubClientException">Thrown if an error occurs when communicating with IoT hub service.
        /// If <see cref="IotHubClientException.IsTransient"/> is set to <c>true</c> then it is a transient exception.
        /// If <see cref="IotHubClientException.IsTransient"/> is set to <c>false</c> then it is a non-transient exception.</exception>
        public Task SendEventAsync(Message message, CancellationToken cancellationToken = default)
            => InternalClient.SendEventAsync(message, cancellationToken);

        /// <summary>
        /// Sends a batch of events to IoT hub. Use AMQP or HTTPs for a true batch operation. MQTT will just send the messages
        /// one after the other. DeviceClient instance must be opened already.
        /// </summary>
        /// <param name="messages">An <see cref="IEnumerable{Message}"/> set of message objects.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="InvalidOperationException">Thrown if DeviceClient instance is not opened already.</exception>
        /// <exception cref="IotHubClientException">Thrown and <see cref="IotHubClientException.StatusCode"/> is set to <see cref="IotHubStatusCode.NetworkErrors"/>
        /// when the operation has been canceled. The inner exception will be <see cref="OperationCanceledException"/>.</exception>
        public Task SendEventBatchAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
            => InternalClient.SendEventBatchAsync(messages, cancellationToken);

        /// <summary>
        /// Receive a message from the device queue using the cancellation token. DeviceClient instance must be opened already.
        /// After handling a received message, a client should call <see cref="CompleteMessageAsync(Message, CancellationToken)"/>,
        /// <see cref="AbandonMessageAsync(Message, CancellationToken)"/>, or <see cref="RejectMessageAsync(Message, CancellationToken)"/>,
        /// and then dispose the message.
        /// </summary>
        /// <remarks>
        /// You cannot reject or abandon messages over MQTT protocol. For more details, see
        /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d#the-cloud-to-device-message-life-cycle"/>.
        /// </remarks>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        /// <returns>The received message.</returns>
        public Task<Message> ReceiveMessageAsync(CancellationToken cancellationToken = default) => InternalClient.ReceiveMessageAsync(cancellationToken);

        /// <summary>
        /// Sets a new delegate for receiving a message from the device queue using a cancellation token.
        /// DeviceClient instance must be opened already.
        /// </summary>
        /// <remarks>
        /// After handling a received message, a client should call <see cref="CompleteMessageAsync(Message, CancellationToken)"/>,
        /// <see cref="AbandonMessageAsync(Message, CancellationToken)"/>, or <see cref="RejectMessageAsync(Message, CancellationToken)"/>,
        /// and then dispose the message.
        /// If a null delegate is passed, it will disable the callback triggered on receiving messages from the service.
        /// </remarks>
        /// <param name="messageHandler">The delegate to be used when a could to device message is received by the client.</param>
        /// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="InvalidOperationException">Thrown if DeviceClient instance is not opened already.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task SetReceiveMessageHandlerAsync(
            Func<Message, object, Task> messageHandler,
            object userContext,
            CancellationToken cancellationToken = default)
            => InternalClient.SetReceiveMessageHandlerAsync(messageHandler, userContext, cancellationToken);

        /// <summary>
        /// Deletes a received message from the device queue.
        /// </summary>
        /// <param name="lockToken">The message lockToken.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="IotHubClientException">Thrown and <see cref="IotHubClientException.StatusCode"/> is set to <see cref="IotHubStatusCode.NetworkErrors"/>
        /// when the operation has been canceled. The inner exception will be <see cref="OperationCanceledException"/>.</exception>
        public Task CompleteMessageAsync(string lockToken, CancellationToken cancellationToken = default)
            => InternalClient.CompleteMessageAsync(lockToken, cancellationToken);

        /// <summary>
        /// Deletes a received message from the device queue.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="IotHubClientException">Thrown and <see cref="IotHubClientException.StatusCode"/> is set to <see cref="IotHubStatusCode.NetworkErrors"/>
        /// when the operation has been canceled. The inner exception will be <see cref="OperationCanceledException"/>.</exception>
        public Task CompleteMessageAsync(Message message, CancellationToken cancellationToken = default)
            => InternalClient.CompleteMessageAsync(message, cancellationToken);

        /// <summary>
        /// Puts a received message back onto the device queue.
        /// </summary>
        /// <remarks>
        /// You cannot reject or abandon messages over MQTT protocol. For more details, see
        /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d#the-cloud-to-device-message-life-cycle"/>.
        /// </remarks>
        /// <param name="lockToken">The message lockToken.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="IotHubClientException">Thrown and <see cref="IotHubClientException.StatusCode"/> is set to <see cref="IotHubStatusCode.NetworkErrors"/>
        /// when the operation has been canceled. The inner exception will be <see cref="OperationCanceledException"/>.</exception>
        public Task AbandonMessageAsync(string lockToken, CancellationToken cancellationToken = default)
            => InternalClient.AbandonMessageAsync(lockToken, cancellationToken);

        /// <summary>
        /// Puts a received message back onto the device queue.
        /// </summary>
        /// <remarks>
        /// You cannot reject or abandon messages over MQTT protocol. For more details, see
        /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d#the-cloud-to-device-message-life-cycle"/>.
        /// </remarks>
        /// <param name="message">The message to abandon.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="IotHubClientException">Thrown and <see cref="IotHubClientException.StatusCode"/> is set to <see cref="IotHubStatusCode.NetworkErrors"/>
        /// when the operation has been canceled. The inner exception will be <see cref="OperationCanceledException"/>.</exception>
        public Task AbandonMessageAsync(Message message, CancellationToken cancellationToken = default)
            => InternalClient.AbandonMessageAsync(message, cancellationToken);

        /// <summary>
        /// Deletes a received message from the device queue and indicates to the server that the message could not be processed.
        /// </summary>
        /// <remarks>
        /// You cannot reject or abandon messages over MQTT protocol. For more details, see
        /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d#the-cloud-to-device-message-life-cycle"/>.
        /// </remarks>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <param name="lockToken">The message lockToken.</param>
        /// <exception cref="IotHubClientException">Thrown and <see cref="IotHubClientException.StatusCode"/> is set to <see cref="IotHubStatusCode.NetworkErrors"/>
        /// when the operation has been canceled. The inner exception will be <see cref="OperationCanceledException"/>.</exception>
        public Task RejectMessageAsync(string lockToken, CancellationToken cancellationToken = default)
            => InternalClient.RejectMessageAsync(lockToken, cancellationToken);

        /// <summary>
        /// Deletes a received message from the device queue and indicates to the server that the message could not be processed.
        /// </summary>
        /// <remarks>
        /// You cannot reject or abandon messages over MQTT protocol. For more details, see
        /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d#the-cloud-to-device-message-life-cycle"/>.
        /// </remarks>
        /// <param name="message">The message to reject.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="IotHubClientException">Thrown and <see cref="IotHubClientException.StatusCode"/> is set to <see cref="IotHubStatusCode.NetworkErrors"/>
        /// when the operation has been canceled. The inner exception will be <see cref="OperationCanceledException"/>.</exception>
        public Task RejectMessageAsync(Message message, CancellationToken cancellationToken = default)
            => InternalClient.RejectMessageAsync(message, cancellationToken);

        /// <summary>
        /// Sets a new delegate for the named method. If a delegate is already associated with
        /// the named method, it will be replaced with the new delegate.
        /// A method handler can be unset by passing a null MethodCallback.
        /// <param name="methodName">The name of the method to associate with the delegate.</param>
        /// <param name="methodHandler">The delegate to be used when a method with the given name is called by the cloud service.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        /// </summary>
        public Task SetMethodHandlerAsync(
            string methodName,
            Func<DirectMethodRequest, object, Task<DirectMethodResponse>> methodHandler,
            object userContext,
            CancellationToken cancellationToken = default)
            => InternalClient.SetMethodHandlerAsync(methodName, methodHandler, userContext, cancellationToken);

        /// <summary>
        /// Sets a new delegate that is called for a method that doesn't have a delegate registered for its name.
        /// If a default delegate is already registered it will replace with the new delegate.
        /// A method handler can be unset by passing a null MethodCallback.
        /// </summary>
        /// <param name="methodHandler">The delegate to be used when a method is called by the cloud service and there is
        /// no delegate registered for that method name.</param>
        /// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task SetMethodDefaultHandlerAsync(
            Func<DirectMethodRequest, object, Task<DirectMethodResponse>> methodHandler,
            object userContext,
            CancellationToken cancellationToken = default)
            => InternalClient.SetMethodDefaultHandlerAsync(methodHandler, userContext, cancellationToken);

        /// <summary>
        /// Retrieve the device twin properties for the current device. DeviceClient instance must be opened already.
        /// For the complete device twin object, use Microsoft.Azure.Devices.RegistryManager.GetTwinAsync(string deviceId).
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="InvalidOperationException">Thrown if DeviceClient instance is not opened already.</exception>
        /// <exception cref="IotHubClientException">Thrown and <see cref="IotHubClientException.StatusCode"/> is set to <see cref="IotHubStatusCode.NetworkErrors"/>
        /// when the operation has been canceled. The inner exception will be <see cref="OperationCanceledException"/>.</exception>
        /// <returns>The device twin object for the current device</returns>
        public Task<Twin> GetTwinAsync(CancellationToken cancellationToken = default) => InternalClient.GetTwinAsync(cancellationToken);

        /// <summary>
        /// Push reported property changes up to the service.
        /// </summary>
        /// <param name="reportedProperties">Reported properties to push</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="IotHubClientException">Thrown and <see cref="IotHubClientException.StatusCode"/> is set to <see cref="IotHubStatusCode.NetworkErrors"/>
        /// when the operation has been canceled. The inner exception will be <see cref="OperationCanceledException"/>.</exception>
        public Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties, CancellationToken cancellationToken = default)
            => InternalClient.UpdateReportedPropertiesAsync(reportedProperties, cancellationToken);

        /// <summary>
        /// Set a callback that will be called whenever the client receives a state update
        /// (desired or reported) from the service. Set callback value to null to clear.
        /// </summary>
        /// <remarks>
        /// This has the side-effect of subscribing to the PATCH topic on the service.
        /// </remarks>
        /// <param name="callback">Callback to call after the state update has been received and applied.</param>
        /// <param name="userContext">Context object that will be passed into callback.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task SetDesiredPropertyUpdateCallbackAsync(
            Func<TwinCollection, object, Task> callback,
            object userContext,
            CancellationToken cancellationToken = default)
            => InternalClient.SetDesiredPropertyUpdateCallbackAsync(callback, userContext, cancellationToken);

        /// <summary>
        /// Get a file upload SAS URI which the Azure Storage SDK can use to upload a file to blob for this device
        /// See <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-file-upload#initialize-a-file-upload">this documentation for more details</see>.
        /// </summary>
        /// <param name="request">The request details for getting the SAS URI, including the destination blob name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The file upload details to be used with the Azure Storage SDK in order to upload a file from this device.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task<FileUploadSasUriResponse> GetFileUploadSasUriAsync(
            FileUploadSasUriRequest request,
            CancellationToken cancellationToken = default)
            => InternalClient.GetFileUploadSasUriAsync(request, cancellationToken);

        /// <summary>
        /// Notify IoT hub that a device's file upload has finished.
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-file-upload#notify-iot-hub-of-a-completed-file-upload" />.
        /// <param name="notification">The notification details, including if the file upload succeeded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task CompleteFileUploadAsync(FileUploadCompletionNotification notification, CancellationToken cancellationToken = default)
            => InternalClient.CompleteFileUploadAsync(notification, cancellationToken);

        /// <summary>
        /// Close the DeviceClient instance.
        /// </summary>
        /// <remarks>
        /// The instance can be re-opened after closing and before disposing.
        /// </remarks>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task CloseAsync(CancellationToken cancellationToken = default) => InternalClient.CloseAsync(cancellationToken);

        /// <summary>
        /// Releases the unmanaged resources used by the DeviceClient and optionally disposes of the managed resources.
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
        /// Releases the unmanaged resources used by the DeviceClient and allows for any derived class to override and
        /// provide custom implementation.
        /// </summary>
        /// <param name="disposing">Setting to true will release both managed and unmanaged resources. Setting to
        /// false will only release the unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                InternalClient?.Dispose();
            }
        }
    }
}
