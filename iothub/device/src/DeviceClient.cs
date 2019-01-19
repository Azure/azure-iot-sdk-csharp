// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
#if NETSTANDARD1_3
    using System.Net.Http;
#endif
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Shared;

    /// <summary>
    /// Contains methods that a device can use to send messages to and receive from the service.
    /// </summary>
    public sealed class DeviceClient : IDisposable
    {

        /// <summary>
        /// Default operation timeout.
        /// </summary>
        public const uint DefaultOperationTimeoutInMilliseconds = 4 * 60 * 1000;

        private readonly InternalClient internalClient;

        private DeviceClient(InternalClient internalClient)
        {
            this.internalClient = internalClient ?? throw new ArgumentNullException(nameof(internalClient));

            if (this.internalClient.IotHubConnectionString?.ModuleId != null)
            {
                throw new ArgumentException("A module ID was specified in the connection string - please use ModuleClient for modules.");
            }
        }

        /// <summary>
        /// Create an Amqp DeviceClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <returns>DeviceClient</returns>
        public static DeviceClient Create(string hostname, IAuthenticationMethod authenticationMethod)
        {
            return Create(() => ClientFactory.Create(hostname, authenticationMethod));
        }

        /// <summary>
        /// Create an Amqp DeviceClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="gatewayHostname">The fully-qualified DNS hostname of Gateway</param>
        /// <returns>DeviceClient</returns>
        public static DeviceClient Create(string hostname, string gatewayHostname, IAuthenticationMethod authenticationMethod)
        {
            return Create(() => ClientFactory.Create(hostname, gatewayHostname, authenticationMethod));
        }

        /// <summary>
        /// Create a DeviceClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="transportType">The transportType used (Http1 or Amqp)</param>
        /// <returns>DeviceClient</returns>
        public static DeviceClient Create(string hostname, IAuthenticationMethod authenticationMethod, TransportType transportType)
        {
            return Create(() => ClientFactory.Create(hostname, authenticationMethod, transportType));
        }

        /// <summary>
        /// Create a DeviceClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="gatewayHostname">The fully-qualified DNS hostname of Gateway</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="transportType">The transportType used (Http1 or Amqp)</param>
        /// <returns>DeviceClient</returns>
        public static DeviceClient Create(string hostname, string gatewayHostname, IAuthenticationMethod authenticationMethod, TransportType transportType)
        {
            return Create(() => ClientFactory.Create(hostname, gatewayHostname, authenticationMethod, transportType));
        }

        /// <summary>
        /// Create a DeviceClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="transportSettings">Prioritized list of transportTypes and their settings</param>
        /// <returns>DeviceClient</returns>
        public static DeviceClient Create(string hostname, IAuthenticationMethod authenticationMethod,
            ITransportSettings[] transportSettings)
        {
            return Create(() => ClientFactory.Create(hostname, authenticationMethod, transportSettings));
        }

        /// <summary>
        /// Create a DeviceClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="gatewayHostname">The fully-qualified DNS hostname of Gateway</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="transportSettings">Prioritized list of transportTypes and their settings</param>
        /// <returns>DeviceClient</returns>
        public static DeviceClient Create(string hostname, string gatewayHostname, IAuthenticationMethod authenticationMethod,
            ITransportSettings[] transportSettings)
        {
            return Create(() => ClientFactory.Create(hostname, gatewayHostname, authenticationMethod, transportSettings));
        }

        /// <summary>
        /// Create a DeviceClient using Amqp transport from the specified connection string
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (including DeviceId)</param>
        /// <returns>DeviceClient</returns>
        public static DeviceClient CreateFromConnectionString(string connectionString)
        {
            return Create(() => ClientFactory.CreateFromConnectionString(connectionString));
        }

        /// <summary>
        /// Create a DeviceClient using Amqp transport from the specified connection string
        /// </summary>
        /// <param name="connectionString">IoT Hub-Scope Connection string for the IoT hub (without DeviceId)</param>
        /// <param name="deviceId">Id of the Device</param>
        /// <returns>DeviceClient</returns>
        public static DeviceClient CreateFromConnectionString(string connectionString, string deviceId)
        {
            return Create(() => ClientFactory.CreateFromConnectionString(connectionString, deviceId));
        }

        /// <summary>
        /// Create DeviceClient from the specified connection string using the specified transport type
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (including DeviceId)</param>
        /// <param name="transportType">Specifies whether Amqp or Http transport is used</param>
        /// <returns>DeviceClient</returns>
        public static DeviceClient CreateFromConnectionString(string connectionString, TransportType transportType)
        {
            return Create(() => ClientFactory.CreateFromConnectionString(connectionString, transportType));
        }

        /// <summary>
        /// Create DeviceClient from the specified connection string using the specified transport type
        /// </summary>
        /// <param name="connectionString">IoT Hub-Scope Connection string for the IoT hub (without DeviceId)</param>
        /// <param name="deviceId">Id of the device</param>
        /// <param name="transportType">The transportType used (Http1 or Amqp)</param>
        /// <returns>DeviceClient</returns>
        public static DeviceClient CreateFromConnectionString(string connectionString, string deviceId, TransportType transportType)
        {
            return Create(() => ClientFactory.CreateFromConnectionString(connectionString, deviceId, transportType));
        }

        /// <summary>
        /// Create DeviceClient from the specified connection string using a prioritized list of transports
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (with DeviceId)</param>
        /// <param name="transportSettings">Prioritized list of transports and their settings</param>
        /// <returns>DeviceClient</returns>
        public static DeviceClient CreateFromConnectionString(string connectionString,
            ITransportSettings[] transportSettings)
        {
            return Create(() => ClientFactory.CreateFromConnectionString(connectionString, transportSettings));
        }
        
        /// <summary>
        /// Create DeviceClient from the specified connection string using the prioritized list of transports
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (without DeviceId)</param>
        /// <param name="deviceId">Id of the device</param>
        /// <param name="transportSettings">Prioritized list of transportTypes and their settings</param>
        /// <returns>DeviceClient</returns>
        public static DeviceClient CreateFromConnectionString(string connectionString, string deviceId,
            ITransportSettings[] transportSettings)
        {
            return Create(() => ClientFactory.CreateFromConnectionString(connectionString, deviceId, transportSettings));
        }

        private static DeviceClient Create(Func<InternalClient> internalClientCreator)
        {
            return new DeviceClient(internalClientCreator());
        }

        internal IDelegatingHandler InnerHandler
        {
            get => this.internalClient.InnerHandler;
            set => this.internalClient.InnerHandler = value;
        }

        internal InternalClient InternalClient => this.internalClient;

        /// <summary> 
        /// Diagnostic sampling percentage value, [0-100];  
        /// 0 means no message will carry on diag info 
        /// </summary>
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
        /// Stores the retry strategy used in the operation retries.
        /// </summary>
        // Codes_SRS_DEVICECLIENT_28_001: [This property shall be defaulted to the exponential retry strategy with backoff 
        // parameters for calculating delay in between retries.]
        [Obsolete("This method has been deprecated.  Please use Microsoft.Azure.Devices.Client.SetRetryPolicy(IRetryPolicy retryPolicy) instead.")]
        public RetryPolicyType RetryPolicy
        {
            get => this.internalClient.RetryPolicy;
            set => this.internalClient.RetryPolicy = value;
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
        /// Explicitly open the DeviceClient instance.
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>  
        /// </summary>
        public Task OpenAsync(CancellationToken cancellationToken) => this.internalClient.OpenAsync(cancellationToken);

        /// <summary>
        /// Close the DeviceClient instance
        /// </summary>
        public Task CloseAsync() => this.internalClient.CloseAsync();

        /// <summary>
        /// Close the DeviceClient instance
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>  
        /// <returns></returns>
        public Task CloseAsync(CancellationToken cancellationToken) => this.internalClient.CloseAsync(cancellationToken);

        /// <summary>
        /// Receive a message from the device queue using the default timeout.
        /// </summary>
        /// <returns>The receive message or null if there was no message until the default timeout</returns>
        public Task<Message> ReceiveAsync() => this.internalClient.ReceiveAsync();

        /// <summary>
        /// Receive a message from the device queue using the default timeout.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>  
        /// <returns>The receive message or null if there was no message until the default timeout</returns>
        public Task<Message> ReceiveAsync(CancellationToken cancellationToken) => this.internalClient.ReceiveAsync(cancellationToken);

        /// <summary>
        /// Receive a message from the device queue with the specified timeout
        /// </summary>
        /// <returns>The receive message or null if there was no message until the specified time has elapsed</returns>
        public Task<Message> ReceiveAsync(TimeSpan timeout) => this.internalClient.ReceiveAsync(timeout);

        /// <summary>
        /// Deletes a received message from the device queue
        /// </summary>
        /// <param name="lockToken">The message lockToken.</param>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task CompleteAsync(string lockToken) => this.internalClient.CompleteAsync(lockToken);

        /// <summary>
        /// Deletes a received message from the device queue
        /// </summary>
        /// <param name="lockToken">The message lockToken.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The lock identifier for the previously received message</returns>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>  
        public Task CompleteAsync(string lockToken, CancellationToken cancellationToken) => this.internalClient.CompleteAsync(lockToken, cancellationToken);

        /// <summary>
        /// Deletes a received message from the device queue
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>The previously received message</returns>
        public Task CompleteAsync(Message message) => this.internalClient.CompleteAsync(message);

        /// <summary>
        /// Deletes a received message from the device queue
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>  
        /// <returns>The previously received message</returns>
        public Task CompleteAsync(Message message, CancellationToken cancellationToken) => this.internalClient.CompleteAsync(message, cancellationToken);

        /// <summary>
        /// Puts a received message back onto the device queue
        /// </summary>
        /// <param name="lockToken">The message lockToken.</param>
        /// <returns>The previously received message</returns>
        public Task AbandonAsync(string lockToken) => this.internalClient.AbandonAsync(lockToken);

        /// <summary>
        /// Puts a received message back onto the device queue
        /// </summary>
        /// <param name="lockToken">The message lockToken.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>  
        /// <returns>The previously received message</returns>
        public Task AbandonAsync(string lockToken, CancellationToken cancellationToken) => this.internalClient.AbandonAsync(lockToken, cancellationToken);

        /// <summary>
        /// Puts a received message back onto the device queue
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task AbandonAsync(Message message) => this.internalClient.AbandonAsync(message);

        /// <summary>
        /// Puts a received message back onto the device queue
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>  
        /// <returns>The lock identifier for the previously received message</returns>
        public Task AbandonAsync(Message message, CancellationToken cancellationToken) => this.internalClient.AbandonAsync(message, cancellationToken);

        /// <summary>
        /// Deletes a received message from the device queue and indicates to the server that the message could not be processed.
        /// </summary>
        /// <param name="lockToken">The message lockToken.</param>
        /// <returns>The previously received message</returns>
        public Task RejectAsync(string lockToken) => this.internalClient.RejectAsync(lockToken);

        /// <summary>
        /// Deletes a received message from the device queue and indicates to the server that the message could not be processed.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <param name="lockToken">The message lockToken.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>  
        /// <returns>The previously received message</returns>
        public Task RejectAsync(string lockToken, CancellationToken cancellationToken) => this.internalClient.RejectAsync(lockToken, cancellationToken);

        /// <summary>
        /// Deletes a received message from the device queue and indicates to the server that the message could not be processed.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task RejectAsync(Message message) => this.internalClient.RejectAsync(message);

        /// <summary>
        /// Deletes a received message from the device queue and indicates to the server that the message could not be processed.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>  
        /// <returns>The lock identifier for the previously received message</returns>
        public Task RejectAsync(Message message, CancellationToken cancellationToken) => this.internalClient.RejectAsync(message, cancellationToken);


        /// <summary>
        /// Sends an event to device hub
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>The message containing the event</returns>
        public Task SendEventAsync(Message message) => this.internalClient.SendEventAsync(message);

        /// <summary>
        /// Sends an event to device hub
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>  
        /// <returns>The message containing the event</returns>
        public Task SendEventAsync(Message message, CancellationToken cancellationToken) => this.internalClient.SendEventAsync(message, cancellationToken);

        /// <summary>
        /// Sends a batch of events to device hub
        /// </summary>
        /// <param name="messages">A list of one or more messages to send</param>
        /// <returns>The task containing the event</returns>
        public Task SendEventBatchAsync(IEnumerable<Message> messages) => this.internalClient.SendEventBatchAsync(messages);

        /// <summary>
        /// Sends a batch of events to device hub
        /// </summary>
        /// <param name="messages">An IEnumerable set of Message objects.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>  
        /// <returns>The task containing the event</returns>
        public Task SendEventBatchAsync(IEnumerable<Message> messages, CancellationToken cancellationToken) => this.internalClient.SendEventBatchAsync(messages, cancellationToken);

        /// <summary>
        /// Uploads a stream to a block blob in a storage account associated with the IoTHub for that device.
        /// If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="blobName"></param>
        /// <param name="source"></param>
        /// <returns>AsncTask</returns>
        public Task UploadToBlobAsync(String blobName, System.IO.Stream source) =>
            this.internalClient.UploadToBlobAsync(blobName, source);

        /// <summary>
        /// Uploads a stream to a block blob in a storage account associated with the IoTHub for that device.
        /// If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="blobName"></param>
        /// <param name="source"></param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>  
        /// <returns>AsncTask</returns>
        public Task UploadToBlobAsync(String blobName, Stream source, CancellationToken cancellationToken) =>
            this.internalClient.UploadToBlobAsync(blobName, source, cancellationToken);

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
        /// Registers a new delegate for the named method. If a delegate is already associated with
        /// the named method, it will be replaced with the new delegate.
        /// <param name="methodName">The name of the method to associate with the delegate.</param>
        /// <param name="methodHandler">The delegate to be used when a method with the given name is called by the cloud service.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>  
        /// </summary>
        public Task SetMethodHandlerAsync(string methodName, MethodCallback methodHandler, object userContext, CancellationToken cancellationToken) =>
            this.internalClient.SetMethodHandlerAsync(methodName, methodHandler, userContext, cancellationToken);


        /// <summary>
        /// Registers a new delegate that is called for a method that doesn't have a delegate registered for its name. 
        /// If a default delegate is already registered it will replace with the new delegate.
        /// </summary>
        /// <param name="methodHandler">The delegate to be used when a method is called by the cloud service and there is no delegate registered for that method name.</param>
        /// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
        public Task SetMethodDefaultHandlerAsync(MethodCallback methodHandler, object userContext) =>
            this.internalClient.SetMethodDefaultHandlerAsync(methodHandler, userContext);

        /// <summary>
        /// Registers a new delegate that is called for a method that doesn't have a delegate registered for its name. 
        /// If a default delegate is already registered it will replace with the new delegate.
        /// </summary>
        /// <param name="methodHandler">The delegate to be used when a method is called by the cloud service and there is no delegate registered for that method name.</param>
        /// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>  
        public Task SetMethodDefaultHandlerAsync(MethodCallback methodHandler, object userContext, CancellationToken cancellationToken) =>
            this.internalClient.SetMethodDefaultHandlerAsync(methodHandler, userContext, cancellationToken);

        /// <summary>
        /// Registers a new delegate for the named method. If a delegate is already associated with
        /// the named method, it will be replaced with the new delegate.
        /// <param name="methodName">The name of the method to associate with the delegate.</param>
        /// <param name="methodHandler">The delegate to be used when a method with the given name is called by the cloud service.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// </summary>

        [Obsolete("Please use SetMethodHandlerAsync.")]
        public void SetMethodHandler(string methodName, MethodCallback methodHandler, object userContext) =>
            this.internalClient.SetMethodHandler(methodName, methodHandler, userContext);

        /// <summary>
        /// Registers a new delegate for the connection status changed callback. If a delegate is already associated, 
        /// it will be replaced with the new delegate.
        /// <param name="statusChangesHandler">The name of the method to associate with the delegate.</param>
        /// </summary>
        public void SetConnectionStatusChangesHandler(ConnectionStatusChangesHandler statusChangesHandler) =>
            this.internalClient.SetConnectionStatusChangesHandler(statusChangesHandler);

        /// <summary>
        /// Releases the unmanaged resources used by the DeviceClient and optionally disposes of the managed resources.
        /// </summary>
        public void Dispose() => this.internalClient?.Dispose();

        /// <summary>
        /// Set a callback that will be called whenever the client receives a state update 
        /// (desired or reported) from the service.  This has the side-effect of subscribing
        /// to the PATCH topic on the service.
        /// </summary>
        /// <param name="callback">Callback to call after the state update has been received and applied</param>
        /// <param name="userContext">Context object that will be passed into callback</param>
        [Obsolete("Please use SetDesiredPropertyUpdateCallbackAsync.")]
        public Task SetDesiredPropertyUpdateCallback(DesiredPropertyUpdateCallback callback, object userContext) =>
            this.internalClient.SetDesiredPropertyUpdateCallback(callback, userContext);

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
        /// Set a callback that will be called whenever the client receives a state update 
        /// (desired or reported) from the service.  This has the side-effect of subscribing
        /// to the PATCH topic on the service.
        /// </summary>
        /// <param name="callback">Callback to call after the state update has been received and applied</param>
        /// <param name="userContext">Context object that will be passed into callback</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>  
        public Task SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback callback, object userContext, CancellationToken cancellationToken) =>
            this.internalClient.SetDesiredPropertyUpdateCallbackAsync(callback, userContext, cancellationToken);

        /// <summary>
        /// Retrieve a device twin object for the current device.
        /// </summary>
        /// <returns>The device twin object for the current device</returns>
        public Task<Twin> GetTwinAsync() => this.internalClient.GetTwinAsync();

        /// <summary>
        /// Retrieve a device twin object for the current device.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>  
        /// <returns>The device twin object for the current device</returns>
        public Task<Twin> GetTwinAsync(CancellationToken cancellationToken) => this.internalClient.GetTwinAsync(cancellationToken);

        /// <summary>
        /// Push reported property changes up to the service.
        /// </summary>
        /// <param name="reportedProperties">Reported properties to push</param>
        public Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties) =>
            this.internalClient.UpdateReportedPropertiesAsync(reportedProperties);

        /// <summary>
        /// Push reported property changes up to the service.
        /// </summary>
        /// <param name="reportedProperties">Reported properties to push</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been canceled.</exception>  
        public Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties, CancellationToken cancellationToken) =>
            this.internalClient.UpdateReportedPropertiesAsync(reportedProperties, cancellationToken);

