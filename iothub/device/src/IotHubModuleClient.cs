// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Edge;
using Microsoft.Azure.Devices.Client.HsmAuthentication;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A client for a device or Edge module.
    /// </summary>
    public class IotHubModuleClient : IotHubBaseClient
    {
        private const string ModuleMethodUriFormat = "/twins/{0}/modules/{1}/methods?" + ClientApiVersionHelper.ApiVersionQueryStringLatest;
        private const string DeviceMethodUriFormat = "/twins/{0}/methods?" + ClientApiVersionHelper.ApiVersionQueryStringLatest;

        private const string IotDeviceModuleMethodInvokeErrorMessage = "This API call is relevant only for IoT Edge modules. Please make sure your client is initialized correctly with a gateway hostname. " +
            "For subscribing to IoT device module direct method invocations, see SetDirectMethodCallbackAsync(...).";

        /// <summary>
        /// Creates a disposable client from the specified connection string.
        /// </summary>
        /// <remarks>
        /// This client is safe to cache and use for the lifetime of an application. Calling <see cref="IotHubBaseClient.DisposeAsync" /> as the application is shutting down
        /// will ensure that network resources and other unmanaged objects are properly cleaned up.
        /// </remarks>
        /// <param name="connectionString">The connection string based on shared access key used in API calls which allows the module to communicate with IoT Hub.</param>
        /// <param name="options">The options that allow configuration of the module client instance during initialization.</param>
        /// <returns>A disposable client instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connectionString"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="connectionString"/> is empty or white-space.</exception>
        /// <exception cref="ArgumentException">Neither shared access key nor shared access signature were presented for authentication.</exception>
        /// <exception cref="InvalidOperationException">Required key/value pairs were missing from the connection string.</exception>
        /// <returns>A disposable client instance.</returns>
        /// <exception cref="InvalidOperationException">A module Id was missing in the connection string. <see cref="IotHubDeviceClient"/> should be used for devices.</exception>
        /// <exception cref="InvalidOperationException">Different gateway hostnames were specified through the connection string and <see cref="IotHubClientOptions.GatewayHostName"/>.
        /// It is recommended to not hand edit connection strings but instead use <see cref="IotHubClientOptions"/> to specify values for the additional fields.</exception>
        /// <example>
        /// <code language="csharp">
        /// await using var client = new IotHubModuleClient(
        ///     connectionString,
        ///     new IotHubClientOptions(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket)));
        /// </code>
        /// </example>
        public IotHubModuleClient(string connectionString, IotHubClientOptions options = default)
            : this(new IotHubConnectionCredentials(connectionString, options?.GatewayHostName), options, null)
        {
        }

        /// <summary>
        /// Creates a disposable <c>IotHubModuleClient</c> from the specified parameters.
        /// </summary>
        /// <remarks>
        /// This client is safe to cache and use for the lifetime of an application. Calling <see cref="IotHubBaseClient.DisposeAsync" /> as the application is shutting down
        /// will ensure that network resources and other unmanaged objects are properly cleaned up.
        /// </remarks>
        /// <param name="hostName">The fully-qualified DNS host name of IoT hub.</param>
        /// <param name="authenticationMethod">
        /// The authentication method that is used. It includes <see cref="ClientAuthenticationWithSharedAccessKeyRefresh"/>, <see cref="ClientAuthenticationWithSharedAccessSignature"/>,
        /// <see cref="ClientAuthenticationWithX509Certificate"/> or <see cref="EdgeModuleAuthenticationWithHsm"/>.
        /// </param>
        /// <param name="options">The options that allow configuration of the module client instance during initialization.</param>
        /// <returns>A disposable <c>IotHubModuleClient</c> instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="hostName"/> or <paramref name="authenticationMethod"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="hostName"/>is empty or white-space.</exception>
        /// <exception cref="ArgumentException">Neither shared access key, shared access signature, nor X509 certificates were presented for authentication.</exception>
        /// <exception cref="ArgumentException">Either shared access key or shared access signature were presented together with X509 certificates for authentication.</exception>
        /// <exception cref="InvalidOperationException">A module Id was missing in the provided <paramref name="authenticationMethod"/>. <see cref="IotHubDeviceClient"/> should be used for devices.</exception>
        /// <example>
        /// <code language="csharp">
        /// await using var client = new IotHubModuleClient(
        ///     hostName,
        ///     new ClientAuthenticationWithSharedAccessKeyRefresh(sharedAccessKey, deviceId, moduleId),
        ///     new IotHubClientOptions(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket)));
        /// </code>
        /// </example>
        public IotHubModuleClient(string hostName, IAuthenticationMethod authenticationMethod, IotHubClientOptions options = default)
            : this(new IotHubConnectionCredentials(authenticationMethod, hostName, options?.GatewayHostName), options, null)
        {
        }

        internal IotHubModuleClient(IotHubConnectionCredentials iotHubConnectionCredentials, IotHubClientOptions options, ICertificateValidator certificateValidator)
            : base(iotHubConnectionCredentials, options, certificateValidator)
        {
            // Validate
            if (iotHubConnectionCredentials.ModuleId.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException("A valid module Id should be specified in the authentication credentails to create an IotHubModuleClient.");
            }

            if (Logging.IsEnabled)
                Logging.CreateClient(
                    this,
                    $"HostName={IotHubConnectionCredentials.HostName};DeviceId={IotHubConnectionCredentials.DeviceId};ModuleId={IotHubConnectionCredentials.ModuleId};isEdgeModule={IotHubConnectionCredentials.IsEdgeModule}",
                    _clientOptions);
        }

        /// <summary>
        /// Creates a disposable <c>IotHubModuleClient</c> instance in an IoT Edge deployment based on environment variables.
        /// </summary>
        /// <param name="options">The options that allow configuration of the module client instance during initialization.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A disposable client instance.</returns>
        /// <exception cref="InvalidOperationException">The required environmental variables were missing. Check the exception thrown for additional details.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled.</exception>
        /// <example>
        /// <code language="csharp">
        /// await using var client = await IotHubModuleClient.CreateFromEnvironmentAsync(new IotHubClientOptions(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket)));
        /// </code>
        /// </example>
        public static async Task<IotHubModuleClient> CreateFromEnvironmentAsync(IotHubClientOptions options = default, CancellationToken cancellationToken = default)
        {
            IotHubClientOptions clientOptions = options != null
                ? options.Clone()
                : new();

            IotHubConnectionCredentials iotHubConnectionCredentials = EdgeModuleClientHelper.CreateIotHubConnectionCredentialsFromEnvironment();
            ICertificateValidator certificateValidator = await EdgeModuleClientHelper.CreateCertificateValidatorFromEnvironmentAsync(
                new TrustBundleProvider(),
                clientOptions,
                cancellationToken);

            return new IotHubModuleClient(iotHubConnectionCredentials, options, certificateValidator);
        }

        /// <summary>
        /// Sends a message to IoT hub.
        /// </summary>
        /// <remarks>
        /// IotHubModuleClient instance must be already open.
        /// <para>
        /// For more information on IoT Edge module routing <see href="https://docs.microsoft.com/azure/iot-edge/module-composition?view=iotedge-2018-06#declare-routes"/>.
        /// </para>
        /// <para>
        /// In case of a transient issue, retrying the operation should work. In case of a non-transient issue, inspect the error details and take steps accordingly.
        /// Please note that the above list is not exhaustive.
        /// </para>
        /// </remarks>
        /// <param name="outputName">The named module route for sending the given message.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="ArgumentNullException"><paramref name="outputName"/> or <paramref name="message"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The client instance is not already open.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled.</exception>
        /// <exception cref="IotHubClientException">An error occured when communicating with IoT hub service.</exception>
        /// <exception cref="ObjectDisposedException">The client has been disposed.</exception>
        /// <example>
        /// <code language="csharp">
        /// await client.SendMessageToRouteAsync(outputName, new TelemetryMessage(payload), cancellationToken);
        /// </code>
        /// </example>
        public async Task SendMessageToRouteAsync(string outputName, TelemetryMessage message, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, outputName, message, nameof(SendMessageToRouteAsync));

            Argument.AssertNotNullOrWhiteSpace(outputName, nameof(outputName));
            Argument.AssertNotNull(message, nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                message.OutputName = outputName;
                await SendTelemetryAsync(message, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, outputName, message, nameof(SendMessageToRouteAsync));
            }
        }

        /// <summary>
        /// Sends a batch of events to IoT hub. Use AMQP or HTTPs for a true batch operation. MQTT will just send the messages one after the other.
        /// </summary>
        /// <remarks>
        /// IotHubModuleClient instance must be already open.
        /// <para>
        /// For more information on IoT Edge module routing <see href="https://docs.microsoft.com/azure/iot-edge/module-composition?view=iotedge-2018-06#declare-routes"/>.
        /// </para>
        /// </remarks>
        /// <param name="outputName">The named module route for sending the given message.</param>
        /// <param name="messages">A list of one or more messages to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The task containing the event.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="outputName"/> or <paramref name="messages"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The client instance is not already open.</exception>
        /// <exception cref="InvalidOperationException">This method is called when the client is configured to use MQTT.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled.</exception>
        /// <exception cref="IotHubClientException">An error occured when communicating with IoT hub service.</exception>
        /// <exception cref="ObjectDisposedException">The client has been disposed.</exception>
        /// <example>
        /// <code language="csharp">
        /// var client = new IotHubModuleClient(
        ///     connectionString,
        ///     new IotHubClientOptions(new IotHubClientAmqpSettings())); // This operation only works over AMQP
        ///
        /// await client.SendMessagesToRouteAsync(outputName, new List&lt;TelemetryMessage&gt; { message1, message2 }, cancellationToken);
        /// </code>
        /// </example>
        public async Task SendMessagesToRouteAsync(string outputName, IEnumerable<TelemetryMessage> messages, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, outputName, messages, nameof(SendMessagesToRouteAsync));

            Argument.AssertNotNullOrWhiteSpace(outputName, nameof(outputName));
            cancellationToken.ThrowIfCancellationRequested();

            var messagesList = messages?.ToList();
            Argument.AssertNotNullOrEmpty(messagesList, nameof(messages));

            try
            {
                messagesList.ForEach(m => m.OutputName = outputName);

                await SendTelemetryAsync(messagesList, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, outputName, messages, nameof(SendMessagesToRouteAsync));
            }
        }

        /// <summary>
        /// Interactively invokes a method from an edge module to an edge device.
        /// Both the edge module and the edge device need to be connected to the same edge hub.
        /// </summary>
        /// <remarks>
        /// IotHubModuleClient instance must be already open.
        /// <para>
        /// This API call is relevant only for IoT Edge modules.
        /// </para>
        /// </remarks>
        /// <param name="deviceId">The unique identifier of the edge device to invoke the method on.</param>
        /// <param name="methodRequest">The details of the method to invoke.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The result of the method invocation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="deviceId"/> or <paramref name="methodRequest"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The client instance is not already open.</exception>
        /// <exception cref="InvalidOperationException">An IoT device module is used to invoke this API.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled.</exception>
        /// <exception cref="IotHubClientException">An error occured when communicating with IoT hub service.</exception>
        /// <exception cref="ObjectDisposedException">The client has been disposed.</exception>
        /// <example>
        /// <code language="csharp">
        /// DirectMethodResponse response = await client.InvokeMethodAsync(deviceId, new EdgeModuleDirectMethodRequest(methodName), cancellationToken);
        /// </code>
        /// </example>
        public Task<DirectMethodResponse> InvokeMethodAsync(string deviceId, EdgeModuleDirectMethodRequest methodRequest, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
            Argument.AssertNotNull(methodRequest, nameof(methodRequest));

            if (!IotHubConnectionCredentials.IsEdgeModule)
            {
                throw new InvalidOperationException(IotDeviceModuleMethodInvokeErrorMessage);
            }

            cancellationToken.ThrowIfCancellationRequested();

            return InvokeMethodAsync(GetDeviceMethodUri(deviceId), methodRequest, cancellationToken);
        }

        /// <summary>
        /// Interactively invokes a method from an edge module to a different edge module.
        /// Both of the edge modules need to be connected to the same edge hub.
        /// </summary>
        /// <remarks>
        /// IotHubModuleClient instance must be already open.
        /// <para>
        /// This API call is relevant only for IoT Edge modules.
        /// </para>
        /// </remarks>
        /// <param name="deviceId">The unique identifier of the device.</param>
        /// <param name="moduleId">The unique identifier of the edge module to invoke the method on.</param>
        /// <param name="methodRequest">The details of the method to invoke.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The result of the method invocation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="deviceId"/>, <paramref name="moduleId"/> or <paramref name="methodRequest"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The client instance is not already open.</exception>
        /// <exception cref="InvalidOperationException">An IoT device module is used to invoke this API.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled.</exception>
        /// <exception cref="IotHubClientException">An error occured when communicating with IoT hub service.</exception>
        /// <exception cref="ObjectDisposedException">The client has been disposed.</exception>
        /// <example>
        /// <code language="csharp">
        /// DirectMethodResponse response = await client.InvokeMethodAsync(deviceId, moduleId, new EdgeModuleDirectMethodRequest(methodName), cancellationToken);
        /// </code>
        /// </example>
        public Task<DirectMethodResponse> InvokeMethodAsync(string deviceId, string moduleId, EdgeModuleDirectMethodRequest methodRequest, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
            Argument.AssertNotNullOrWhiteSpace(moduleId, nameof(moduleId));
            Argument.AssertNotNull(methodRequest, nameof(methodRequest));

            if (!IotHubConnectionCredentials.IsEdgeModule)
            {
                throw new InvalidOperationException(IotDeviceModuleMethodInvokeErrorMessage);
            }

            cancellationToken.ThrowIfCancellationRequested();

            return InvokeMethodAsync(GetModuleMethodUri(deviceId, moduleId), methodRequest, cancellationToken);
        }

        private async Task<DirectMethodResponse> InvokeMethodAsync(Uri uri, EdgeModuleDirectMethodRequest methodRequest, CancellationToken cancellationToken = default)
        {
            methodRequest.PayloadConvention = _clientOptions.PayloadConvention;
            DirectMethodResponse result = await InnerHandler.InvokeMethodAsync(methodRequest, uri, cancellationToken).ConfigureAwait(false);

            return new DirectMethodResponse(result.Status)
            {
                Payload = result.Payload,
                PayloadConvention = _clientOptions.PayloadConvention,
            };
        }

        private static Uri GetDeviceMethodUri(string deviceId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            return new Uri(string.Format(CultureInfo.InvariantCulture, DeviceMethodUriFormat, deviceId), UriKind.Relative);
        }

        private static Uri GetModuleMethodUri(string deviceId, string moduleId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            moduleId = WebUtility.UrlEncode(moduleId);
            return new Uri(string.Format(CultureInfo.InvariantCulture, ModuleMethodUriFormat, deviceId, moduleId), UriKind.Relative);
        }
    }
}
