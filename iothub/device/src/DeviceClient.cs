// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Linq;
#if NETSTANDARD1_3
    using System.Net.Http;
#endif
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client.Transport;
    using Microsoft.Azure.Devices.Shared;

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
    public sealed class DeviceClient : BaseClient
    {
        private ProductInfo productInfo = new ProductInfo();

        /// <summary>
        /// Stores custom product information that will be appended to the user agent string that is sent to IoT Hub.
        /// </summary>
        public string ProductInfo
        {
            // We store DeviceClient.ProductInfo as a string property of an object (rather than directly as a string)
            // so that updates will propagate down to the transport layer throughout the lifetime of the DeviceClient
            // object instance.
            get => productInfo.Extra;
            set => productInfo.Extra = value;
        }

        DeviceClient(IotHubConnectionString iotHubConnectionString, ITransportSettings[] transportSettings, IDeviceClientPipelineBuilder pipelineBuilder)
            : base(iotHubConnectionString, transportSettings)
        {
            var pipelineContext = new PipelineContext();
            pipelineContext.Set(transportSettings);
            pipelineContext.Set(iotHubConnectionString);
            pipelineContext.Set<OnMethodCalledDelegate>(OnMethodCalled);
            pipelineContext.Set<Action<TwinCollection>>(OnReportedStatePatchReceived);
            pipelineContext.Set<OnConnectionClosedDelegate>(OnConnectionClosed);
            pipelineContext.Set<OnConnectionOpenedDelegate>(OnConnectionOpened);
            pipelineContext.Set(this.productInfo);

            IDelegatingHandler innerHandler = pipelineBuilder.Build(pipelineContext);
            this.InnerHandler = innerHandler;
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
            return Create(hostname, authenticationMethod, TransportType.Amqp);
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
            return Create(hostname, gatewayHostname, authenticationMethod, TransportType.Amqp);
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
            return Create(hostname, null, authenticationMethod, transportType);
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
            if (hostname == null)
            {
                throw new ArgumentNullException(nameof(hostname));
            }

            if (authenticationMethod == null)
            {
                throw new ArgumentNullException(nameof(authenticationMethod));
            }

            IotHubConnectionStringBuilder connectionStringBuilder = IotHubConnectionStringBuilder.Create(hostname, gatewayHostname, authenticationMethod);

#if !NETMF
            if (authenticationMethod is DeviceAuthenticationWithX509Certificate)
            {
                if (connectionStringBuilder.Certificate == null)
                {
                    throw new ArgumentException("certificate must be present in DeviceAuthenticationWithX509Certificate");
                }

                if (!connectionStringBuilder.Certificate.HasPrivateKey)
                {
                    throw new ArgumentException("certificate in DeviceAuthenticationWithX509Certificate must have a private key");
                }

                DeviceClient dc = CreateFromConnectionString(connectionStringBuilder.ToString(), PopulateCertificateInTransportSettings(connectionStringBuilder, transportType));
                dc.Certificate = connectionStringBuilder.Certificate;
                return dc;
            }
#endif

            return CreateFromConnectionString(connectionStringBuilder.ToString(), authenticationMethod, transportType, null);
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
            return Create(hostname, null, authenticationMethod, transportSettings);
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
            if (hostname == null)
            {
                throw new ArgumentNullException("hostname");
            }

            if (authenticationMethod == null)
            {
                throw new ArgumentNullException("authenticationMethod");
            }

            var connectionStringBuilder = IotHubConnectionStringBuilder.Create(hostname, gatewayHostname, authenticationMethod);
#if !NETMF
            if (authenticationMethod is DeviceAuthenticationWithX509Certificate)
            {
                if (connectionStringBuilder.Certificate == null)
                {
                    throw new ArgumentException("certificate must be present in DeviceAuthenticationWithX509Certificate");
                }

                if (!connectionStringBuilder.Certificate.HasPrivateKey)
                {
                    throw new ArgumentException("certificate in DeviceAuthenticationWithX509Certificate must have a private key");
                }

                DeviceClient dc = CreateFromConnectionString(connectionStringBuilder.ToString(), PopulateCertificateInTransportSettings(connectionStringBuilder, transportSettings));
                dc.Certificate = connectionStringBuilder.Certificate;
                return dc;
            }
#endif
            return CreateFromConnectionString(connectionStringBuilder.ToString(), authenticationMethod, transportSettings, null);
        }

        /// <summary>
        /// Create a DeviceClient using Amqp transport from the specified connection string
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (including DeviceId)</param>
        /// <returns>DeviceClient</returns>
        public static DeviceClient CreateFromConnectionString(string connectionString)
        {
            return CreateFromConnectionString(connectionString, TransportType.Amqp);
        }

        /// <summary>
        /// Create a DeviceClient using Amqp transport from the specified connection string
        /// </summary>
        /// <param name="connectionString">IoT Hub-Scope Connection string for the IoT hub (without DeviceId)</param>
        /// <param name="deviceId">Id of the Device</param>
        /// <returns>DeviceClient</returns>
        public static DeviceClient CreateFromConnectionString(string connectionString, string deviceId)
        {
            return CreateFromConnectionString(connectionString, deviceId, TransportType.Amqp);
        }

        /// <summary>
        /// Create DeviceClient from the specified connection string using the specified transport type
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (including DeviceId)</param>
        /// <param name="transportType">Specifies whether Amqp or Http transport is used</param>
        /// <returns>DeviceClient</returns>
        public static DeviceClient CreateFromConnectionString(string connectionString, TransportType transportType)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            return CreateFromConnectionString(connectionString, null, transportType, null);
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
        public static DeviceClient CreateFromConnectionString(string connectionString,
            ITransportSettings[] transportSettings)
        {
            return CreateFromConnectionString(connectionString, null, transportSettings, null);
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

        internal static DeviceClient CreateFromConnectionString(
            string connectionString,
            IAuthenticationMethod authenticationMethod,
            TransportType transportType,
            IDeviceClientPipelineBuilder pipelineBuilder)
        {
            ITransportSettings[] transportSettings = GetTransportSettings(transportType);
            return CreateFromConnectionString(
                connectionString,
                authenticationMethod,
                transportSettings,
                pipelineBuilder);
        }

        internal static DeviceClient CreateFromConnectionString(
            string connectionString,
            IAuthenticationMethod authenticationMethod,
            ITransportSettings[] transportSettings,
            IDeviceClientPipelineBuilder pipelineBuilder)
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

            var builder = IotHubConnectionStringBuilder.CreateWithIAuthenticationOverride(
                connectionString,
                authenticationMethod);

            IotHubConnectionString iotHubConnectionString = builder.ToIotHubConnectionString();

            ValidateTransportSettings(transportSettings);

            pipelineBuilder = pipelineBuilder ?? BuildPipeline();

            // Defer concrete DeviceClient creation to OpenAsync
            return new DeviceClient(iotHubConnectionString, transportSettings, pipelineBuilder);
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

            HttpTransportHandler httpTransport = null;
            var context = new PipelineContext();
            context.Set(this.productInfo);

            //We need to add the certificate to the fileUpload httpTransport if DeviceAuthenticationWithX509Certificate
            if (this.Certificate != null)
            {
                Http1TransportSettings transportSettings = new Http1TransportSettings();
                transportSettings.ClientCertificate = this.Certificate;
                httpTransport = new HttpTransportHandler(context, IotHubConnectionString, transportSettings);
            }
            else
            {
                httpTransport = new HttpTransportHandler(context, IotHubConnectionString, new Http1TransportSettings());
            }
            return httpTransport.UploadToBlobAsync(blobName, source);
        }
    }
}
