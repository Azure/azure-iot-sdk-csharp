// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#define WIP_TWIN_MQTT
#if WINDOWS_UWP
#undef WIP_TWIN_MQTT
#endif

namespace Microsoft.Azure.Devices.Client
{
    using Common;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client.Extensions;
    using Microsoft.Azure.Devices.Client.Transport;
    using Microsoft.Azure.Devices.Shared;
#if !WINDOWS_UWP && !PCL
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
#endif

#if WIP_TWIN_MQTT
    /// <summary>
    /// Delegate for desired property update callbacks.  This will be called
    /// every time we receive a PATCH from the service.
    /// </summary>
    /// <param name="desiredProperties">Properties that were contained in the update that was received from the service</param>
    /// <param name="userContext">Context object passed in when the callback was registered</param>
    public delegate Task DesiredPropertyUpdateCallback(TwinCollection desiredProperties, object userContext);
#endif

    public delegate Task<MethodResponse> MethodCallback(MethodRequest methodRequest, object userContext);

    /*
     * Class Diagram and Chain of Responsibility in Device Client 
                                     +--------------------+
                                     | <<interface>>      |
                                     | IDelegatingHandler |
                                     |  * Open            |
                                     |  * Close           |
                                     |  * SendEvent       |
                                     |  * SendEvents      |
                                     |  * Receive         |
                                     |  * Complete        |
                                     |  * Abandon         |
                                     |  * Reject          |
                                     +-------+------------+
                                             |
                                             |implements
                                             |
                                             |
                                     +-------+-------+
                                     |  <<abstract>> |     
                                     |  Default      |
             +---+inherits----------->  Delegating   <------inherits-----------------+
             |                       |  Handler      |                               |
             |           +--inherits->               <--inherits----+                |
             |           |           +-------^-------+              |                |
             |           |                   |inherits              |                |
             |           |                   |                      |                |
+------------+       +---+---------+      +--+----------+       +---+--------+       +--------------+
|            |       |             |      |             |       |            |       | <<abstract>> |
| GateKeeper |  use  | Retry       | use  |  Error      |  use  | Routing    |  use  | Transport    |
| Delegating +-------> Delegating  +------>  Delegating +-------> Delegating +-------> Delegating   |
| Handler    |       | Handler     |      |  Handler    |       | Handler    |       | Handler      |
|            |       |             |      |             |       |            |       |              |
| overrides: |       | overrides:  |      |  overrides  |       | overrides: |       | overrides:   |
|  Open      |       |  Open       |      |   Open      |       |  Open      |       |  Receive     |
|  Close     |       |  SendEvent  |      |   SendEvent |       |            |       |              |
|            |       |  SendEvents |      |   SendEvents|       +------------+       +--^--^---^----+
+------------+       |  Receive    |      |   Receive   |                               |  |   |
                     |  Reject     |      |   Reject    |                               |  |   |
                     |  Abandon    |      |   Abandon   |                               |  |   |
                     |  Complete   |      |   Complete  |                               |  |   |
                     |             |      |             |                               |  |   |
                     +-------------+      +-------------+     +-------------+-+inherits-+  |   +---inherits-+-------------+
                                                              |             |              |                |             |
                                                              | AMQP        |              inherits         | HTTP        |
                                                              | Transport   |              |                | Transport   |
                                                              | Handler     |          +---+---------+      | Handler     |
                                                              |             |          |             |      |             |
                                                              | overrides:  |          | MQTT        |      | overrides:  |
                                                              |  everything |          | Transport   |      |  everything |
                                                              |             |          | Handler     |      |             |
                                                              +-------------+          |             |      +-------------+
                                                                                       | overrides:  |
                                                                                       |  everything |
                                                                                       |             |
                                                                                       +-------------+
TODO: revisit DefaultDelegatingHandler - it seems redundant as long as we have to many overloads in most of the classes.
*/