#region Device Streaming
        /// <summary>
        /// Waits for an incoming Cloud-to-Device Stream request.
        /// </summary>
        /// <returns>A stream request when received</returns>
        public Task<DeviceStreamRequest> WaitForDeviceStreamRequestAsync() 
            => this.internalClient.WaitForDeviceStreamRequestAsync();

        /// <summary>
        /// Waits for an incoming Cloud-to-Device Stream request.
        /// </summary>
        /// <param name="cancellationToken">Token used for cancelling this operation.</param>
        /// <returns>A stream request when received</returns>
        public Task<DeviceStreamRequest> WaitForDeviceStreamRequestAsync(CancellationToken cancellationToken)
            => this.internalClient.WaitForDeviceStreamRequestAsync(cancellationToken);

        /// <summary>
        /// Accepts a Device Stream request.
        /// </summary>
        /// <param name="request">The Device Stream request received through </param>
        /// <returns>An awaitable async task</returns>
        public Task AcceptDeviceStreamRequestAsync(DeviceStreamRequest request)
            => this.internalClient.AcceptDeviceStreamRequestAsync(request);

        /// <summary>
        /// Accepts a Device Stream request.
        /// </summary>
        /// <param name="request">The Device Stream request received through </param>
        /// <param name="cancellationToken">Token used for cancelling this operation.</param>
        /// <returns>An awaitable async task</returns>
        public Task AcceptDeviceStreamRequestAsync(DeviceStreamRequest request, CancellationToken cancellationToken)
            => this.internalClient.AcceptDeviceStreamRequestAsync(request, cancellationToken);

        /// <summary>
        /// Rejects a Device Stream request.
        /// </summary>
        /// <param name="request">The Device Stream request received through </param>
        /// <returns>An awaitable async task</returns>
        public Task RejectDeviceStreamRequestAsync(DeviceStreamRequest request)
            => this.internalClient.RejectDeviceStreamRequestAsync(request);

        /// <summary>
        /// Rejects a Device Stream request.
        /// </summary>
        /// <param name="request">The Device Stream request received through </param>
        /// <param name="cancellationToken">Token used for cancelling this operation.</param>
        /// <returns>An awaitable async task</returns>
        public Task RejectDeviceStreamRequestAsync(DeviceStreamRequest request, CancellationToken cancellationToken)
            => this.internalClient.RejectDeviceStreamRequestAsync(request, cancellationToken);
#endregion Device Streaming
    }
}
