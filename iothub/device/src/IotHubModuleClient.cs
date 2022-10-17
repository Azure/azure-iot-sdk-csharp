// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Edge;
using Microsoft.Azure.Devices.Client.Transport;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Contains methods that a module can use to send messages to and receive from the service and interact with module twins.
    /// </summary>
    public class IotHubModuleClient : IotHubBaseClient
    {
        private const string ModuleMethodUriFormat = "/twins/{0}/modules/{1}/methods?" + ClientApiVersionHelper.ApiVersionQueryStringLatest;
        private const string DeviceMethodUriFormat = "/twins/{0}/methods?" + ClientApiVersionHelper.ApiVersionQueryStringLatest;
        private readonly ICertificateValidator _certValidator;

        private readonly SemaphoreSlim _moduleReceiveMessageSemaphore = new(1, 1);

        /// <summary>
        /// Creates a disposable <c>IotHubModuleClient</c> from the specified connection string.
        /// </summary>
        /// <param name="connectionString">The connection string based on shared access key used in API calls which allows the module to communicate with IoT Hub.</param>
        /// <param name="options">The options that allow configuration of the module client instance during initialization.</param>
        /// <returns>A disposable client instance.</returns>
        /// <exception cref="ArgumentNullException">Either <paramref name="connectionString"/> is null,
        /// or the IoT hub host name, device Id or module Id in the connection string is null.</exception>
        /// <exception cref="ArgumentException">Either <paramref name="connectionString"/> is an empty string or consists only of white-space characters,
        /// or the IoT hub host name, device Id or module Id in the connection string are an empty string or consist only of white-space characters.</exception>
        /// <exception cref="ArgumentException">Neither shared access key nor shared access signature were presented for authentication.</exception>
        public IotHubModuleClient(string connectionString, IotHubClientOptions options = default)
            : this(new IotHubConnectionCredentials(connectionString), options, null)
        {
        }

        /// <summary>
        /// Creates a disposable <c>IotHubModuleClient</c> from the specified parameters.
        /// </summary>
        /// <param name="hostName">The fully-qualified DNS host name of IoT hub.</param>
        /// <param name="authenticationMethod">The authentication method that is used.</param>
        /// <param name="options">The options that allow configuration of the module client instance during initialization.</param>
        /// <returns>A disposable <c>IotHubModuleClient</c> instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="hostName"/>, device Id, module Id or <paramref name="authenticationMethod"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="hostName"/>, device Id or module Id are an empty string or consist only of white-space characters.</exception>
        /// <exception cref="ArgumentException">Neither shared access key, shared access signature or X509 certificates were presented for authentication.</exception>
        /// <exception cref="ArgumentException">Either shared access key or shared access signature where presented together with X509 certificates for authentication.</exception>
        public IotHubModuleClient(string hostName, IAuthenticationMethod authenticationMethod, IotHubClientOptions options = default)
            : this(new IotHubConnectionCredentials(authenticationMethod, hostName, options?.GatewayHostName), options, null)
        {
        }

        internal IotHubModuleClient(IotHubConnectionCredentials iotHubConnectionCredentials, IotHubClientOptions options, ICertificateValidator certificateValidator)
            : base(iotHubConnectionCredentials, options)
        {
            // Validate
            if (iotHubConnectionCredentials.ModuleId.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException("A valid module Id should be specified in the authentication credentails to create an IotHubModuleClient.");
            }

            _certValidator = certificateValidator ?? NullCertificateValidator.Instance;

            if (Logging.IsEnabled)
                Logging.CreateClient(
                    this,
                    $"HostName={IotHubConnectionCredentials.HostName};DeviceId={IotHubConnectionCredentials.DeviceId};ModuleId={IotHubConnectionCredentials.ModuleId}",
                    _clientOptions);
        }

        /// <summary>
        /// Creates a disposable <c>IotHubModuleClient</c> instance in an IoT Edge deployment based on environment variables.
        /// </summary>
        /// <param name="options">The options that allow configuration of the module client instance during initialization.</param>
        /// <returns>A disposable client instance.</returns>
        public static async Task<IotHubModuleClient> CreateFromEnvironmentAsync(IotHubClientOptions options = default)
        {
            IotHubClientOptions clientOptions = options != null
                ? options.Clone()
                : new();

            IotHubConnectionCredentials iotHubConnectionCredentials = EdgeModuleClientHelper.CreateIotHubConnectionCredentialsFromEnvironment();
            ICertificateValidator certificateValidator = await EdgeModuleClientHelper.CreateCertificateValidatorFromEnvironmentAsync(new TrustBundleProvider(), clientOptions);

            return new IotHubModuleClient(iotHubConnectionCredentials, options, certificateValidator);
        }

        /// <summary>
        /// Sends an event to IoT hub. IotHubModuleClient instance must be opened already.
        /// </summary>
        /// <remarks>
        /// <para>
        /// For more information on IoT Edge module routing <see href="https://docs.microsoft.com/azure/iot-edge/module-composition?view=iotedge-2018-06#declare-routes"/>.
        /// </para>
        /// In case of a transient issue, retrying the operation should work. In case of a non-transient issue, inspect the error details and take steps accordingly.
        /// Please note that the above list is not exhaustive.
        /// </remarks>
        /// <param name="outputName">The output target for sending the given message.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="ArgumentNullException">Thrown when a required parameter is null.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        /// <exception cref="InvalidOperationException">Thrown if ModuleClient instance is not opened already.</exception>
        /// <exception cref="SocketException">Thrown if a socket error occurs.</exception>
        /// <exception cref="WebSocketException">Thrown if an error occurs when performing an operation on a WebSocket connection.</exception>
        /// <exception cref="IOException">Thrown if an I/O error occurs.</exception>
        /// <exception cref="IotHubClientException">Thrown if an error occurs when communicating with IoT hub service.</exception>
        public async Task SendTelemetryAsync(string outputName, TelemetryMessage message, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, outputName, message, nameof(SendTelemetryAsync));

            Argument.AssertNotNullOrWhiteSpace(outputName, nameof(outputName));
            Argument.AssertNotNull(message, nameof(message));

            ValidateModuleTransportHandler("SendTelemetryAsync for a named output");

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                message.SystemProperties.Add(MessageSystemPropertyNames.OutputName, outputName);

                await InnerHandler.SendTelemetryAsync(message, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, outputName, message, nameof(SendTelemetryAsync));
            }
        }

        /// <summary>
        /// Sends a batch of events to IoT hub. Use AMQP or HTTPs for a true batch operation. MQTT will just send the messages one after the other.
        /// IotHubModuleClient instance must be opened already.
        /// </summary>
        /// <remarks>
        /// For more information on IoT Edge module routing <see href="https://docs.microsoft.com/azure/iot-edge/module-composition?view=iotedge-2018-06#declare-routes"/>.
        /// </remarks>
        /// <param name="outputName">The output target for sending the given message.</param>
        /// <param name="messages">A list of one or more messages to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The task containing the event</returns>
        /// <exception cref="InvalidOperationException">Thrown if IotHubModuleClient instance is not opened already.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public async Task SendTelemetryBatchAsync(string outputName, IEnumerable<TelemetryMessage> messages, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, outputName, messages, nameof(SendTelemetryBatchAsync));

            Argument.AssertNotNullOrWhiteSpace(outputName, nameof(outputName));

            var messagesList = messages?.ToList();
            Argument.AssertNotNullOrEmpty(messagesList, nameof(messages));

            ValidateModuleTransportHandler("SendTelemetryBatchAsync for a named output");

            try
            {
                messagesList.ForEach(m => m.SystemProperties.Add(MessageSystemPropertyNames.OutputName, outputName));

                await InnerHandler.SendTelemetryAsync(messagesList, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, outputName, messages, nameof(SendTelemetryBatchAsync));
            }
        }

        /// <summary>
        /// Interactively invokes a method from an edge module to an edge device.
        /// Both the edge module and the edge device need to be connected to the same edge hub.
        /// IotHubModuleClient instance must be opened already.
        /// </summary>
        /// <remarks>
        /// This API call is relevant only for IoT Edge modules.
        /// </remarks>
        /// <param name="deviceId">The unique identifier of the edge device to invoke the method on.</param>
        /// <param name="methodRequest">The details of the method to invoke.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The result of the method invocation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if IotHubModuleClient instance is not opened already.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task<DirectMethodResponse> InvokeMethodAsync(string deviceId, DirectMethodRequest methodRequest, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(methodRequest, nameof(methodRequest));
            return InvokeMethodAsync(GetDeviceMethodUri(deviceId), methodRequest, cancellationToken);
        }

        /// <summary>
        /// Interactively invokes a method from an edge module to a different edge module.
        /// Both of the edge modules need to be connected to the same edge hub.
        /// IotHubModuleClient instance must be opened already.
        /// </summary>
        /// <remarks>
        /// This API call is relevant only for IoT Edge modules.
        /// </remarks>
        /// <param name="deviceId">The unique identifier of the device.</param>
        /// <param name="moduleId">The unique identifier of the edge module to invoke the method on.</param>
        /// <param name="methodRequest">The details of the method to invoke.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The result of the method invocation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if IotHubModuleClient instance is not opened already.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task<DirectMethodResponse> InvokeMethodAsync(string deviceId, string moduleId, DirectMethodRequest methodRequest, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(methodRequest, nameof(methodRequest));
            return InvokeMethodAsync(GetModuleMethodUri(deviceId, moduleId), methodRequest, cancellationToken);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _moduleReceiveMessageSemaphore?.Dispose();
            }

            // Call the base class implementation.
            base.Dispose(disposing);
        }

        private void ValidateModuleTransportHandler(string apiName)
        {
            if (IotHubConnectionCredentials.ModuleId.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException($"{apiName} is available for Modules only.");
            }
        }

        private async Task<DirectMethodResponse> InvokeMethodAsync(Uri uri, DirectMethodRequest methodRequest, CancellationToken cancellationToken = default)
        {
            HttpClientHandler httpClientHandler = null;
            Func<object, X509Certificate, X509Chain, SslPolicyErrors, bool> customCertificateValidation = _certValidator.GetCustomCertificateValidation();

            try
            {
                var transportSettings = new IotHubClientHttpSettings();

                if (customCertificateValidation != null)
                {
                    httpClientHandler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = customCertificateValidation,
                        SslProtocols = transportSettings.SslProtocols,
                    };
                }

                var pipelineContext = new PipelineContext
                {
                    IotHubConnectionCredentials = IotHubConnectionCredentials,
                };

                using var httpTransport = new HttpTransportHandler(pipelineContext, transportSettings, httpClientHandler);
                methodRequest.PayloadConvention = _clientOptions.PayloadConvention;

                DirectMethodResponse result = await httpTransport.InvokeMethodAsync(methodRequest, uri, cancellationToken).ConfigureAwait(false);

                return new DirectMethodResponse(result.Status)
                {
                    Payload = result.Payload,
                    PayloadConvention = _clientOptions.PayloadConvention,
                };
            }
            finally
            {
                httpClientHandler?.Dispose();
                _certValidator.Dispose();
            }
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