    /// <summary>
    /// Contains methods that a device can use to send messages to and receive from the service.
    /// </summary>
    public sealed class DeviceClient
#if !PCL
        : IDisposable
#endif
    {
        const string DeviceId = "DeviceId";
        const string DeviceIdParameterPattern = @"(^\s*?|.*;\s*?)" + DeviceId + @"\s*?=.*";
        IotHubConnectionString iotHubConnectionString = null;
#if !PCL
        const RegexOptions RegexOptions = System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase;
#else
        const RegexOptions RegexOptions = System.Text.RegularExpressions.RegexOptions.IgnoreCase;
#endif
        static readonly Regex DeviceIdParameterRegex = new Regex(DeviceIdParameterPattern, RegexOptions);

        internal IDelegatingHandler InnerHandler { get; set; }

        SemaphoreSlim methodsDictionarySemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Stores the timeout used in the operation retries.
        /// </summary>
        // Codes_SRS_DEVICECLIENT_28_002: [This property shall be defaulted to 240000 (4 minutes).]
        public uint OperationTimeoutInMilliseconds { get; set; } = 4 * 60 * 1000;

        /// <summary>
        /// Stores the retry strategy used in the operation retries.
        /// </summary>
        public RetryPolicyType RetryPolicy { get; set; }

        /// <summary>
        /// Stores Methods supported by the client device and their associated delegate.
        /// </summary>
        volatile Dictionary<string, Tuple<MethodCallback, object>> deviceMethods;

        internal delegate Task OnMethodCalledDelegate(MethodRequestInternal methodRequestInternal);

#if WIP_TWIN_MQTT
        /// <summary>
        /// Callback to call whenever the twin's desired state is updated by the service
        /// </summary>
        internal DesiredPropertyUpdateCallback desiredPropertyUpdateCallback;

        /// <summary>
        /// Has twin funcitonality been enabled with the service?
        /// </summary>
        Boolean patchSubscribedWithService = false;

        /// <summary>
        /// userContext passed when registering the twin patch callback
        /// </summary>
        Object twinPatchCallbackContext = null;
#endif

        DeviceClient(IotHubConnectionString iotHubConnectionString, ITransportSettings[] transportSettings, IDeviceClientPipelineBuilder pipelineBuilder)
        {
            this.iotHubConnectionString = iotHubConnectionString;

            var pipelineContext = new PipelineContext();
            pipelineContext.Set(transportSettings);
            pipelineContext.Set(iotHubConnectionString);
            pipelineContext.Set<OnMethodCalledDelegate>(OnMethodCalled);

            IDelegatingHandler innerHandler = pipelineBuilder.Build(pipelineContext);

            this.InnerHandler = innerHandler;
        }

        DeviceClient(IotHubConnectionString iotHubConnectionString)
        {
            this.iotHubConnectionString = iotHubConnectionString;

            var pipelineContext = new PipelineContext();
            pipelineContext.Set(iotHubConnectionString);
            pipelineContext.Set<ITransportSettings>(new Http1TransportSettings());

            IDeviceClientPipelineBuilder pipelineBuilder = new DeviceClientPipelineBuilder()
                .With(ctx => new GateKeeperDelegatingHandler(ctx))
                .With(ctx => new ErrorDelegatingHandler(ctx))
                .With(ctx => new HttpTransportHandler(ctx, ctx.Get<IotHubConnectionString>(), ctx.Get<ITransportSettings>() as Http1TransportSettings));

            this.InnerHandler = pipelineBuilder.Build(pipelineContext);
        }

        static IDeviceClientPipelineBuilder BuildPipeline()
        {
            var transporthandlerFactory = new TransportHandlerFactory();
            IDeviceClientPipelineBuilder pipelineBuilder = new DeviceClientPipelineBuilder()
                .With(ctx => new GateKeeperDelegatingHandler(ctx))
#if !WINDOWS_UWP && !PCL
                .With(ctx => new RetryDelegatingHandler(ctx))
#endif
                .With(ctx => new ErrorDelegatingHandler(ctx))
                .With(ctx => new ProtocolRoutingDelegatingHandler(ctx))
                .With(ctx => transporthandlerFactory.Create(ctx));
            return pipelineBuilder;
        }

        internal Task SendMethodResponseAsync(MethodResponseInternal methodResponse)
        {
            return ApplyTimeout(operationTimeoutCancellationToken =>
            {
                return this.InnerHandler.SendMethodResponseAsync(methodResponse, operationTimeoutCancellationToken);
            });
        }

        /// <summary>
        /// Create an Amqp DeviceClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <returns>DeviceClient</returns>
        public static DeviceClient Create(string hostname, IAuthenticationMethod authenticationMethod)
        {
#if WINDOWS_UWP || PCL
            return Create(hostname, authenticationMethod, TransportType.Http1);
#else
            return Create(hostname, authenticationMethod, TransportType.Amqp);
#endif
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
            if (hostname == null)
            {
                throw new ArgumentNullException(nameof(hostname));
            }

            if (authenticationMethod == null)
            {
                throw new ArgumentNullException(nameof(authenticationMethod));
            }

            IotHubConnectionStringBuilder connectionStringBuilder = IotHubConnectionStringBuilder.Create(hostname, authenticationMethod);

#if !WINDOWS_UWP && !PCL && !NETMF
            if (authenticationMethod is DeviceAuthenticationWithX509Certificate)
            {
                if (connectionStringBuilder.Certificate == null)
                {
                    throw new ArgumentException("certificate must be present in DeviceAuthenticationWithX509Certificate");
                }

                return CreateFromConnectionString(connectionStringBuilder.ToString(), PopulateCertificateInTransportSettings(connectionStringBuilder, transportType));
            }
#endif
            return CreateFromConnectionString(connectionStringBuilder.ToString(), transportType);
        }

#if !PCL
        /// <summary>
        /// Create a DeviceClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="transportSettings">Prioritized list of transportTypes and their settings</param>
        /// <returns>DeviceClient</returns>
        public static DeviceClient Create(string hostname, IAuthenticationMethod authenticationMethod, [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArrayAttribute] ITransportSettings[] transportSettings)
        {
            if (hostname == null)
            {
                throw new ArgumentNullException("hostname");
            }

            if (authenticationMethod == null)
            {
                throw new ArgumentNullException("authenticationMethod");
            }

            var connectionStringBuilder = IotHubConnectionStringBuilder.Create(hostname, authenticationMethod);
#if !WINDOWS_UWP && !PCL && !NETMF
            if (authenticationMethod is DeviceAuthenticationWithX509Certificate)
            {
                if (connectionStringBuilder.Certificate == null)
                {
                    throw new ArgumentException("certificate must be present in DeviceAuthenticationWithX509Certificate");
                }

                return CreateFromConnectionString(connectionStringBuilder.ToString(), PopulateCertificateInTransportSettings(connectionStringBuilder, transportSettings));
            }
#endif
            return CreateFromConnectionString(connectionStringBuilder.ToString(), transportSettings);
        }
#endif

        /// <summary>
        /// Create a DeviceClient using Amqp transport from the specified connection string
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (including DeviceId)</param>
        /// <returns>DeviceClient</returns>
        public static DeviceClient CreateFromConnectionString(string connectionString)
        {
#if WINDOWS_UWP || PCL
            return CreateFromConnectionString(connectionString, TransportType.Http1);
#else
            return CreateFromConnectionString(connectionString, TransportType.Amqp);
#endif
        }

        /// <summary>
        /// Create a DeviceClient using Amqp transport from the specified connection string
        /// (WinRT) Create a DeviceClient using Http transport from the specified connection string
        /// </summary>
        /// <param name="connectionString">IoT Hub-Scope Connection string for the IoT hub (without DeviceId)</param>
        /// <param name="deviceId">Id of the Device</param>
        /// <returns>DeviceClient</returns>
        public static DeviceClient CreateFromConnectionString(string connectionString, string deviceId)
        {
#if WINDOWS_UWP || PCL
            return CreateFromConnectionString(connectionString, deviceId, TransportType.Http1);    
#else
            return CreateFromConnectionString(connectionString, deviceId, TransportType.Amqp);
#endif
        }

        /// <summary>
        /// Create DeviceClient from the specified connection string using the specified transport type
        /// (PCL) Only Http transport is allowed
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (including DeviceId)</param>
        /// <param name="transportType">Specifies whether Amqp or Http transport is used</param>
        /// <returns>DeviceClient</returns>
        public static DeviceClient CreateFromConnectionString(string connectionString, TransportType transportType)
        {
            return CreateFromConnectionString(connectionString, transportType, null);
        }

        /// <summary>
        /// Create DeviceClient from the specified connection string using the specified transport type
        /// (PCL) Only Http transport is allowed
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (including DeviceId)</param>
        /// <param name="transportType">Specifies whether Amqp or Http transport is used</param>
        /// <param name="pipelineBuilder">Device client pipeline builder</param>
        /// <returns>DeviceClient</returns>
        internal static DeviceClient CreateFromConnectionString(string connectionString, TransportType transportType, IDeviceClientPipelineBuilder pipelineBuilder)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            switch (transportType)
            {
                case TransportType.Amqp:
                    return CreateFromConnectionString(connectionString, new ITransportSettings[]
                    {
                        new AmqpTransportSettings(TransportType.Amqp_Tcp_Only),
                        new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only)
                    },
                    pipelineBuilder);
                case TransportType.Mqtt:
#if WINDOWS_UWP || PCL
                    throw new NotImplementedException("Mqtt protocol is not supported");
#else
                    return CreateFromConnectionString(connectionString, new ITransportSettings[]
                    {
                        new MqttTransportSettings(TransportType.Mqtt_Tcp_Only),
                        new MqttTransportSettings(TransportType.Mqtt_WebSocket_Only)
                    }, pipelineBuilder);
#endif
                case TransportType.Amqp_WebSocket_Only:
                case TransportType.Amqp_Tcp_Only:
#if PCL
                    throw new NotImplementedException("Amqp protocol is not supported");
#else
                    return CreateFromConnectionString(connectionString, new ITransportSettings[] { new AmqpTransportSettings(transportType) }, pipelineBuilder);
#endif
                case TransportType.Mqtt_WebSocket_Only:
                case TransportType.Mqtt_Tcp_Only:
#if WINDOWS_UWP || PCL
                    throw new NotImplementedException("Mqtt protocol is not supported");
#else
                    return CreateFromConnectionString(connectionString, new ITransportSettings[] { new MqttTransportSettings(transportType) }, pipelineBuilder);
#endif
                case TransportType.Http1:
                    return CreateFromConnectionString(connectionString, new ITransportSettings[] { new Http1TransportSettings() }, pipelineBuilder);
                default:
#if !PCL
                    throw new InvalidOperationException("Unsupported Transport Type {0}".FormatInvariant(transportType));
#else
                    throw new InvalidOperationException($"Unsupported Transport Type {transportType}");
#endif
            }
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
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            if (deviceId == null)
            {
                throw new ArgumentNullException(nameof(deviceId));
            }

