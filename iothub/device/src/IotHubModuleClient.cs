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
        private readonly bool _isAnEdgeModule;
        private readonly ICertificateValidator _certValidator;

        private readonly SemaphoreSlim _moduleReceiveMessageSemaphore = new(1, 1);

        // Cloud-to-module message callback information
        private volatile Tuple<Func<Message, object, Task<MessageAcknowledgement>>, object> _defaultEventCallback;

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

            // There is a distinction between a Module Twin and and Edge module. We set this flag in order
            // to correctly select the receiver link for AMQP on a Module Twin. This does not affect MQTT.
            // We can determine that this is an edge module if the connection string is using a gateway host.
            _isAnEdgeModule = !IotHubConnectionCredentials.GatewayHostName.IsNullOrWhiteSpace();

            _certValidator = certificateValidator ?? NullCertificateValidator.Instance;

            if (Logging.IsEnabled)
                Logging.CreateClient(
                    this,
                    $"HostName={IotHubConnectionCredentials.HostName};DeviceId={IotHubConnectionCredentials.DeviceId};ModuleId={IotHubConnectionCredentials.ModuleId}",
                    ClientOptions);
        }

        /// <summary>
        /// Creates a ModuleClient instance in an IoT Edge deployment based on environment variables.
        /// </summary>
        /// <param name="options">The options that allow configuration of the module client instance during initialization.</param>
        /// <returns>A disposable client instance.</returns>
        public static async Task<IotHubModuleClient> CreateFromEnvironmentAsync(IotHubClientOptions options = default)
        {
            // Make sure client options is initialized.
            if (options == default)
            {
                options = new();
            }

            IotHubConnectionCredentials iotHubConnectionCredentials = EdgeModuleClientHelper.CreateIotHubConnectionCredentialsFromEnvironment();
            ICertificateValidator certificateValidator = await EdgeModuleClientHelper.CreateCertificateValidatorFromEnvironmentAsync(new TrustBundleProvider(), options);

            return new IotHubModuleClient(iotHubConnectionCredentials, options, certificateValidator);
        }

        /// <summary>
        /// Sends an event to IoT hub. IotHubModuleClient instance must be opened already.
        /// </summary>
        /// <remarks>
        /// In case of a transient issue, retrying the operation should work. In case of a non-transient issue, inspect the error details and take steps accordingly.
        /// Please note that the above list is not exhaustive.
        /// </remarks>
        /// <param name="outputName">The output target for sending the given message.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="ArgumentNullException">Thrown when a required parameter is null.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        /// <exception cref="IotHubClientException">Thrown and <see cref="IotHubClientException.StatusCode"/> is set to <see cref="IotHubStatusCode.NetworkErrors"/>
        /// if the client encounters a transient retryable exception. </exception>
        /// <exception cref="InvalidOperationException">Thrown if ModuleClient instance is not opened already.</exception>
        /// <exception cref="SocketException">Thrown if a socket error occurs.</exception>
        /// <exception cref="WebSocketException">Thrown if an error occurs when performing an operation on a WebSocket connection.</exception>
        /// <exception cref="IOException">Thrown if an I/O error occurs.</exception>
        /// <exception cref="IotHubClientException">Thrown if an error occurs when communicating with IoT hub service.
        /// If <see cref="IotHubClientException.IsTransient"/> is set to <c>true</c> then it is a transient exception and should be retried,
        /// but if <c>false</c> then it is a non-transient exception and should probably not be retried.</exception>
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
        /// Sends a batch of events to IoT hub. Use AMQP or HTTPs for a true batch operation. MQTT will just send the messages one after the other.
        /// ModuleClient instance must be opened already.
        /// </summary>
        /// <remarks>
        /// For more information on IoT Edge module routing <see href="https://docs.microsoft.com/azure/iot-edge/module-composition?view=iotedge-2018-06#declare-routes"/>.
        /// </remarks>
        /// <param name="outputName">The output target for sending the given message.</param>
        /// <param name="messages">A list of one or more messages to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The task containing the event</returns>
        /// <exception cref="InvalidOperationException">Thrown if ModuleClient instance is not opened already.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
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
        /// Sets a new default delegate which applies to all endpoints.
        /// </summary>
        /// <remarks>
        /// If a default delegate was set previously, it will be overwritten.
        /// A message handler can be unset by setting <paramref name="messageHandler"/> to null.
        /// </remarks>
        /// <param name="messageHandler">The delegate to be called when a message is sent to any input.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The task containing the event</returns>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public async Task SetMessageHandlerAsync(
            Func<Message, object, Task<MessageAcknowledgement>> messageHandler,
            object userContext,
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
                    await EnableEventReceiveAsync(_isAnEdgeModule, cancellationToken).ConfigureAwait(false);
                    _defaultEventCallback = new Tuple<Func<Message, object, Task<MessageAcknowledgement>>, object>(messageHandler, userContext);
                }
                else
                {
                    _defaultEventCallback = null;
                    await DisableEventReceiveAsync(_isAnEdgeModule, cancellationToken).ConfigureAwait(false);
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
        /// Interactively invokes a method from an edge module to an edge device.
        /// Both the edge module and the edge device need to be connected to the same edge hub.
        /// ModuleClient instance must be opened already.
        /// </summary>
        /// <param name="deviceId">The unique identifier of the edge device to invoke the method on.</param>
        /// <param name="methodRequest">The details of the method to invoke.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The result of the method invocation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if ModuleClient instance is not opened already.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task<DirectMethodResponse> InvokeMethodAsync(string deviceId, DirectMethodRequest methodRequest, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(methodRequest, nameof(methodRequest));
            return InvokeMethodAsync(GetDeviceMethodUri(deviceId), methodRequest, cancellationToken);
        }

        /// <summary>
        /// Interactively invokes a method from an edge module to a different edge module.
        /// Both of the edge modules need to be connected to the same edge hub.
        /// ModuleClient instance must be opened already.
        /// </summary>
        /// <param name="deviceId">The unique identifier of the device.</param>
        /// <param name="moduleId">The unique identifier of the edge module to invoke the method on.</param>
        /// <param name="methodRequest">The details of the method to invoke.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The result of the method invocation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if ModuleClient instance is not opened already.</exception>
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

        internal override void AddToPipelineContext()
        {
            PipelineContext.ModuleEventCallback = OnModuleEventMessageReceivedAsync;
        }

        private void ValidateModuleTransportHandler(string apiName)
        {
            if (IotHubConnectionCredentials.ModuleId.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException($"{apiName} is available for Modules only.");
            }
        }

        // Enable telemetry downlink for modules
        private Task EnableEventReceiveAsync(bool isAnEdgeModule, CancellationToken cancellationToken = default)
        {
            // The telemetry downlink needs to be enabled only for the first time that the _defaultEventCallback delegate is set.
            return _defaultEventCallback == null
                ? InnerHandler.EnableEventReceiveAsync(isAnEdgeModule, cancellationToken)
                : Task.CompletedTask;
        }

        // Disable telemetry downlink for modules
        private Task DisableEventReceiveAsync(bool isAnEdgeModule, CancellationToken cancellationToken = default)
        {
            // The telemetry downlink should be disabled only after _defaultEventCallback delegate has been removed.
            return _defaultEventCallback == null
                ? InnerHandler.DisableEventReceiveAsync(isAnEdgeModule, cancellationToken)
                : Task.CompletedTask;
        }

        /// <summary>
        /// The delegate for handling event messages received
        /// </summary>
        /// <param name="message">The message received</param>
        internal async Task OnModuleEventMessageReceivedAsync(Message message)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, message?.InputName, nameof(OnModuleEventMessageReceivedAsync));

            if (message == null)
            {
                return;
            }

            try
            {
                var response = MessageAcknowledgement.Complete;
                if (_defaultEventCallback?.Item1 != null)
                {
                    Func<Message, object, Task<MessageAcknowledgement>> userSuppliedCallback = _defaultEventCallback.Item1;
                    object userContext = _defaultEventCallback.Item2;

                    response = await userSuppliedCallback
                        .Invoke(message, userContext)
                        .ConfigureAwait(false);
                }

                if (Logging.IsEnabled)
                    Logging.Info(this, $"{nameof(MessageAcknowledgement)} = {response}", nameof(OnModuleEventMessageReceivedAsync));

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

                        default:
                            break;
                    }
                }
                catch (Exception ex) when (Logging.IsEnabled)
                {
                    Logging.Error(this, ex, nameof(OnModuleEventMessageReceivedAsync));
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, message?.InputName, nameof(OnModuleEventMessageReceivedAsync));
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
                var methodInvokeRequest = new DirectMethodRequest()
                {
                    MethodName = methodRequest.MethodName,
                    Payload = methodRequest.Payload,
                    ResponseTimeout = methodRequest.ResponseTimeout,
                    ConnectionTimeout = methodRequest.ConnectionTimeout
                };

                DirectMethodResponse result = await httpTransport.InvokeMethodAsync(methodInvokeRequest, uri, cancellationToken).ConfigureAwait(false);

                return new DirectMethodResponse(result.Status)
                {
                    Payload = result.Payload
                };
            }
            finally
            {
                httpClientHandler?.Dispose();
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
