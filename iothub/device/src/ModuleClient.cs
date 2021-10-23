// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Contains methods that a module can use to send messages to and receive from the service and interact with module twins.
    /// </summary>
    public class ModuleClient : IDisposable
#if !NET451 && !NET472 && !NETSTANDARD2_0
        , IAsyncDisposable
#endif
    {
        private const string ModuleMethodUriFormat = "/twins/{0}/modules/{1}/methods?" + ClientApiVersionHelper.ApiVersionQueryStringLatest;
        private const string DeviceMethodUriFormat = "/twins/{0}/methods?" + ClientApiVersionHelper.ApiVersionQueryStringLatest;
        private bool _isEdgeModule;
        private readonly ICertificateValidator _certValidator;

        internal InternalClient InternalClient { get; private set; }

        /// <summary>
        /// Constructor for a module client to be created from an <see cref="InternalClient"/>.
        /// </summary>
        /// <param name="internalClient">The internal client to use for the commands.</param>
        /// <param name="isEdgeModule">Sets if this module client is created for an edge module.</param>
        /// <remarks>
        /// For AMQP connections an Edge module uses a different receiver than for a Module Twin. Setting the <paramref name="isEdgeModule"/> parameter will correctly select the correct AMQP link to create.
        /// </remarks>
        internal ModuleClient(InternalClient internalClient, bool isEdgeModule) : this(internalClient, NullCertificateValidator.Instance, isEdgeModule)
        {
        }

        /// <summary>
        /// Constructor for a module client to be created from an <see cref="InternalClient"/>. With a specific certificate validator.
        /// </summary>
        /// <param name="internalClient">The internal client to use for the commands.</param>
        /// <param name="certValidator">The custom certificate validator to use for connection.</param>
        /// <param name="isEdgeModule">Sets if this module client is created for an edge module.</param>
        /// <remarks>
        /// For AMQP connections a Edge Module uses a different receiver than for a Module Twin. Setting the <paramref name="isEdgeModule"/> parameter will correctly select the correct AMQP link to create.
        /// </remarks>
        internal ModuleClient(InternalClient internalClient, ICertificateValidator certValidator, bool isEdgeModule)
        {
            InternalClient = internalClient ?? throw new ArgumentNullException(nameof(internalClient));
            _certValidator = certValidator ?? throw new ArgumentNullException(nameof(certValidator));

            if (string.IsNullOrWhiteSpace(InternalClient.IotHubConnectionString?.ModuleId))
            {
                throw new ArgumentException("A valid module Id should be specified to create a ModuleClient");
            }

            // There is a distinction between a Module Twin and and Edge module. We set this flag in order
            // to correctly select the reciver link for AMQP on a Module Twin. This does not affect MQTT.
            _isEdgeModule = isEdgeModule;

            if (Logging.IsEnabled)
                Logging.Associate(this, this, internalClient, nameof(ModuleClient));
        }

        /// <summary>
        /// Creates an AMQP ModuleClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS host name of IoT Hub</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="options">The options that allow configuration of the module client instance during initialization.</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient Create(string hostname, IAuthenticationMethod authenticationMethod, ClientOptions options = default)
        {
            return Create(() => ClientFactory.Create(hostname, authenticationMethod, options));
        }

        /// <summary>
        /// Creates an AMQP ModuleClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS host name of IoT Hub</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="gatewayHostname">The fully-qualified DNS host name of Gateway</param>
        /// <param name="options">The options that allow configuration of the module client instance during initialization.</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient Create(string hostname, string gatewayHostname, IAuthenticationMethod authenticationMethod, ClientOptions options = default)
        {
            return Create(() => ClientFactory.Create(hostname, gatewayHostname, authenticationMethod, options));
        }

        /// <summary>
        /// Creates a ModuleClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS host name of IoT Hub</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="transportType">The transportType used (Http1 or AMQP)</param>
        /// <param name="options">The options that allow configuration of the module client instance during initialization.</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient Create(string hostname, IAuthenticationMethod authenticationMethod, TransportType transportType, ClientOptions options = default)
        {
            return Create(() => ClientFactory.Create(hostname, authenticationMethod, transportType, options));
        }

        /// <summary>
        /// Creates a ModuleClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS host name of IoT Hub</param>
        /// <param name="gatewayHostname">The fully-qualified DNS host name of Gateway</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="transportType">The transportType used (Http1 or AMQP)</param>
        /// <param name="options">The options that allow configuration of the module client instance during initialization.</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient Create(string hostname, string gatewayHostname, IAuthenticationMethod authenticationMethod,
            TransportType transportType, ClientOptions options = default)
        {
            return Create(() => ClientFactory.Create(hostname, gatewayHostname, authenticationMethod, transportType, options));
        }

        /// <summary>
        /// Creates a ModuleClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS host name of IoT Hub</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="transportSettings">Prioritized list of transportTypes and their settings</param>
        /// <param name="options">The options that allow configuration of the module client instance during initialization.</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient Create(string hostname, IAuthenticationMethod authenticationMethod,
            ITransportSettings[] transportSettings, ClientOptions options = default)
        {
            return Create(() => ClientFactory.Create(hostname, authenticationMethod, transportSettings, options));
        }

        /// <summary>
        /// Creates a ModuleClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS host name of IoT Hub</param>
        /// <param name="gatewayHostname">The fully-qualified DNS host name of Gateway</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="transportSettings">Prioritized list of transportTypes and their settings</param>
        /// <param name="options">The options that allow configuration of the module client instance during initialization.</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient Create(string hostname, string gatewayHostname, IAuthenticationMethod authenticationMethod,
            ITransportSettings[] transportSettings, ClientOptions options = default)
        {
            return Create(() => ClientFactory.Create(hostname, gatewayHostname, authenticationMethod, transportSettings, options));
        }

        /// <summary>
        /// Creates a ModuleClient using AMQP transport from the specified connection string
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (including DeviceId)</param>
        /// <param name="options">The options that allow configuration of the module client instance during initialization.</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient CreateFromConnectionString(string connectionString, ClientOptions options = default)
        {
            return Create(() => ClientFactory.CreateFromConnectionString(connectionString, options));
        }

        /// <summary>
        /// Creates ModuleClient from the specified connection string using the specified transport type
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (including DeviceId)</param>
        /// <param name="transportType">Specifies whether AMQP or HTTP transport is used</param>
        /// <param name="options">The options that allow configuration of the module client instance during initialization.</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient CreateFromConnectionString(string connectionString, TransportType transportType, ClientOptions options = default)
        {
            return Create(() => ClientFactory.CreateFromConnectionString(connectionString, transportType, options));
        }

        /// <summary>
        /// Creates ModuleClient from the specified connection string using a prioritized list of transports
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (with DeviceId)</param>
        /// <param name="transportSettings">Prioritized list of transports and their settings</param>
        /// <param name="options">The options that allow configuration of the module client instance during initialization.</param>
        /// <returns>ModuleClient</returns>
        public static ModuleClient CreateFromConnectionString(string connectionString,
            ITransportSettings[] transportSettings, ClientOptions options = default)
        {
            return Create(() => ClientFactory.CreateFromConnectionString(connectionString, transportSettings, options));
        }

        /// <summary>
        /// Creates a ModuleClient instance in an IoT Edge deployment
        /// based on environment variables.
        /// </summary>
        /// <param name="options">The options that allow configuration of the module client instance during initialization.</param>
        /// <returns>ModuleClient instance</returns>
        public static Task<ModuleClient> CreateFromEnvironmentAsync(ClientOptions options = default)
        {
            return CreateFromEnvironmentAsync(TransportType.Amqp, options);
        }

        /// <summary>
        /// Creates a ModuleClient instance in an IoT Edge deployment
        /// based on environment variables.
        /// </summary>
        /// <param name="transportType">Specifies whether AMQP or HTTP transport is used</param>
        /// <param name="options">The options that allow configuration of the module client instance during initialization.</param>
        /// <returns>ModuleClient instance</returns>
        public static Task<ModuleClient> CreateFromEnvironmentAsync(TransportType transportType, ClientOptions options = default)
        {
            return CreateFromEnvironmentAsync(ClientFactory.GetTransportSettings(transportType), options);
        }

        /// <summary>
        /// Creates a ModuleClient instance in an IoT Edge deployment
        /// based on environment variables.
        /// </summary>
        /// <param name="transportSettings">Prioritized list of transports and their settings</param>
        /// <param name="options">The options that allow configuration of the module client instance during initialization.</param>
        /// <returns>ModuleClient instance</returns>
        public static Task<ModuleClient> CreateFromEnvironmentAsync(ITransportSettings[] transportSettings, ClientOptions options = default)
        {
            return new EdgeModuleClientFactory(transportSettings, new TrustBundleProvider(), options).CreateAsync();
        }

        private static ModuleClient Create(Func<InternalClient> internalClientCreator)
        {
            return new ModuleClient(internalClientCreator(), false);
        }

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
        /// Stores the timeout used in the operation retries. Note that this value is ignored for operations
        /// where a cancellation token is provided. For example, SendEventAsync(Message) will use this timeout, but
        /// SendEventAsync(Message, CancellationToken) will not. The latter operation will only be canceled by the
        /// provided cancellation token.
        /// </summary>
        // Codes_SRS_DEVICECLIENT_28_002: [This property shall be defaulted to 240000 (4 minutes).]
        public uint OperationTimeoutInMilliseconds
        {
            get => InternalClient.OperationTimeoutInMilliseconds;
            set => InternalClient.OperationTimeoutInMilliseconds = value;
        }

        /// <summary>
        /// Stores custom product information that will be appended to the user agent string that is sent to IoT Hub.
        /// </summary>
        public string ProductInfo
        {
            get => InternalClient.ProductInfo;
            set => InternalClient.ProductInfo = value;
        }

        /// <summary>
        /// Sets the retry policy used in the operation retries.
        /// The change will take effect after any in-progress operations.
        /// </summary>
        /// <param name="retryPolicy">The retry policy. The default is new ExponentialBackoff(int.MaxValue, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(100));</param>
        // Codes_SRS_DEVICECLIENT_28_001: [This property shall be defaulted to the exponential retry strategy with back-off
        // parameters for calculating delay in between retries.]
        public void SetRetryPolicy(IRetryPolicy retryPolicy)
        {
            InternalClient.SetRetryPolicy(retryPolicy);
        }

        /// <summary>
        /// Explicitly open the ModuleClient instance.
        /// </summary>
        public Task OpenAsync() => InternalClient.OpenAsync();

        /// <summary>
        /// Explicitly open the ModuleClient instance.
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        /// </summary>
        public Task OpenAsync(CancellationToken cancellationToken) => InternalClient.OpenAsync(cancellationToken);

        /// <summary>
        /// Close the ModuleClient instance
        /// </summary>
        public Task CloseAsync() => InternalClient.CloseAsync();

        /// <summary>
        /// Close the ModuleClient instance
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        /// <returns></returns>
        public Task CloseAsync(CancellationToken cancellationToken) => InternalClient.CloseAsync(cancellationToken);

        /// <summary>
        /// Deletes a received message from the module queue
        /// </summary>
        /// <param name="lockToken">The message lockToken.</param>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task CompleteAsync(string lockToken) => InternalClient.CompleteAsync(lockToken);

        /// <summary>
        /// Deletes a received message from the module queue
        /// </summary>
        /// <param name="lockToken">The message lockToken.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task CompleteAsync(string lockToken, CancellationToken cancellationToken) => InternalClient.CompleteAsync(lockToken, cancellationToken);

        /// <summary>
        /// Deletes a received message from the module queue
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>The previously received message</returns>
        public Task CompleteAsync(Message message) => InternalClient.CompleteAsync(message);

        /// <summary>
        /// Deletes a received message from the module queue
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <param name="message">The message.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        /// <returns>The previously received message</returns>
        public Task CompleteAsync(Message message, CancellationToken cancellationToken) => InternalClient.CompleteAsync(message, cancellationToken);

        /// <summary>
        /// Puts a received message back onto the module queue
        /// </summary>
        /// <param name="lockToken">The message lockToken.</param>
        /// <returns>The previously received message</returns>
        public Task AbandonAsync(string lockToken) => InternalClient.AbandonAsync(lockToken);

        /// <summary>
        /// Puts a received message back onto the module queue
        /// </summary>
        /// <param name="lockToken">The message lockToken.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        /// <returns>The previously received message</returns>
        public Task AbandonAsync(string lockToken, CancellationToken cancellationToken) => InternalClient.AbandonAsync(lockToken, cancellationToken);

        /// <summary>
        /// Puts a received message back onto the module queue
        /// </summary>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task AbandonAsync(Message message) => InternalClient.AbandonAsync(message);

        /// <summary>
        /// Puts a received message back onto the module queue
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task AbandonAsync(Message message, CancellationToken cancellationToken) => InternalClient.AbandonAsync(message, cancellationToken);

        /// <summary>
        /// Sends an event to IoT hub
        /// </summary>
        /// <param name="message">The message.</param>
        /// <exception cref="ArgumentNullException">Thrown when a required parameter is null.</exception>
        /// <exception cref="TimeoutException">Thrown if the service does not respond to the request within the timeout specified for the operation.
        /// The timeout values are largely transport protocol specific. Check the corresponding transport settings to see if they can be configured.
        /// The operation timeout for the client can be set using <see cref="OperationTimeoutInMilliseconds"/>.</exception>
        /// <exception cref="IotHubCommunicationException">Thrown if the client encounters a transient retryable exception. </exception>
        /// <exception cref="SocketException">Thrown if a socket error occurs.</exception>
        /// <exception cref="WebSocketException">Thrown if an error occurs when performing an operation on a WebSocket connection.</exception>
        /// <exception cref="IOException">Thrown if an I/O error occurs.</exception>
        /// <exception cref="ClosedChannelException">Thrown if the MQTT transport layer closes unexpectedly.</exception>
        /// <exception cref="IotHubException">Thrown if an error occurs when communicating with IoT Hub service.
        /// If <see cref="IotHubException.IsTransient"/> is set to <c>true</c> then it is a transient exception.
        /// If <see cref="IotHubException.IsTransient"/> is set to <c>false</c> then it is a non-transient exception.</exception>
        /// <remarks>
        /// In case of a transient issue, retrying the operation should work. In case of a non-transient issue, inspect the error details and take steps accordingly.
        /// Please note that the list of exceptions is not exhaustive.
        /// </remarks>
        /// <returns>The message containing the event</returns>
        public Task SendEventAsync(Message message) => InternalClient.SendEventAsync(message);

        /// <summary>
        /// Sends an event to IoT hub
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="ArgumentNullException">Thrown when a required parameter is null.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the service does not respond to the request before the expiration of the passed <see cref="CancellationToken"/>.
        /// If a cancellation token is not supplied to the operation call, a cancellation token with an expiration time of 4 minutes is used.
        /// </exception>
        /// <exception cref="IotHubCommunicationException">Thrown if the client encounters a transient retryable exception. </exception>
        /// <exception cref="SocketException">Thrown if a socket error occurs.</exception>
        /// <exception cref="WebSocketException">Thrown if an error occurs when performing an operation on a WebSocket connection.</exception>
        /// <exception cref="IOException">Thrown if an I/O error occurs.</exception>
        /// <exception cref="ClosedChannelException">Thrown if the MQTT transport layer closes unexpectedly.</exception>
        /// <exception cref="IotHubException">Thrown if an error occurs when communicating with IoT Hub service.
        /// If <see cref="IotHubException.IsTransient"/> is set to <c>true</c> then it is a transient exception.
        /// If <see cref="IotHubException.IsTransient"/> is set to <c>false</c> then it is a non-transient exception.</exception>
        /// <remarks>
        /// In case of a transient issue, retrying the operation should work. In case of a non-transient issue, inspect the error details and take steps accordingly.
        /// Please note that the list of exceptions is not exhaustive.
        /// </remarks>
        /// <returns>The message containing the event</returns>
        public Task SendEventAsync(Message message, CancellationToken cancellationToken) => InternalClient.SendEventAsync(message, cancellationToken);

        /// <summary>
        /// Sends a batch of events to IoT hub. Use AMQP or HTTPs for a true batch operation. MQTT will just send the messages one after the other.
        /// For more information on IoT Edge module routing <see href="https://docs.microsoft.com/en-us/azure/iot-edge/module-composition?view=iotedge-2018-06#declare-routes"/>
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns>The task containing the event</returns>
        public Task SendEventBatchAsync(IEnumerable<Message> messages) => InternalClient.SendEventBatchAsync(messages);

        /// <summary>
        /// Sends a batch of events to IoT hub. Use AMQP or HTTPs for a true batch operation. MQTT will just send the messages one after the other.
        /// For more information on IoT Edge module routing <see href="https://docs.microsoft.com/en-us/azure/iot-edge/module-composition?view=iotedge-2018-06#declare-routes"/>/// Sends a batch of events to IoT hub. Requires AMQP or AMQP over WebSockets.
        /// </summary>
        /// <param name="messages">An IEnumerable set of Message objects.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        /// <returns>The task containing the event</returns>
        public Task SendEventBatchAsync(IEnumerable<Message> messages, CancellationToken cancellationToken) => InternalClient.SendEventBatchAsync(messages, cancellationToken);

        /// <summary>
        /// Sets a new delegate for the named method. If a delegate is already associated with
        /// the named method, it will be replaced with the new delegate.
        /// A method handler can be unset by passing a null MethodCallback.
        /// <param name="methodName">The name of the method to associate with the delegate.</param>
        /// <param name="methodHandler">The delegate to be used when a method with the given name is called by the cloud service.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// </summary>
        public Task SetMethodHandlerAsync(string methodName, MethodCallback methodHandler, object userContext) =>
            InternalClient.SetMethodHandlerAsync(methodName, methodHandler, userContext);

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
        public Task SetMethodHandlerAsync(string methodName, MethodCallback methodHandler, object userContext, CancellationToken cancellationToken) =>
            InternalClient.SetMethodHandlerAsync(methodName, methodHandler, userContext, cancellationToken);

        /// <summary>
        /// Sets a new delegate that is called for a method that doesn't have a delegate registered for its name.
        /// If a default delegate is already registered it will replace with the new delegate.
        /// A method handler can be unset by passing a null MethodCallback.
        /// </summary>
        /// <param name="methodHandler">The delegate to be used when a method is called by the cloud service and there is no delegate registered for that method name.</param>
        /// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
        public Task SetMethodDefaultHandlerAsync(MethodCallback methodHandler, object userContext) =>
            InternalClient.SetMethodDefaultHandlerAsync(methodHandler, userContext);

        /// <summary>
        /// Sets a new delegate that is called for a method that doesn't have a delegate registered for its name.
        /// If a default delegate is already registered it will replace with the new delegate.
        /// A method handler can be unset by passing a null MethodCallback.
        /// </summary>
        /// <param name="methodHandler">The delegate to be used when a method is called by the cloud service and there is no delegate registered for that method name.</param>
        /// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task SetMethodDefaultHandlerAsync(MethodCallback methodHandler, object userContext, CancellationToken cancellationToken) =>
            InternalClient.SetMethodDefaultHandlerAsync(methodHandler, userContext, cancellationToken);

        /// <summary>
        /// Sets a new delegate for the connection status changed callback. If a delegate is already associated,
        /// it will be replaced with the new delegate. Note that this callback will never be called if the client is configured to use HTTP as that protocol is stateless
        /// <param name="statusChangesHandler">The name of the method to associate with the delegate.</param>
        /// </summary>
        public void SetConnectionStatusChangesHandler(ConnectionStatusChangesHandler statusChangesHandler) =>
            InternalClient.SetConnectionStatusChangesHandler(statusChangesHandler);

        /// <summary>
        /// Releases the unmanaged resources used by the ModuleClient and optionally disposes of the managed resources.
        /// </summary>
        /// <remarks>
        /// The method <see cref="CloseAsync()"/> should be called before disposing.
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