            if (DeviceIdParameterRegex.IsMatch(connectionString))
            {
                throw new ArgumentException("Connection string must not contain DeviceId keyvalue parameter", nameof(connectionString));
            }

            return CreateFromConnectionString(connectionString + ";" + DeviceId + "=" + deviceId, transportType);
        }

        /// <summary>
        /// Create DeviceClient from the specified connection string using a prioritized list of transports
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (with DeviceId)</param>
        /// <param name="transportSettings">Prioritized list of transports and their settings</param>
        /// <returns>DeviceClient</returns>
        public static DeviceClient CreateFromConnectionString(string connectionString, [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] ITransportSettings[] transportSettings)
        {
            return CreateFromConnectionString(connectionString, transportSettings, null);
        }

        /// <summary>
        /// Create DeviceClient from the specified connection string using a prioritized list of transports
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (with DeviceId)</param>
        /// <param name="transportSettings">Prioritized list of transports and their settings</param>
        /// <param name="pipelineBuilder">Device client pipeline builder</param>
        /// <returns>DeviceClient</returns>
        internal static DeviceClient CreateFromConnectionString(string connectionString, [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] ITransportSettings[] transportSettings, IDeviceClientPipelineBuilder pipelineBuilder)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            if (transportSettings == null)
            {
                throw new ArgumentNullException(nameof(transportSettings));
            }

            if (transportSettings.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(connectionString), "Must specify at least one TransportSettings instance");
            }

            IotHubConnectionString iotHubConnectionString = IotHubConnectionString.Parse(connectionString);

            foreach (ITransportSettings transportSetting in transportSettings)
            {
                switch (transportSetting.GetTransportType())
                {
                    case TransportType.Amqp_WebSocket_Only:
                    case TransportType.Amqp_Tcp_Only:
                        if (!(transportSetting is AmqpTransportSettings))
                        {
                            throw new InvalidOperationException("Unknown implementation of ITransportSettings type");
                        }
                        break;
                    case TransportType.Http1:
                        if (!(transportSetting is Http1TransportSettings))
                        {
                            throw new InvalidOperationException("Unknown implementation of ITransportSettings type");
                        }
                        break;
#if !WINDOWS_UWP && !PCL
                    case TransportType.Mqtt_WebSocket_Only:
                    case TransportType.Mqtt_Tcp_Only:
                        if (!(transportSetting is MqttTransportSettings))
                        {
                            throw new InvalidOperationException("Unknown implementation of ITransportSettings type");
                        }
                        break;
#endif
                    default:
                        throw new InvalidOperationException("Unsupported Transport Type {0}".FormatInvariant(transportSetting.GetTransportType()));
                }
            }

            pipelineBuilder = pipelineBuilder ?? BuildPipeline();

            // Defer concrete DeviceClient creation to OpenAsync
            return new DeviceClient(iotHubConnectionString, transportSettings, pipelineBuilder);
        }

        /// <summary>
        /// Create DeviceClient from the specified connection string using the prioritized list of transports
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (without DeviceId)</param>
        /// <param name="deviceId">Id of the device</param>
        /// <param name="transportSettings">Prioritized list of transportTypes and their settings</param>
        /// <returns>DeviceClient</returns>
        public static DeviceClient CreateFromConnectionString(string connectionString, string deviceId, [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArrayAttribute] ITransportSettings[] transportSettings)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            if (deviceId == null)
            {
                throw new ArgumentNullException(nameof(deviceId));
            }

            if (DeviceIdParameterRegex.IsMatch(connectionString))
            {
                throw new ArgumentException("Connection string must not contain DeviceId keyvalue parameter", nameof(connectionString));
            }

            return CreateFromConnectionString(connectionString + ";" + DeviceId + "=" + deviceId, transportSettings);
        }

        CancellationTokenSource GetOperationTimeoutCancellationTokenSource()
        {
            return new CancellationTokenSource(TimeSpan.FromMilliseconds(OperationTimeoutInMilliseconds));
        }

        /// <summary>
        /// Explicitly open the DeviceClient instance.
        /// </summary>

        public Task OpenAsync()
        {
            // Codes_SRS_DEVICECLIENT_28_007: [ The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable(authentication, quota exceed) error occurs.]
            return ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.OpenAsync(true, operationTimeoutCancellationToken));
        }

        /// <summary>
        /// Close the DeviceClient instance
        /// </summary>
        /// <returns></returns>
        public Task CloseAsync()
        {
            return this.InnerHandler.CloseAsync();
        }

        /// <summary>
        /// Receive a message from the device queue using the default timeout.
        /// </summary>
        /// <returns>The receive message or null if there was no message until the default timeout</returns>
        public Task<Message> ReceiveAsync()
        {
            // Codes_SRS_DEVICECLIENT_28_011: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable(authentication, quota exceed) error occurs.]
            return ApplyTimeoutMessage(operationTimeoutCancellationToken => this.InnerHandler.ReceiveAsync(operationTimeoutCancellationToken));
        }

        /// <summary>
        /// Receive a message from the device queue with the specified timeout
        /// </summary>
        /// <returns>The receive message or null if there was no message until the specified time has elapsed</returns>
        public Task<Message> ReceiveAsync(TimeSpan timeout)
        {
            // Codes_SRS_DEVICECLIENT_28_011: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable(authentication, quota exceed) error occurs.]
            return ApplyTimeoutMessage(operationTimeoutCancellationToken => this.InnerHandler.ReceiveAsync(timeout, operationTimeoutCancellationToken));
        }

        /// <summary>
        /// Deletes a received message from the device queue
        /// </summary>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task CompleteAsync(string lockToken)
        {
            if (string.IsNullOrEmpty(lockToken))
            {
                throw Fx.Exception.ArgumentNull("lockToken");
            }

            // Codes_SRS_DEVICECLIENT_28_013: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
            return ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.CompleteAsync(lockToken, operationTimeoutCancellationToken));
        }

        /// <summary>
        /// Deletes a received message from the device queue
        /// </summary>
        /// <returns>The previously received message</returns>
        public Task CompleteAsync(Message message)
        {
            if (message == null)
            {
                throw Fx.Exception.ArgumentNull("message");
            }
            // Codes_SRS_DEVICECLIENT_28_015: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
            return this.CompleteAsync(message.LockToken);
        }

        /// <summary>
        /// Puts a received message back onto the device queue
        /// </summary>
        /// <returns>The previously received message</returns>
        public Task AbandonAsync(string lockToken)
        {
            if (string.IsNullOrEmpty(lockToken))
            {
                throw Fx.Exception.ArgumentNull("lockToken");
            }
            // Codes_SRS_DEVICECLIENT_28_015: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
            return ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.AbandonAsync(lockToken, operationTimeoutCancellationToken));
        }

        /// <summary>
        /// Puts a received message back onto the device queue
        /// </summary>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task AbandonAsync(Message message)
        {
            if (message == null)
            {
                throw Fx.Exception.ArgumentNull("message");
            }

            return this.AbandonAsync(message.LockToken);
        }

        /// <summary>
        /// Deletes a received message from the device queue and indicates to the server that the message could not be processed.
        /// </summary>
        /// <returns>The previously received message</returns>
        public Task RejectAsync(string lockToken)
        {
            if (string.IsNullOrEmpty(lockToken))
            {
                throw Fx.Exception.ArgumentNull("lockToken");
            }

            return ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.RejectAsync(lockToken, operationTimeoutCancellationToken));
        }

        /// <summary>
        /// Deletes a received message from the device queue and indicates to the server that the message could not be processed.
        /// </summary>
        /// <returns>The lock identifier for the previously received message</returns>
        public Task RejectAsync(Message message)
        {
            if (message == null)
            {
                throw Fx.Exception.ArgumentNull("message");
            }
            // Codes_SRS_DEVICECLIENT_28_017: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication, quota exceed) occurs.]
            return ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.RejectAsync(message.LockToken, operationTimeoutCancellationToken));
        }

        /// <summary>
        /// Sends an event to device hub
        /// </summary>
        /// <returns>The message containing the event</returns>
        public Task SendEventAsync(Message message)
        {
            if (message == null)
            {
                throw Fx.Exception.ArgumentNull("message");
            }
            // Codes_SRS_DEVICECLIENT_28_019: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication or quota exceed) occurs.]
            return ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.SendEventAsync(message, operationTimeoutCancellationToken));
        }

        /// <summary>
        /// Sends a batch of events to device hub
        /// </summary>
        /// <returns>The task containing the event</returns>
        public Task SendEventBatchAsync(IEnumerable<Message> messages)
        {
            if (messages == null)
            {
                throw Fx.Exception.ArgumentNull("messages");
            }
            // Codes_SRS_DEVICECLIENT_28_019: [The async operation shall retry until time specified in OperationTimeoutInMilliseconds property expire or unrecoverable error(authentication or quota exceed) occurs.]
            return ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.SendEventAsync(messages, operationTimeoutCancellationToken));
        }

        Task ApplyTimeout(Func<CancellationToken, Task> operation)
        {
            if (OperationTimeoutInMilliseconds == 0)
            {
                return operation(CancellationToken.None)
                    .WithTimeout(TimeSpan.MaxValue, () => Resources.OperationTimeoutExpired, CancellationToken.None);
            }

            CancellationTokenSource operationTimeoutCancellationTokenSource = GetOperationTimeoutCancellationTokenSource();

            var result = operation(operationTimeoutCancellationTokenSource.Token)
                .WithTimeout(TimeSpan.FromMilliseconds(OperationTimeoutInMilliseconds), () => Resources.OperationTimeoutExpired, operationTimeoutCancellationTokenSource.Token);
            result.ContinueWith(t =>
            {
                operationTimeoutCancellationTokenSource.Dispose();
            });
            return result;
        }

        Task<Message> ApplyTimeoutMessage(Func<CancellationToken, Task<Message>> operation)
        {
            if (OperationTimeoutInMilliseconds == 0)
            {
                return operation(CancellationToken.None)
                    .WithTimeout(TimeSpan.MaxValue, () => Resources.OperationTimeoutExpired, CancellationToken.None);
            }

            CancellationTokenSource operationTimeoutCancellationTokenSource = GetOperationTimeoutCancellationTokenSource();

            var result = operation(operationTimeoutCancellationTokenSource.Token)
                .WithTimeout(TimeSpan.FromMilliseconds(OperationTimeoutInMilliseconds), () => Resources.OperationTimeoutExpired, operationTimeoutCancellationTokenSource.Token);
            result.ContinueWith(t =>
            {
                operationTimeoutCancellationTokenSource.Dispose();
                return t.Result;
            });
            return result;
        }

