// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Microsoft.Azure.Devices.Client
{
    using System.Globalization;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using Microsoft.Azure.Devices.Client.Edge;
    using Microsoft.Azure.Devices.Client.Transport;
    using System;
    using System.Collections.Generic;
#if NETSTANDARD1_3
using System.Net.Http;
#endif
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client.Extensions;
    using Microsoft.Azure.Devices.Shared;

    /// <summary>
    /// Contains methods that a device can use to send messages to and receive from the service.
    /// </summary>
    public sealed class ModuleClient : IDisposable
    {
        const string ModuleMethodUriFormat = "/twins/{0}/modules/{1}/methods?" + ClientApiVersionHelper.ApiVersionQueryString;
        const string DeviceMethodUriFormat = "/twins/{0}/methods?" + ClientApiVersionHelper.ApiVersionQueryString;
        private readonly InternalClient internalClient;
        private readonly ICertificateValidator certValidator;

        internal ModuleClient(InternalClient internalClient) : this(internalClient, NullCertificateValidator.Instance)
        {
        }

        internal ModuleClient(InternalClient internalClient, ICertificateValidator certValidator)
        {
            this.internalClient = internalClient ?? throw new ArgumentNullException(nameof(internalClient));
            this.certValidator = certValidator ?? throw new ArgumentNullException(nameof(certValidator));

            if (string.IsNullOrWhiteSpace(this.internalClient.IotHubConnectionString?.ModuleId))
            {
                throw new ArgumentException("A valid module ID should be specified to create a ModuleClient");
            }
        }

        /// <summary>
        /// Create an Amqp ModuleClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient Create(string hostname, IAuthenticationMethod authenticationMethod)
        {
            return Create(() => ClientFactory.Create(hostname, authenticationMethod));
        }

        /// <summary>
        /// Create an Amqp ModuleClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="gatewayHostname">The fully-qualified DNS hostname of Gateway</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient Create(string hostname, string gatewayHostname, IAuthenticationMethod authenticationMethod)
        {
            return Create(() => ClientFactory.Create(hostname, gatewayHostname, authenticationMethod));
        }

        /// <summary>
        /// Create a ModuleClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="transportType">The transportType used (Http1 or Amqp)</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient Create(string hostname, IAuthenticationMethod authenticationMethod, TransportType transportType)
        {
            return Create(() => ClientFactory.Create(hostname, authenticationMethod, transportType));
        }

        /// <summary>
        /// Create a ModuleClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="gatewayHostname">The fully-qualified DNS hostname of Gateway</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="transportType">The transportType used (Http1 or Amqp)</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient Create(string hostname, string gatewayHostname, IAuthenticationMethod authenticationMethod, TransportType transportType)
        {
            return Create(() => ClientFactory.Create(hostname, gatewayHostname, authenticationMethod, transportType));
        }

        /// <summary>
        /// Create a ModuleClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="transportSettings">Prioritized list of transportTypes and their settings</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient Create(string hostname, IAuthenticationMethod authenticationMethod,
            ITransportSettings[] transportSettings)
        {
            return Create(() => ClientFactory.Create(hostname, authenticationMethod, transportSettings));
        }

        /// <summary>
        /// Create a ModuleClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="gatewayHostname">The fully-qualified DNS hostname of Gateway</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="transportSettings">Prioritized list of transportTypes and their settings</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient Create(string hostname, string gatewayHostname, IAuthenticationMethod authenticationMethod,
            ITransportSettings[] transportSettings)
        {
            return Create(() => ClientFactory.Create(hostname, gatewayHostname, authenticationMethod, transportSettings));
        }

        /// <summary>
        /// Create a ModuleClient using Amqp transport from the specified connection string
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (including DeviceId)</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient CreateFromConnectionString(string connectionString)
        {
            return Create(() => ClientFactory.CreateFromConnectionString(connectionString));
        }

        /// <summary>
        /// Create ModuleClient from the specified connection string using the specified transport type
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (including DeviceId)</param>
        /// <param name="transportType">Specifies whether Amqp or Http transport is used</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient CreateFromConnectionString(string connectionString, TransportType transportType)
        {
            return Create(() => ClientFactory.CreateFromConnectionString(connectionString, transportType));
        }

        /// <summary>
        /// Create ModuleClient from the specified connection string using a prioritized list of transports
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (with DeviceId)</param>
        /// <param name="transportSettings">Prioritized list of transports and their settings</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient CreateFromConnectionString(string connectionString,
            ITransportSettings[] transportSettings)
        {
            return Create(() => ClientFactory.CreateFromConnectionString(connectionString, transportSettings));
        }

        /// <summary>
        /// Creates a ModuleClient instance in an IoT Edge deployment
        /// based on environment variables.
        /// </summary>
        /// <returns>ModuleClient instance</returns>
        public static Task<ModuleClient> CreateFromEnvironmentAsync()
        {
            return CreateFromEnvironmentAsync(TransportType.Amqp);
        }

        /// <summary>
        /// Creates a ModuleClient instance in an IoT Edge deployment
        /// based on environment variables.
        /// </summary>
        /// <param name="transportType">Specifies whether Amqp or Http transport is used</param>
        /// <returns>ModuleClient instance</returns>
        public static Task<ModuleClient> CreateFromEnvironmentAsync(TransportType transportType)
        {
            return CreateFromEnvironmentAsync(ClientFactory.GetTransportSettings(transportType));
        }

        /// <summary>
        /// Creates a ModuleClient instance in an IoT Edge deployment
        /// based on environment variables.
        /// </summary>
        /// <param name="transportSettings">Prioritized list of transports and their settings</param>
        /// <returns>ModuleClient instance</returns>
        public static Task<ModuleClient> CreateFromEnvironmentAsync(ITransportSettings[] transportSettings)
        {
            return new EdgeModuleClientFactory(transportSettings, new TrustBundleProvider()).CreateAsync();
        }

        private static ModuleClient Create(Func<InternalClient> internalClientCreator)
        {
            return new ModuleClient(internalClientCreator());
        }

        internal IDelegatingHandler InnerHandler
        {
            get => this.internalClient.InnerHandler;
            set => this.internalClient.InnerHandler = value;
        }

        internal InternalClient InternalClient => this.internalClient;

        public int DiagnosticSamplingPercentage
        {
            get => this.internalClient.DiagnosticSamplingPercentage;
            set => this.internalClient.DiagnosticSamplingPercentage = value;
        }

        /// <summary>
        /// Stores the timeout used in the operation retries.
        /// </summary>
        // Codes_SRS_DEVICECLIENT_28_002: [This property shall be defaulted to 240000 (4 minutes).]
        public uint OperationTimeoutInMilliseconds
        {
            get => this.internalClient.OperationTimeoutInMilliseconds;
            set => this.internalClient.OperationTimeoutInMilliseconds = value;
        }

        /// <summary>
        /// Stores custom product information that will be appended to the user agent string that is sent to IoT Hub.
        /// </summary>
        public string ProductInfo
        {
            get => this.internalClient.ProductInfo;
            set => this.internalClient.ProductInfo = value;
        }

        /// <summary>
        /// Sets the retry policy used in the operation retries.
        /// </summary>
        /// <param name="retryPolicy">The retry policy. The default is new ExponentialBackoff(int.MaxValue, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(100));</param>
        // Codes_SRS_DEVICECLIENT_28_001: [This property shall be defaulted to the exponential retry strategy with backoff 
        // parameters for calculating delay in between retries.]
        public void SetRetryPolicy(IRetryPolicy retryPolicy)
        {
            this.internalClient.SetRetryPolicy(retryPolicy);
        }

        /// <summary>
        /// Explicitly open the DeviceClient instance.
        /// </summary>
        public Task OpenAsync() => this.internalClient.OpenAsync();

        /// <summary>
        /// Close the DeviceClient instance
        /// </summary>
        /// <returns></returns>
        public Task CloseAsync() => this.internalClient.CloseAsync();

        /// <summary>
        /// Deletes a received message from the device queue
        /// </summary>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task CompleteAsync(string lockToken) => this.internalClient.CompleteAsync(lockToken);

        /// <summary>
        /// Deletes a received message from the device queue
        /// </summary>
        /// <returns>The previously received message</returns>
        public Task CompleteAsync(Message message) => this.internalClient.CompleteAsync(message);

        /// <summary>
        /// Puts a received message back onto the device queue
        /// </summary>
        /// <returns>The previously received message</returns>
        public Task AbandonAsync(string lockToken) => this.internalClient.AbandonAsync(lockToken);

        /// <summary>
        /// Puts a received message back onto the device queue
        /// </summary>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task AbandonAsync(Message message) => this.internalClient.AbandonAsync(message);

        /// <summary>
        /// Sends an event to device hub
        /// </summary>
        /// <returns>The message containing the event</returns>
        public Task SendEventAsync(Message message) => this.internalClient.SendEventAsync(message);

        /// <summary>
        /// Sends a batch of events to device hub
        /// </summary>
        /// <returns>The task containing the event</returns>
        public Task SendEventBatchAsync(IEnumerable<Message> messages) => this.internalClient.SendEventBatchAsync(messages);

        /// <summary>
        /// Registers a new delegate for the named method. If a delegate is already associated with
        /// the named method, it will be replaced with the new delegate.
        /// <param name="methodName">The name of the method to associate with the delegate.</param>
        /// <param name="methodHandler">The delegate to be used when a method with the given name is called by the cloud service.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// </summary>
        public Task SetMethodHandlerAsync(string methodName, MethodCallback methodHandler, object userContext) =>
            this.internalClient.SetMethodHandlerAsync(methodName, methodHandler, userContext);

        /// <summary>
        /// Registers a new delegate that is called for a method that doesn't have a delegate registered for its name. 
        /// If a default delegate is already registered it will replace with the new delegate.
        /// </summary>
        /// <param name="methodHandler">The delegate to be used when a method is called by the cloud service and there is no delegate registered for that method name.</param>
        /// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
        public Task SetMethodDefaultHandlerAsync(MethodCallback methodHandler, object userContext) =>
            this.internalClient.SetMethodDefaultHandlerAsync(methodHandler, userContext);

        /// <summary>
        /// Registers a new delegate for the connection status changed callback. If a delegate is already associated, 
        /// it will be replaced with the new delegate.
        /// <param name="statusChangesHandler">The name of the method to associate with the delegate.</param>
        /// </summary>
        public void SetConnectionStatusChangesHandler(ConnectionStatusChangesHandler statusChangesHandler) =>
            this.internalClient.SetConnectionStatusChangesHandler(statusChangesHandler);

        public void Dispose() => this.internalClient?.Dispose();

        /// <summary>
        /// Set a callback that will be called whenever the client receives a state update 
        /// (desired or reported) from the service.  This has the side-effect of subscribing
        /// to the PATCH topic on the service.
        /// </summary>
        /// <param name="callback">Callback to call after the state update has been received and applied</param>
        /// <param name="userContext">Context object that will be passed into callback</param>
        public Task SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback callback, object userContext) =>
            this.internalClient.SetDesiredPropertyUpdateCallbackAsync(callback, userContext);

        /// <summary>
        /// Retrieve a device twin object for the current device.
        /// </summary>
        /// <returns>The device twin object for the current device</returns>
        public Task<Twin> GetTwinAsync() => this.internalClient.GetTwinAsync();

        /// <summary>
        /// Push reported property changes up to the service.
        /// </summary>
        /// <param name="reportedProperties">Reported properties to push</param>
        public Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties) =>
            this.internalClient.UpdateReportedPropertiesAsync(reportedProperties);

        #region Module Specific API

        /// <summary>
        /// <param name="outputName">The output target for sending the given message</param>
        /// <param name="message">The message to send</param>
        /// <returns>The message containing the event</returns>
        public Task SendEventAsync(string outputName, Message message) =>
            this.internalClient.SendEventAsync(outputName, message);

        /// <summary>
        /// Sends a batch of events to device hub
        /// <param name="outputName">The output target for sending the given message</param>
        /// <param name="messages">A list of one or more messages to send</param>
        /// </summary>
        /// <returns>The task containing the event</returns>
        public Task SendEventBatchAsync(string outputName, IEnumerable<Message> messages) =>
            this.internalClient.SendEventBatchAsync(outputName, messages);

        /// <summary>
        /// Registers a new delgate for the particular input. If a delegate is already associated with
        /// the input, it will be replaced with the new delegate.
        /// <param name="inputName">The name of the input to associate with the delegate.</param>
        /// <param name="messageHandler">The delegate to be used when a message is sent to the particular inputName.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// </summary>
        /// <returns>The task containing the event</returns>
        public Task SetInputMessageHandlerAsync(string inputName, MessageHandler messageHandler, object userContext) =>
            this.internalClient.SetInputMessageHandlerAsync(inputName, messageHandler, userContext);

        /// <summary>
        /// Registers a new default delegate which applies to all endpoints. If a delegate is already associated with
        /// the input, it will be called, else the default delegate will be called. If a default delegate was set previously,
        /// it will be overwritten.
        /// <param name="messageHandler">The delegate to be called when a message is sent to any input.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// </summary>
        /// <returns>The task containing the event</returns>
        public Task SetMessageHandlerAsync(MessageHandler messageHandler, object userContext) =>
            this.internalClient.SetMessageHandlerAsync(messageHandler, userContext);

        /// <summary>
        /// Interactively invokes a method on device
        /// </summary>
        /// <param name="deviceId">Device Id</param>
        /// <param name="methodInvokeRequest">Device method parameters (passthrough to device)</param>
        /// <returns>Method result</returns>
        public Task<MethodResponse> InvokeMethodAsync(string deviceId, MethodRequest methodInvokeRequest)
        {
            return this.InvokeMethodAsync(deviceId, methodInvokeRequest, CancellationToken.None);
        }

        /// <summary>
        /// Interactively invokes a method on device
        /// </summary>
        /// <param name="deviceId">Device Id</param>
        /// <param name="methodInvokeRequest">Device method parameters (passthrough to device)</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>Method result</returns>
        public Task<MethodResponse> InvokeMethodAsync(string deviceId, MethodRequest methodInvokeRequest, CancellationToken cancellationToken)
        {
            return InvokeMethodAsync(GetDeviceMethodUri(deviceId), methodInvokeRequest, cancellationToken);
        }

        /// <summary>
        /// Interactively invokes a method on module
        /// </summary>
        /// <param name="deviceId">Device Id</param>
        /// <param name="moduleId">Module Id</param>
        /// <param name="methodInvokeRequest">Device method parameters (passthrough to device)</param>
        /// <returns>Method result</returns>
        public Task<MethodResponse> InvokeMethodAsync(string deviceId, string moduleId, MethodRequest methodInvokeRequest)
        {
            return this.InvokeMethodAsync(deviceId, moduleId, methodInvokeRequest, CancellationToken.None);
        }

        /// <summary>
        /// Interactively invokes a method on module
        /// </summary>
        /// <param name="deviceId">Device Id</param>
        /// <param name="moduleId">Module Id</param>
        /// <param name="methodInvokeRequest">Device method parameters (passthrough to device)</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>Method result</returns>
        public Task<MethodResponse> InvokeMethodAsync(string deviceId, string moduleId, MethodRequest methodInvokeRequest, CancellationToken cancellationToken)
        {
            return InvokeMethodAsync(GetModuleMethodUri(deviceId, moduleId), methodInvokeRequest, cancellationToken);
        }

        private async Task<MethodResponse> InvokeMethodAsync(Uri uri, MethodRequest methodRequest, CancellationToken cancellationToken)
        {
            HttpClientHandler httpClientHandler = null;
            var customCertificateValidation =  this.certValidator.GetCustomCertificateValidation();

            if (customCertificateValidation != null)
            {
#if NETSTANDARD1_3 || NETSTANDARD2_0
                httpClientHandler = new HttpClientHandler();
                httpClientHandler.ServerCertificateCustomValidationCallback = customCertificateValidation;
#else
            httpClientHandler = new WebRequestHandler();
            ((WebRequestHandler)httpClientHandler).ServerCertificateValidationCallback = (sender, certificate, chain, errors) =>
            {
                return customCertificateValidation(sender, certificate, chain, errors);
            };
#endif
            }

            var context = new PipelineContext();
            context.Set(new ProductInfo() { Extra = this.InternalClient.ProductInfo });

            Http1TransportSettings transportSettings = new Http1TransportSettings();
            //We need to add the certificate to the httpTransport if DeviceAuthenticationWithX509Certificate
            if (this.internalClient.Certificate != null)
            {
                transportSettings.ClientCertificate = this.internalClient.Certificate;
            }

            HttpTransportHandler httpTransport = new HttpTransportHandler(context, this.internalClient.IotHubConnectionString, transportSettings, httpClientHandler);
            var methodInvokeRequest = new MethodInvokeRequest(methodRequest.Name, methodRequest.DataAsJson, methodRequest.ResponseTimeout, methodRequest.ConnectionTimeout);
            var result = await httpTransport.InvokeMethodAsync(methodInvokeRequest, uri, cancellationToken);
            return new MethodResponse(Encoding.UTF8.GetBytes(result.GetPayloadAsJson()), result.Status);
        }

        static Uri GetDeviceMethodUri(string deviceId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            return new Uri(string.Format(CultureInfo.InvariantCulture, DeviceMethodUriFormat, deviceId), UriKind.Relative);
        }

        static Uri GetModuleMethodUri(string deviceId, string moduleId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            moduleId = WebUtility.UrlEncode(moduleId);
            return new Uri(string.Format(CultureInfo.InvariantCulture, ModuleMethodUriFormat, deviceId, moduleId), UriKind.Relative);
        }

        #endregion Module Specific API
    }
}
