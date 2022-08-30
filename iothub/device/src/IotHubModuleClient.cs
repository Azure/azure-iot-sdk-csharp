// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using Microsoft.Azure.Devices.Client.Edge;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Transport;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Contains methods that a module can use to send messages to and receive from the service and interact with module twins.
    /// </summary>
    public class IotHubModuleClient : IDisposable
    {
        private const string ModuleMethodUriFormat = "/twins/{0}/modules/{1}/methods?" + ClientApiVersionHelper.ApiVersionQueryStringLatest;
        private const string DeviceMethodUriFormat = "/twins/{0}/methods?" + ClientApiVersionHelper.ApiVersionQueryStringLatest;
        private readonly bool _isAnEdgeModule;
        private readonly ICertificateValidator _certValidator;

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
        {
            Argument.AssertNotNullOrWhiteSpace(iotHubConnectionCredentials.ModuleId, nameof(iotHubConnectionCredentials.ModuleId));

            // Make sure client options is initialized.
            if (options == default)
            {
                options = new();
            }

            InternalClient = new InternalClient(iotHubConnectionCredentials, options, null);

            // There is a distinction between a Module Twin and and Edge module. We set this flag in order
            // to correctly select the receiver link for AMQP on a Module Twin. This does not affect MQTT.
            // We can determine that this is an edge module if the connection string is using a gateway host.
            _isAnEdgeModule = !InternalClient.IotHubConnectionCredentials.GatewayHostName.IsNullOrWhiteSpace();

            _certValidator = certificateValidator ?? NullCertificateValidator.Instance;

            if (Logging.IsEnabled)
                Logging.CreateClient(
                    InternalClient,
                    $"HostName={InternalClient.IotHubConnectionCredentials.HostName};DeviceId={InternalClient.IotHubConnectionCredentials.DeviceId};ModuleId={InternalClient.IotHubConnectionCredentials.ModuleId}",
                    options);
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

        internal InternalClient InternalClient { get; private set; }

        internal IDelegatingHandler InnerHandler
        {
            get => InternalClient.InnerHandler;
            set => InternalClient.InnerHandler = value;
        }

        /// <summary>
        /// The diagnostic sampling percentage.
        /// </summary>
        public int DiagnosticSamplingPercentage
        {
            get => InternalClient.DiagnosticSamplingPercentage;
            set => InternalClient.DiagnosticSamplingPercentage = value;
        }

        /// <summary>
        /// Sets a new delegate for the connection status changed callback. If a delegate is already associated,
        /// it will be replaced with the new delegate. Note that this callback will never be called if the client is configured to use HTTP as that protocol is stateless
        /// <param name="statusChangeHandler">The name of the method to associate with the delegate.</param>
        /// </summary>
        public void SetConnectionStatusChangeHandler(Action<ConnectionStatusInfo> statusChangeHandler)
            => InternalClient.SetConnectionStatusChangeHandler(statusChangeHandler);

        /// <summary>
        /// The latest connection status information since the last status change.
        /// </summary>
        public ConnectionStatusInfo ConnectionStatusInfo => InternalClient._connectionStatusInfo;

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
            object userContext, CancellationToken cancellationToken = default)
            => InternalClient.SetDesiredPropertyUpdateCallbackAsync(callback, userContext, cancellationToken);

        /// <summary>
        /// Sets a new delegate for the named method. If a delegate is already associated with the named method, it will be replaced with the new delegate.
        /// A method handler can be unset by passing a null MethodCallback.
        /// </summary>
        /// <param name="methodName">The name of the method to associate with the delegate.</param>
        /// <param name="methodHandler">The delegate to be used when a method with the given name is called by the cloud service.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
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
        /// <param name="methodHandler">The delegate to be used when a method is called by the cloud service and there is no delegate registered for that method name.</param>
        /// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task SetMethodDefaultHandlerAsync(
            Func<DirectMethodRequest, object, Task<DirectMethodResponse>> methodHandler,
            object userContext,
            CancellationToken cancellationToken = default)
            => InternalClient.SetMethodDefaultHandlerAsync(methodHandler, userContext, cancellationToken);

        /// <summary>
        /// Sets a new delegate for the particular input. If a delegate is already associated with
        /// the input, it will be replaced with the new delegate.
        /// </summary>
        /// <param name="inputName">The name of the input to associate with the delegate.</param>
        /// <param name="messageHandler">The delegate to be used when a message is sent to the particular inputName.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The task containing the event</returns>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task SetInputMessageHandlerAsync(
            string inputName,
            Func<Message, object, Task<MessageResponse>> messageHandler,
            object userContext,
            CancellationToken cancellationToken = default)
            => InternalClient.SetInputMessageHandlerAsync(inputName, messageHandler, userContext, _isAnEdgeModule, cancellationToken);

        /// <summary>
        /// Sets a new default delegate which applies to all endpoints. If a delegate is already associated with
        /// the input, it will be called, else the default delegate will be called. If a default delegate was set previously,
        /// it will be overwritten.
        /// </summary>
        /// <param name="messageHandler">The delegate to be called when a message is sent to any input.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The task containing the event</returns>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task SetMessageHandlerAsync(
            Func<Message, object, Task<MessageResponse>> messageHandler,
            object userContext,
            CancellationToken cancellationToken = default)
            => InternalClient.SetMessageHandlerAsync(messageHandler, userContext, _isAnEdgeModule, cancellationToken);

        /// <summary>
        /// Sets the retry policy used in the operation retries.
        /// </summary>
        /// <remarks>
        /// The change will take effect after any in-progress operations.
        /// The default is:
        /// <c>new ExponentialBackoff(int.MaxValue, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(100));</c>
        /// </remarks>
        /// <param name="retryPolicy">The retry policy.</param>
        public void SetRetryPolicy(IRetryPolicy retryPolicy)
        {
            InternalClient.SetRetryPolicy(retryPolicy);
        }

        /// <summary>
        /// Open the ModuleClient instance. Must be done before any operation can begin.
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        /// </summary>
        public Task OpenAsync(CancellationToken cancellationToken = default) => InternalClient.OpenAsync(cancellationToken);

        /// <summary>
        /// Close the client instance.
        /// </summary>
        /// <remarks>
        /// The instance can be re-opened after closing and before disposing.
        /// </remarks>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task CloseAsync(CancellationToken cancellationToken = default) => InternalClient.CloseAsync(cancellationToken);

        /// <summary>
        /// Sends an event to IoT hub. ModuleClient instance must be opened already.
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
        /// <exception cref="InvalidOperationException">Thrown if ModuleClient instance is not opened already.</exception>
        /// <exception cref="SocketException">Thrown if a socket error occurs.</exception>
        /// <exception cref="WebSocketException">Thrown if an error occurs when performing an operation on a WebSocket connection.</exception>
        /// <exception cref="IOException">Thrown if an I/O error occurs.</exception>
        /// <exception cref="ClosedChannelException">Thrown if the MQTT transport layer closes unexpectedly.</exception>
        /// <exception cref="IotHubClientException">Thrown if an error occurs when communicating with IoT hub service.
        /// If <see cref="IotHubClientException.IsTransient"/> is set to <c>true</c> then it is a transient exception.
        /// If <see cref="IotHubClientException.IsTransient"/> is set to <c>false</c> then it is a non-transient exception.</exception>
        public Task SendEventAsync(Message message, CancellationToken cancellationToken = default) => InternalClient.SendEventAsync(message, cancellationToken);

        /// <summary>
        /// Sends a batch of events to IoT hub. Use AMQP or HTTPs for a true batch operation. MQTT will just send the messages one after the other.
        /// For more information on IoT Edge module routing <see href="https://docs.microsoft.com/azure/iot-edge/module-composition?view=iotedge-2018-06#declare-routes"/>.
        /// ModuleClient instance must be opened already.
        /// </summary>
        /// <param name="messages">An IEnumerable set of Message objects.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The task containing the event</returns>
        /// <exception cref="InvalidOperationException">Thrown if ModuleClient instance is not opened already.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task SendEventBatchAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default) => InternalClient.SendEventBatchAsync(messages, cancellationToken);

        /// <summary>
        /// Sends an event to IoT hub. ModuleClient instance must be opened already.
        /// </summary>
        /// <remarks>
        /// In case of a transient issue, retrying the operation should work. In case of a non-transient issue, inspect the error details and take steps accordingly.
        /// Please note that the above list is not exhaustive.
        /// </remarks>
        /// <param name="outputName">The output target for sending the given message.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="ArgumentNullException">Thrown when a required parameter is null.</exception>
        /// <exception cref="IotHubClientException">Thrown and <see cref="IotHubClientException.StatusCode"/> is set to <see cref="IotHubStatusCode.NetworkErrors"/>
        /// when the operation has been canceled. The inner exception will be <see cref="OperationCanceledException"/>.</exception>
        /// <exception cref="IotHubClientException">Thrown and <see cref="IotHubClientException.StatusCode"/> is set to <see cref="IotHubStatusCode.NetworkErrors"/>
        /// if the client encounters a transient retryable exception. </exception>
        /// <exception cref="InvalidOperationException">Thrown if ModuleClient instance is not opened already.</exception>
        /// <exception cref="SocketException">Thrown if a socket error occurs.</exception>
        /// <exception cref="WebSocketException">Thrown if an error occurs when performing an operation on a WebSocket connection.</exception>
        /// <exception cref="IOException">Thrown if an I/O error occurs.</exception>
        /// <exception cref="ClosedChannelException">Thrown if the MQTT transport layer closes unexpectedly.</exception>
        /// <exception cref="IotHubClientException">Thrown if an error occurs when communicating with IoT hub service.
        /// If <see cref="IotHubClientException.IsTransient"/> is set to <c>true</c> then it is a transient exception.
        /// If <see cref="IotHubClientException.IsTransient"/> is set to <c>false</c> then it is a non-transient exception.</exception>
        /// <returns>The message containing the event</returns>
        public Task SendEventAsync(string outputName, Message message, CancellationToken cancellationToken = default) =>
            InternalClient.SendEventAsync(outputName, message, cancellationToken);

        /// <summary>
        /// Sends a batch of events to IoT hub. Use AMQP or HTTPs for a true batch operation. MQTT will just send the messages one after the other.
        /// For more information on IoT Edge module routing <see href="https://docs.microsoft.com/azure/iot-edge/module-composition?view=iotedge-2018-06#declare-routes"/>.
        /// ModuleClient instance must be opened already.
        /// </summary>
        /// <param name="outputName">The output target for sending the given message.</param>
        /// <param name="messages">A list of one or more messages to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The task containing the event</returns>
        /// <exception cref="InvalidOperationException">Thrown if ModuleClient instance is not opened already.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task SendEventBatchAsync(string outputName, IEnumerable<Message> messages, CancellationToken cancellationToken = default) =>
            InternalClient.SendEventBatchAsync(outputName, messages, cancellationToken);

        /// <summary>
        /// Retrieve a module twin object for the current module. ModuleClient instance must be opened already.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The module twin object for the current module</returns>
        /// <exception cref="InvalidOperationException">Thrown if ModuleClient instance is not opened already.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task<Twin> GetTwinAsync(CancellationToken cancellationToken = default) => InternalClient.GetTwinAsync(cancellationToken);

        /// <summary>
        /// Push reported property changes up to the service.
        /// </summary>
        /// <param name="reportedProperties">Reported properties to push.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties, CancellationToken cancellationToken = default) =>
            InternalClient.UpdateReportedPropertiesAsync(reportedProperties, cancellationToken);

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

        /// <summary>
        /// Deletes a received message from the module queue.
        /// </summary>
        /// <param name="lockToken">The message lockToken.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The lock identifier for the previously received message</returns>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task CompleteMessageAsync(string lockToken, CancellationToken cancellationToken = default) => InternalClient.CompleteMessageAsync(lockToken, cancellationToken);

        /// <summary>
        /// Deletes a received message from the module queue.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <param name="message">The message.</param>
        /// <returns>The previously received message</returns>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task CompleteMessageAsync(Message message, CancellationToken cancellationToken = default) => InternalClient.CompleteMessageAsync(message, cancellationToken);

        /// <summary>
        /// Puts a received message back onto the module queue.
        /// </summary>
        /// <param name="lockToken">The message lockToken.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The previously received message</returns>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task AbandonMessageAsync(string lockToken, CancellationToken cancellationToken = default) => InternalClient.AbandonMessageAsync(lockToken, cancellationToken);

        /// <summary>
        /// Puts a received message back onto the module queue.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task AbandonMessageAsync(Message message, CancellationToken cancellationToken = default) => InternalClient.AbandonMessageAsync(message, cancellationToken);

        /// <summary>
        /// Releases the unmanaged resources used by the ModuleClient and optionally disposes of the managed resources.
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
        /// Releases the unmanaged resources used by the ModuleClient and allows for any derived class to override and
        /// provide custom implementation.
        /// </summary>
        /// <param name="disposing">Setting to true will release both managed and unmanaged resources. Setting to
        /// false will only release the unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                InternalClient?.Dispose();
                InternalClient = null;
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
                    IotHubConnectionCredentials = InternalClient.IotHubConnectionCredentials,
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

                return new DirectMethodResponse()
                {
                    Status = result.Status,
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