#if WIP_TWIN_MQTT
        Task<Twin> ApplyTimeoutTwin(Func<CancellationToken, Task<Twin>> operation)
        {
            if (OperationTimeoutInMilliseconds == 0)
            {
                return operation(CancellationToken.None)
                    .WithTimeout(TimeSpan.MaxValue, () => Resources.OperationTimeoutExpired, CancellationToken.None);
            }

            CancellationTokenSource operationTimeoutCancellationTokenSource = GetOperationTimeoutCancellationTokenSource();

            var result = operation(operationTimeoutCancellationTokenSource.Token)
                .WithTimeout(TimeSpan.FromMilliseconds(OperationTimeoutInMilliseconds), () => Resources.OperationTimeoutExpired, operationTimeoutCancellationTokenSource.Token);
            result.ContinueWith(t =>
            {
                operationTimeoutCancellationTokenSource.Dispose();
                return t.Result;
            });
            return result;
        }
#endif

#if !WINDOWS_UWP && !PCL // ArturL: we should be able to support UploadToBlobAsync for UWP now
        /// <summary>
        /// Uploads a stream to a block blob in a storage account associated with the IoTHub for that device.
        /// If the blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="blobName"></param>
        /// <param name="source"></param>
        /// <returns>AsncTask</returns>
        public Task UploadToBlobAsync(String blobName, System.IO.Stream source)
        {
            if (String.IsNullOrEmpty(blobName))
            {
                throw Fx.Exception.ArgumentNull("blobName");
            }
            if (source == null)
            {
                throw Fx.Exception.ArgumentNull("source");
            }
            if (blobName.Length > 1024)
            {
                throw Fx.Exception.Argument("blobName", "Length cannot exceed 1024 characters");
            }
            if (blobName.Split('/').Count() > 254)
            {
                throw Fx.Exception.Argument("blobName", "Path segment count cannot exceed 254");
            }

            var httpTransport = new HttpTransportHandler(iotHubConnectionString);
            return httpTransport.UploadToBlobAsync(blobName, source);
        }
