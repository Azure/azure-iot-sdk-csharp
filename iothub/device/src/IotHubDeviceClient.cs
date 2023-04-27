// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Contains methods that a device can use to send messages to and receive from the service.
    /// </summary>
    /// <threadsafety static="true" instance="true" />
    public class IotHubDeviceClient : IotHubBaseClient
    {
        /// <summary>
        /// Creates a disposable client from the specified connection string.
        /// </summary>
        /// <remarks>
        /// This client is safe to cache and use for the lifetime of an application. Calling <see cref="IotHubBaseClient.DisposeAsync" /> as the application is shutting down
        /// will ensure that network resources and other unmanaged objects are properly cleaned up.
        /// </remarks>
        /// <param name="connectionString">The connection string based on shared access key used in API calls which allows the device to communicate with IoT Hub.</param>
        /// <param name="options">The optional configuration of the device client instance.</param>
        /// <returns>A disposable client instance.</returns>
        /// <exception cref="ArgumentNullException">Either <paramref name="connectionString"/> is null,
        /// or the IoT hub host name or device Id in the connection string is null.</exception>
        /// <exception cref="ArgumentException">Either <paramref name="connectionString"/> is an empty string or consists only of white-space characters,
        /// or the IoT hub host name or device Id in the connection string are an empty string or consist only of white-space characters.</exception>
        /// <exception cref="ArgumentException">Neither shared access key nor shared access signature were presented for authentication.</exception>
        /// <exception cref="InvalidOperationException">A module Id was specified in the connection string. <see cref="IotHubModuleClient"/> should be used for modules.</exception>
        /// <example>
        /// <code language="csharp">
        /// await using var client = new IotHubDeviceClient(
        ///     connectionString,
        ///     new IotHubClientOptions(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket)));
        /// </code>
        /// </example>
        public IotHubDeviceClient(string connectionString, IotHubClientOptions options = default)
            : this(new IotHubConnectionCredentials(connectionString), options)
        {
        }

        /// <summary>
        /// Creates a disposable client from the specified parameters.
        /// </summary>
        /// <remarks>
        /// This client is safe to cache and use for the lifetime of an application. Calling <see cref="IotHubBaseClient.DisposeAsync" /> as the application is shutting down
        /// will ensure that network resources and other unmanaged objects are properly cleaned up.
        /// </remarks>
        /// <param name="hostName">The fully-qualified DNS host name of IoT hub.</param>
        /// <param name="authenticationMethod">
        /// The authentication method that is used. It includes <see cref="ClientAuthenticationWithSharedAccessKeyRefresh"/>, <see cref="ClientAuthenticationWithSharedAccessSignature"/>
        /// or <see cref="ClientAuthenticationWithX509Certificate"/>.
        /// </param>
        /// <param name="options">The optional configuration of the device client instance.</param>
        /// <returns>A disposable client instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="hostName"/>, device Id or <paramref name="authenticationMethod"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="hostName"/> or device Id are an empty string or consist only of white-space characters.</exception>
        /// <exception cref="ArgumentException">Neither shared access key, shared access signature or X509 certificates were presented for authentication.</exception>
        /// <exception cref="ArgumentException">Either shared access key or shared access signature were presented together with X509 certificates for authentication.</exception>
        /// <exception cref="ArgumentException"><see cref="ClientAuthenticationWithX509Certificate.CertificateChain"/> is used over a protocol other than MQTT over TCP or AMQP over TCP></exception>
        /// <exception cref="IotHubClientException"><see cref="ClientAuthenticationWithX509Certificate.CertificateChain"/> could not be installed.</exception>
        /// <exception cref="InvalidOperationException">A module Id was specified in the provided <paramref name="authenticationMethod"/>. <see cref="IotHubModuleClient"/> should be used for modules.</exception>
        /// <example>
        /// <code language="csharp">
        /// await using var client = new IotHubDeviceClient(
        ///     hostName,
        ///     new ClientAuthenticationWithSharedAccessKeyRefresh(sharedAccessKey, deviceId),
        ///     new IotHubClientOptions(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket)));
        /// </code>
        /// </example>
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
            if (IotHubConnectionCredentials.AuthenticationMethod is ClientAuthenticationWithX509Certificate x509CertificateAuth
                && x509CertificateAuth.CertificateChain != null)
            {
                if (_clientOptions.TransportSettings.Protocol != IotHubClientTransportProtocol.Tcp)
                {
                    throw new ArgumentException("Certificate chains for devices are only supported on MQTT over TCP and AMQP over TCP.");
                }
            }

            if (Logging.IsEnabled)
                Logging.CreateClient(
                    this,
                    $"HostName={IotHubConnectionCredentials.HostName};DeviceId={IotHubConnectionCredentials.DeviceId}",
                    _clientOptions);
        }

        /// <summary>
        /// Get a file upload SAS URI which the Azure Storage SDK can use to upload a file to blob for this device.
        /// </summary>
        /// <remarks>
        /// See <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-file-upload#initialize-a-file-upload">this documentation for more details</see>.
        /// </remarks>
        /// <param name="request">The request details for getting the SAS URI, including the destination blob name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The file upload details to be used with the Azure Storage SDK in order to upload a file from this device.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The client instance is not already open.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled.</exception>
        /// <exception cref="IotHubClientException">An error occured when communicating with IoT hub service.</exception>
        /// <exception cref="ObjectDisposedException">The client has been disposed.</exception>
        /// <example>
        /// <code language="csharp">
        /// FileUploadSasUriResponse sasUri = await client.GetFileUploadSasUriAsync(new FileUploadSasUriRequest(fileName), cancellationToken);
        /// </code>
        /// </example>
        public Task<FileUploadSasUriResponse> GetFileUploadSasUriAsync(
            FileUploadSasUriRequest request,
            CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(request, nameof(request));
            cancellationToken.ThrowIfCancellationRequested();

            return InnerHandler.GetFileUploadSasUriAsync(request, cancellationToken);
        }

        /// <summary>
        /// Notify IoT hub that a device's file upload has finished.
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-file-upload#notify-iot-hub-of-a-completed-file-upload" />.
        /// <param name="notification">The notification details, including if the file upload succeeded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException"><paramref name="notification"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The client instance is not already open.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled.</exception>
        /// <exception cref="IotHubClientException">An error occured when communicating with IoT hub service.</exception>
        /// <exception cref="ObjectDisposedException">The client has been disposed.</exception>
        /// <example>
        /// <code language="csharp">
        /// await client.CompleteFileUploadAsync(
        ///     new FileUploadCompletionNotification(correlationId: correlationId, isSuccess: true)
        ///     {
        ///         StatusCode = 200,
        ///         StatusDescription = "Success",
        ///     },
        ///     cancellationToken);
        /// </code>
        /// </example>
        public Task CompleteFileUploadAsync(FileUploadCompletionNotification notification, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(notification, nameof(notification));
            cancellationToken.ThrowIfCancellationRequested();

            return InnerHandler.CompleteFileUploadAsync(notification, cancellationToken);
        }
    }
}