#if !NET451 && !NET472 && !NETSTANDARD2_0
        // IAsyncDisposable is available in .NET Standard 2.1 and above

        /// <summary>
        /// Disposes the client in an async way. See <see cref="IAsyncDisposable"/> for more information.
        /// </summary>
        /// <remarks>
        /// Includes a call to <see cref="CloseAsync()"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// await using var client = ModuleClient.CreateFromConnectionString(...);
        /// </code>
        /// or
        /// <code>
        /// var client = ModuleClient.CreateFromConnectionString(...);
        /// try
        /// {
        ///     // do work
        /// }
        /// finally
        /// {
        ///     await client.DisposeAsync();
        /// }
        /// </code>
        /// </example>
        [SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "SuppressFinalize is called by Dispose(), which this method calls.")]
        public async ValueTask DisposeAsync()
        {
            await CloseAsync().ConfigureAwait(false);
            Dispose();
        }

#endif

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

        /// <summary>
        /// Set a callback that will be called whenever the client receives a state update
        /// (desired or reported) from the service.
        /// Set callback value to null to clear.
        /// </summary>
        /// <remarks>
        /// This has the side-effect of subscribing to the PATCH topic on the service.
        /// </remarks>
        /// <param name="callback">Callback to call after the state update has been received and applied</param>
        /// <param name="userContext">Context object that will be passed into callback</param>
        public Task SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback callback, object userContext) =>
            InternalClient.SetDesiredPropertyUpdateCallbackAsync(callback, userContext);

        /// <summary>
        /// Set a callback that will be called whenever the client receives a state update
        /// (desired or reported) from the service.
        /// Set callback value to null to clear.
        /// </summary>
        /// <remarks>
        /// This has the side-effect of subscribing to the PATCH topic on the service.
        /// </remarks>
        /// <param name="callback">Callback to call after the state update has been received and applied</param>
        /// <param name="userContext">Context object that will be passed into callback</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback callback, object userContext, CancellationToken cancellationToken) =>
            InternalClient.SetDesiredPropertyUpdateCallbackAsync(callback, userContext, cancellationToken);

        /// <summary>
        /// Retrieve a module twin object for the current module.
        /// </summary>
        /// <returns>The module twin object for the current module</returns>
        public Task<Twin> GetTwinAsync() => InternalClient.GetTwinAsync();

        /// <summary>
        /// Retrieve a module twin object for the current module.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        /// <returns>The module twin object for the current module</returns>
        public Task<Twin> GetTwinAsync(CancellationToken cancellationToken) => InternalClient.GetTwinAsync(cancellationToken);

        /// <summary>
        /// Push reported property changes up to the service.
        /// </summary>
        /// <param name="reportedProperties">Reported properties to push</param>
        public Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties) =>
            InternalClient.UpdateReportedPropertiesAsync(reportedProperties);

        /// <summary>
        /// Push reported property changes up to the service.
        /// </summary>
        /// <param name="reportedProperties">Reported properties to push</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        public Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties, CancellationToken cancellationToken) =>
            InternalClient.UpdateReportedPropertiesAsync(reportedProperties, cancellationToken);

        #region Module Specific API

        // APIs that are available only in module client

        /// <summary>
        /// Sends an event to IoT hub.
        /// </summary>
        /// <param name="outputName">The output target for sending the given message</param>
        /// <param name="message">The message to send</param>
        /// <exception cref="ArgumentNullException">Thrown when a required parameter is null.</exception>
        /// <exception cref="TimeoutException">Thrown if the service does not respond to the request within the timeout specified for the operation.
        /// The timeout values are largely transport protocol specific. Check the corresponding transport settings to see if they can be configured.
        /// The operation timeout for the client can be set using <see cref="OperationTimeoutInMilliseconds"/>.</exception>
        /// <exception cref="IotHubCommunicationException">Thrown if the client encounters a transient retryable exception. </exception>
        /// <exception cref="SocketException">Thrown if a socket error occurs.</exception>
        /// <exception cref="WebSocketException">Thrown if an error occurs when performing an operation on a WebSocket connection.</exception>
        /// <exception cref="IOException">Thrown if an I/O error occurs.</exception>
        /// <exception cref="ClosedChannelException">Thrown if the MQTT transport layer closes unexpectedly.</exception>
        /// <exception cref="IotHubException">Thrown if an error occurs when communicating with IoT Hub service.
        /// If <see cref="IotHubException.IsTransient"/> is set to <c>true</c> then it is a transient exception.
        /// If <see cref="IotHubException.IsTransient"/> is set to <c>false</c> then it is a non-transient exception.</exception>
        /// <remarks>
        /// In case of a transient issue, retrying the operation should work. In case of a non-transient issue, inspect the error details and take steps accordingly.
        /// Please note that the above list is not exhaustive.
        /// </remarks>
        /// <returns>The message containing the event</returns>
        public Task SendEventAsync(string outputName, Message message) =>
            InternalClient.SendEventAsync(outputName, message);

        /// <summary>
        /// Sends an event to IoT hub.
        /// </summary>
        /// <param name="outputName">The output target for sending the given message</param>
        /// <param name="message">The message to send</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="ArgumentNullException">Thrown when a required parameter is null.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the service does not respond to the request before the expiration of the passed <see cref="CancellationToken"/>.
        /// If a cancellation token is not supplied to the operation call, a cancellation token with an expiration time of 4 minutes is used.
        /// </exception>
        /// <exception cref="IotHubCommunicationException">Thrown if the client encounters a transient retryable exception. </exception>
        /// <exception cref="SocketException">Thrown if a socket error occurs.</exception>
        /// <exception cref="WebSocketException">Thrown if an error occurs when performing an operation on a WebSocket connection.</exception>
        /// <exception cref="IOException">Thrown if an I/O error occurs.</exception>
        /// <exception cref="ClosedChannelException">Thrown if the MQTT transport layer closes unexpectedly.</exception>
        /// <exception cref="IotHubException">Thrown if an error occurs when communicating with IoT Hub service.
        /// If <see cref="IotHubException.IsTransient"/> is set to <c>true</c> then it is a transient exception.
        /// If <see cref="IotHubException.IsTransient"/> is set to <c>false</c> then it is a non-transient exception.</exception>
        /// <remarks>
        /// In case of a transient issue, retrying the operation should work. In case of a non-transient issue, inspect the error details and take steps accordingly.
        /// Please note that the above list is not exhaustive.
        /// </remarks>
        /// <returns>The message containing the event</returns>
        public Task SendEventAsync(string outputName, Message message, CancellationToken cancellationToken) =>
            InternalClient.SendEventAsync(outputName, message, cancellationToken);

        /// <summary>
        /// Sends a batch of events to IoT hub. Use AMQP or HTTPs for a true batch operation. MQTT will just send the messages one after the other.
        /// For more information on IoT Edge module routing <see href="https://docs.microsoft.com/en-us/azure/iot-edge/module-composition?view=iotedge-2018-06#declare-routes"/>
        /// </summary>
        /// <param name="outputName">The output target for sending the given message</param>
        /// <param name="messages">A list of one or more messages to send</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        /// <returns>The task containing the event</returns>
        public Task SendEventBatchAsync(string outputName, IEnumerable<Message> messages) =>
            InternalClient.SendEventBatchAsync(outputName, messages);

        /// <summary>
        /// Sends a batch of events to IoT hub. Use AMQP or HTTPs for a true batch operation. MQTT will just send the messages one after the other.
        /// For more information on IoT Edge module routing <see href="https://docs.microsoft.com/en-us/azure/iot-edge/module-composition?view=iotedge-2018-06#declare-routes"/>
        /// </summary>
        /// <param name="outputName">The output target for sending the given message</param>
        /// <param name="messages">A list of one or more messages to send</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        /// <returns>The task containing the event</returns>
        public Task SendEventBatchAsync(string outputName, IEnumerable<Message> messages, CancellationToken cancellationToken) =>
            InternalClient.SendEventBatchAsync(outputName, messages, cancellationToken);

        /// <summary>
        /// Sets a new delegate for the particular input. If a delegate is already associated with
        /// the input, it will be replaced with the new delegate.
        /// </summary>
        /// <param name="inputName">The name of the input to associate with the delegate.</param>
        /// <param name="messageHandler">The delegate to be used when a message is sent to the particular inputName.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        /// <returns>The task containing the event</returns>
        public Task SetInputMessageHandlerAsync(string inputName, MessageHandler messageHandler, object userContext) =>
            InternalClient.SetInputMessageHandlerAsync(inputName, messageHandler, userContext, _isEdgeModule);

        /// <summary>
        /// Sets a new delegate for the particular input. If a delegate is already associated with
        /// the input, it will be replaced with the new delegate.
        /// </summary>
        /// <param name="inputName">The name of the input to associate with the delegate.</param>
        /// <param name="messageHandler">The delegate to be used when a message is sent to the particular inputName.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        /// <returns>The task containing the event</returns>
        public Task SetInputMessageHandlerAsync(string inputName, MessageHandler messageHandler, object userContext, CancellationToken cancellationToken) =>
            InternalClient.SetInputMessageHandlerAsync(inputName, messageHandler, userContext, _isEdgeModule, cancellationToken);

        /// <summary>
        /// Sets a new default delegate which applies to all endpoints. If a delegate is already associated with
        /// the input, it will be called, else the default delegate will be called. If a default delegate was set previously,
        /// it will be overwritten.
        /// </summary>
        /// <param name="messageHandler">The delegate to be called when a message is sent to any input.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        /// <returns>The task containing the event</returns>
        public Task SetMessageHandlerAsync(MessageHandler messageHandler, object userContext) =>
            InternalClient.SetMessageHandlerAsync(messageHandler, userContext, _isEdgeModule);

        /// <summary>
        /// Sets a new default delegate which applies to all endpoints. If a delegate is already associated with
        /// the input, it will be called, else the default delegate will be called. If a default delegate was set previously,
        /// it will be overwritten.
        /// </summary>
        /// <param name="messageHandler">The delegate to be called when a message is sent to any input.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        /// <returns>The task containing the event</returns>
        public Task SetMessageHandlerAsync(MessageHandler messageHandler, object userContext, CancellationToken cancellationToken) =>
            InternalClient.SetMessageHandlerAsync(messageHandler, userContext, _isEdgeModule, cancellationToken);

        /// <summary>
        /// Interactively invokes a method from an edge module to an edge device.
        /// Both the edge module and the edge device need to be connected to the same edge hub.
        /// </summary>
        /// <param name="deviceId">The unique identifier of the edge device to invoke the method on.</param>
        /// <param name="methodRequest">The details of the method to invoke.</param>
        /// <returns>The result of the method invocation.</returns>
        public Task<MethodResponse> InvokeMethodAsync(string deviceId, MethodRequest methodRequest) =>
            InvokeMethodAsync(deviceId, methodRequest, CancellationToken.None);

        /// <summary>
        /// Interactively invokes a method from an edge module to an edge device.
        /// Both the edge module and the edge device need to be connected to the same edge hub.
        /// </summary>
        /// <param name="deviceId">The unique identifier of the edge device to invoke the method on.</param>
        /// <param name="methodRequest">The details of the method to invoke.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        /// <returns>The result of the method invocation.</returns>
        public Task<MethodResponse> InvokeMethodAsync(string deviceId, MethodRequest methodRequest, CancellationToken cancellationToken)
        {
            methodRequest.ThrowIfNull(nameof(methodRequest));
            return InvokeMethodAsync(GetDeviceMethodUri(deviceId), methodRequest, cancellationToken);
        }

        /// <summary>
        /// Interactively invokes a method from an edge module to a different edge module.
        /// Both of the edge modules need to be connected to the same edge hub.
        /// </summary>
        /// <param name="deviceId">The unique identifier of the device.</param>
        /// <param name="moduleId">The unique identifier of the edge module to invoke the method on.</param>
        /// <param name="methodRequest">The details of the method to invoke.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        /// <returns>The result of the method invocation.</returns>
        public Task<MethodResponse> InvokeMethodAsync(string deviceId, string moduleId, MethodRequest methodRequest) =>
            InvokeMethodAsync(deviceId, moduleId, methodRequest, CancellationToken.None);

        /// <summary>
        /// Interactively invokes a method from an edge module to a different edge module.
        /// Both of the edge modules need to be connected to the same edge hub.
        /// </summary>
        /// <param name="deviceId">The unique identifier of the device.</param>
        /// <param name="moduleId">The unique identifier of the edge module to invoke the method on.</param>
        /// <param name="methodRequest">The details of the method to invoke.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>
        /// <returns>The result of the method invocation.</returns>
        public Task<MethodResponse> InvokeMethodAsync(string deviceId, string moduleId, MethodRequest methodRequest, CancellationToken cancellationToken)
        {
            methodRequest.ThrowIfNull(nameof(methodRequest));
            return InvokeMethodAsync(GetModuleMethodUri(deviceId, moduleId), methodRequest, cancellationToken);
        }

        private async Task<MethodResponse> InvokeMethodAsync(Uri uri, MethodRequest methodRequest, CancellationToken cancellationToken)
        {
            HttpClientHandler httpClientHandler = null;
            Func<object, X509Certificate, X509Chain, SslPolicyErrors, bool> customCertificateValidation = _certValidator.GetCustomCertificateValidation();

            try
            {
                if (customCertificateValidation != null)
                {
                    TlsVersions.Instance.SetLegacyAcceptableVersions();

#if !NET451
                    httpClientHandler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = customCertificateValidation,
                        SslProtocols = TlsVersions.Instance.Preferred,
                    };
#else
                    httpClientHandler = new WebRequestHandler();
                    ((WebRequestHandler)httpClientHandler).ServerCertificateValidationCallback = (sender, certificate, chain, errors) =>
                    {
                        return customCertificateValidation(sender, certificate, chain, errors);
                    };
#endif
                }

                var context = new PipelineContext();
                context.Set(new ProductInfo { Extra = InternalClient.ProductInfo });

                var transportSettings = new Http1TransportSettings();
                //We need to add the certificate to the httpTransport if DeviceAuthenticationWithX509Certificate
                if (InternalClient.Certificate != null)
                {
                    transportSettings.ClientCertificate = InternalClient.Certificate;
                }

                using var httpTransport = new HttpTransportHandler(context, InternalClient.IotHubConnectionString, transportSettings, httpClientHandler);
                var methodInvokeRequest = new MethodInvokeRequest(methodRequest.Name, methodRequest.DataAsJson, methodRequest.ResponseTimeout, methodRequest.ConnectionTimeout);
                MethodInvokeResponse result = await httpTransport.InvokeMethodAsync(methodInvokeRequest, uri, cancellationToken).ConfigureAwait(false);

                return new MethodResponse(Encoding.UTF8.GetBytes(result.GetPayloadAsJson()), result.Status);
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

        #endregion Module Specific API
    }
}