#endif

        /// <summary>
        /// Registers a new delgate for the named method. If a delegate is already associated with
        /// the named method, it will be replaced with the new delegate.
        /// <param name="methodName">The name of the method to associate with the delegate.</param>
        /// <param name="methodHandler">The delegate to be used when a method with the given name is called by the cloud service.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// </summary>
        public async Task SetMethodHandlerAsync(string methodName, MethodCallback methodHandler, object userContext)
        {
            try
            {
                await methodsDictionarySemaphore.WaitAsync();

                if (methodHandler != null)
                {
                    // codes_SRS_DEVICECLIENT_10_001: [ The SetMethodHandler shall lazy-initialize the deviceMethods property. ]
                    if (this.deviceMethods == null)
                    {
                        this.deviceMethods = new Dictionary<string, Tuple<MethodCallback, object>>();

                        // codes_SRS_DEVICECLIENT_10_005: [ The SetMethodHandler shall EnableMethodsAsync when called for the first time. ]
                        await ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.EnableMethodsAsync(operationTimeoutCancellationToken));
                    }
                    this.deviceMethods[methodName] = new Tuple<MethodCallback, object>(methodHandler, userContext);
                }
                else
                {
                    // codes_SRS_DEVICECLIENT_10_002: [ If the given methodName already has an associated delegate, the existing delegate shall be removed. ]
                    // codes_SRS_DEVICECLIENT_10_003: [ The given delegate will only be added if it is not null. ]
                    if (this.deviceMethods != null)
                    {
                        this.deviceMethods.Remove(methodName);

                        if (this.deviceMethods.Count == 0)
                        {
                            // codes_SRS_DEVICECLIENT_10_006: [ The SetMethodHandler shall DisableMethodsAsync when the last delegate has been removed. ]
                            await ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.DisableMethodsAsync(operationTimeoutCancellationToken));

                            // codes_SRS_DEVICECLIENT_10_004: [ The deviceMethods property shall be deleted if the last delegate has been removed. ]
                            this.deviceMethods = null;
                        }
                    }
                }
            }
            finally
            {
                methodsDictionarySemaphore.Release();
            }
        }

        /// <summary>
        /// Registers a new delgate for the named method. If a delegate is already associated with
        /// the named method, it will be replaced with the new delegate.
        /// <param name="methodName">The name of the method to associate with the delegate.</param>
        /// <param name="methodHandler">The delegate to be used when a method with the given name is called by the cloud service.</param>
        /// <param name="userContext">generic parameter to be interpreted by the client code.</param>
        /// </summary>

        [Obsolete("Please use SetMethodHandlerAsync.")]
        public void SetMethodHandler(string methodName, MethodCallback methodHandler, object userContext)
        {
            methodsDictionarySemaphore.Wait();

            try
            {
                if (methodHandler != null)
                {
                    // codes_SRS_DEVICECLIENT_10_001: [ The SetMethodHandler shall lazy-initialize the deviceMethods property. ]
                    if (this.deviceMethods == null)
                    {
                        this.deviceMethods = new Dictionary<string, Tuple<MethodCallback, object>>();

                        // codes_SRS_DEVICECLIENT_10_005: [ The SetMethodHandler shall EnableMethodsAsync when called for the first time. ]
                        ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.EnableMethodsAsync(operationTimeoutCancellationToken)).Wait();
                    }
                    this.deviceMethods[methodName] = new Tuple<MethodCallback, object>(methodHandler, userContext);
                }
                else
                {
                    // codes_SRS_DEVICECLIENT_10_002: [ If the given methodName already has an associated delegate, the existing delegate shall be removed. ]
                    // codes_SRS_DEVICECLIENT_10_003: [ The given delegate will only be added if it is not null. ]
                    if (this.deviceMethods != null)
                    {
                        this.deviceMethods.Remove(methodName);

                        if (this.deviceMethods.Count == 0)
                        {
                            // codes_SRS_DEVICECLIENT_10_006: [ The SetMethodHandler shall DisableMethodsAsync when the last delegate has been removed. ]
                            ApplyTimeout(operationTimeoutCancellationToken => this.InnerHandler.DisableMethodsAsync(operationTimeoutCancellationToken)).Wait();

                            // codes_SRS_DEVICECLIENT_10_004: [ The deviceMethods property shall be deleted if the last delegate has been removed. ]
                            this.deviceMethods = null;
                        }
                    }
                }
            }
            finally
            {
                methodsDictionarySemaphore.Release();
            }
        }

        internal async Task OnMethodCalled(MethodRequestInternal methodRequestInternal)
        {
            Tuple<MethodCallback, object> m = null;

            // codes_SRS_DEVICECLIENT_10_012: [ If the given methodRequestInternal argument is null, fail silently ]
            if (methodRequestInternal != null)
            {
                byte[] requestData = methodRequestInternal.GetBytes();

                methodsDictionarySemaphore.Wait();
                try
                {
                    Utils.ValidateDataIsEmptyOrJson(requestData);                    
                    // codes_SRS_DEVICECLIENT_10_013: [ If the given method does not have an associated delegate, fail silently ]
                    this.deviceMethods?.TryGetValue(methodRequestInternal.Name, out m);
                }
                catch (Exception)
                {
                    // codes_SRS_DEVICECLIENT_28_020: [ If the given methodRequestInternal data is not valid json, fail silently ]
                }
                finally
                {
                    methodsDictionarySemaphore.Release();
                }

                if (m != null)
                {
                    try
                    {
                        // codes_SRS_DEVICECLIENT_10_011: [ The OnMethodCalled shall invoke the specified delegate. ]
                        MethodResponse rv = await m.Item1(new MethodRequest(methodRequestInternal.Name, requestData), m.Item2);

                        MethodResponseInternal methodResponseInternal;

                        // codes_SRS_DEVICECLIENT_03_012: [If the MethodResponse does not contain result, the MethodResponseInternal constructor shall be invoked with no results.]
                        if (rv.Result == null)
                        {
                            methodResponseInternal = new MethodResponseInternal(methodRequestInternal.RequestId, rv.Status);
                        }
                        // codes_SRS_DEVICECLIENT_03_013: [Otherwise, the MethodResponseInternal constructor shall be invoked with the result supplied.]
                        else
                        {
                            methodResponseInternal = new MethodResponseInternal(rv.Result, methodRequestInternal.RequestId, rv.Status);

                        }

                        await this.SendMethodResponseAsync(methodResponseInternal);
                    }
                    catch (Exception)
                    {
                        // codes_SRS_DEVICECLIENT_28_021: [ If the MethodResponse from the MethodHandler is not valid json, fail silently ]
                    }
                }
            }
        }

        public void Dispose()
        {
            this.InnerHandler?.Dispose();
        }

#if !WINDOWS_UWP && !PCL
        static ITransportSettings[] PopulateCertificateInTransportSettings(IotHubConnectionStringBuilder connectionStringBuilder, TransportType transportType)
        {
            switch (transportType)
            {
                case TransportType.Amqp:
                    return new ITransportSettings[]
                    {
                        new AmqpTransportSettings(TransportType.Amqp_Tcp_Only)
                        {
                            ClientCertificate = connectionStringBuilder.Certificate
                        },
                        new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only)
                        {
                            ClientCertificate = connectionStringBuilder.Certificate
                        }
                    };
                case TransportType.Amqp_Tcp_Only:
                    return new ITransportSettings[]
                    {
                        new AmqpTransportSettings(TransportType.Amqp_Tcp_Only)
                        {
                            ClientCertificate = connectionStringBuilder.Certificate
                        }
                    };
                case TransportType.Amqp_WebSocket_Only:
                    return new ITransportSettings[]
                    {
                        new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only)
                        {
                            ClientCertificate = connectionStringBuilder.Certificate
                        }
                    };
                case TransportType.Http1:
                    return new ITransportSettings[]
                    {
                        new Http1TransportSettings()
                        {
                            ClientCertificate = connectionStringBuilder.Certificate
                        }
                    };
                case TransportType.Mqtt:
                    return new ITransportSettings[]
                    {
                        new MqttTransportSettings(TransportType.Mqtt_Tcp_Only)
                        {
                            ClientCertificate = connectionStringBuilder.Certificate
                        },
                        new MqttTransportSettings(TransportType.Mqtt_WebSocket_Only)
                        {
                            ClientCertificate = connectionStringBuilder.Certificate
                        }
                    };
                case TransportType.Mqtt_Tcp_Only:
                    return new ITransportSettings[]
                    {
                        new MqttTransportSettings(TransportType.Mqtt_Tcp_Only)
                        {
                            ClientCertificate = connectionStringBuilder.Certificate
                        }
                    };
                case TransportType.Mqtt_WebSocket_Only:
                    return new ITransportSettings[]
                    {
                        new MqttTransportSettings(TransportType.Mqtt_WebSocket_Only)
                        {
                            ClientCertificate = connectionStringBuilder.Certificate
                        }
                    };
                default:
                    throw new InvalidOperationException("Unsupported Transport {0}".FormatInvariant(transportType));
            }
        }

        static ITransportSettings[] PopulateCertificateInTransportSettings(IotHubConnectionStringBuilder connectionStringBuilder, ITransportSettings[] transportSettings)
        {
            foreach (var transportSetting in  transportSettings)
            {
                switch (transportSetting.GetTransportType())
                {
                    case TransportType.Amqp_WebSocket_Only:
                    case TransportType.Amqp_Tcp_Only:
                        ((AmqpTransportSettings)transportSetting).ClientCertificate = connectionStringBuilder.Certificate;
                        break;
                    case TransportType.Http1:
                        ((Http1TransportSettings)transportSetting).ClientCertificate = connectionStringBuilder.Certificate;
                        break;
                    case TransportType.Mqtt_WebSocket_Only:
                    case TransportType.Mqtt_Tcp_Only:
                        ((MqttTransportSettings)transportSetting).ClientCertificate = connectionStringBuilder.Certificate;
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported Transport {0}".FormatInvariant(transportSetting.GetTransportType()));
                }
            }

            return transportSettings;
        }
#endif

#if WIP_TWIN_MQTT
        /// <summary>
        /// Set a callback that will be called whenever the client receives a state update 
        /// (desired or reported) from the service.  This has the side-effect of subscribing
        /// to the PATCH topic on the service.
        /// </summary>
        /// <param name="callback">Callback to call after the state update has been received and applied</param>
        /// <param name="userContext">Context object that will be passed into callback</param>
        public Task SetDesiredPropertyUpdateCallback(DesiredPropertyUpdateCallback callback, object userContext)
        {
            // Codes_SRS_DEVICECLIENT_18_007: `SetDesiredPropertyUpdateCallback` shall throw an `ArgumentNull` exception if `callback` is null
            if (callback == null)
            {
                throw Fx.Exception.ArgumentNull("callback");
            }

            return ApplyTimeout(async operationTimeoutCancellationToken =>
            {
                // Codes_SRS_DEVICECLIENT_18_003: `SetDesiredPropertyUpdateCallback` shall call the transport to register for PATCHes on it's first call.
                // Codes_SRS_DEVICECLIENT_18_004: `SetDesiredPropertyUpdateCallback` shall not call the transport to register for PATCHes on subsequent calls
                if (!this.patchSubscribedWithService)
                {
                    this.InnerHandler.TwinUpdateHandler = this.OnReportedStatePatchReceived;
                    await this.InnerHandler.EnableTwinPatchAsync(operationTimeoutCancellationToken);
                    patchSubscribedWithService = true;
                }

                this.desiredPropertyUpdateCallback = callback;
                this.twinPatchCallbackContext = userContext;
            });
        }

        /// <summary>
        /// Retrieve a device twin object for the current device.
        /// </summary>
        /// <returns>The device twin object for the current device</returns>
        public Task<Twin> GetTwinAsync()
        {
            return ApplyTimeoutTwin(async operationTimeoutCancellationToken =>
            {
                // Codes_SRS_DEVICECLIENT_18_001: `GetTwinAsync` shall call `SendTwinGetAsync` on the transport to get the twin state
                return await this.InnerHandler.SendTwinGetAsync(operationTimeoutCancellationToken);
            });
        }

        /// <summary>
        /// Push reported property changes up to the service.
        /// </summary>
        /// <param name="reportedProperties">Reported properties to push</param>
        public Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties)
        {
            // Codes_SRS_DEVICECLIENT_18_006: `UpdateReportedPropertiesAsync` shall throw an `ArgumentNull` exception if `reportedProperties` is null
            if (reportedProperties == null)
            {
                throw Fx.Exception.ArgumentNull("reportedProperties");
            }
            return ApplyTimeout(async operationTimeoutCancellationToken =>
            {
                // Codes_SRS_DEVICECLIENT_18_002: `UpdateReportedPropertiesAsync` shall call `SendTwinPatchAsync` on the transport to update the reported properties
                 await this.InnerHandler.SendTwinPatchAsync(reportedProperties, operationTimeoutCancellationToken);
            });
        }

    //  Codes_SRS_DEVICECLIENT_18_005: When a patch is received from the service, the `callback` shall be called.
    void OnReportedStatePatchReceived(TwinCollection patch)
        {
            if (this.desiredPropertyUpdateCallback != null)
            {
                this.desiredPropertyUpdateCallback(patch, this.twinPatchCallbackContext);
            }
        }
#endif // WIP_TWIN_MQTT

    }
}


