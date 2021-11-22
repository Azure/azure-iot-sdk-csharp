// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNetty.Codecs.Mqtt.Packets;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    /// <summary>
    /// Settings for MQTT transport
    /// </summary>
    public class MqttTransportSettings : ITransportSettings
    {
        private readonly TransportType _transportType;

        private const bool DefaultCleanSession = false;
        private const bool DefaultDeviceReceiveAckCanTimeout = false;
        private const bool DefaultHasWill = false;
        private const bool DefaultMaxOutboundRetransmissionEnforced = false;
        private const int DefaultKeepAliveInSeconds = 300;
        private const int DefaultMaxPendingInboundMessages = 50;
        private const QualityOfService DefaultPublishToServerQoS = QualityOfService.AtLeastOnce;
        private const QualityOfService DefaultReceivingQoS = QualityOfService.AtLeastOnce;

        // The CONNACK timeout has been chosen to be 60 seconds to be in alignment with the service implemented timeout for processing connection requests.
        private static readonly TimeSpan s_defaultConnectArrivalTimeout = TimeSpan.FromSeconds(60);

        private static readonly TimeSpan s_defaultDeviceReceiveAckTimeout = TimeSpan.FromSeconds(300);
        private static readonly TimeSpan s_defaultReceiveTimeout = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Creates an instance based on the specified type options
        /// </summary>
        /// <param name="transportType">The transport type, of Mqtt_WebSocket_Only, or Mqtt_Tcp_Only</param>
        public MqttTransportSettings(TransportType transportType)
        {
            _transportType = transportType;

            switch (transportType)
            {
                case TransportType.Mqtt_WebSocket_Only:
                    Proxy = DefaultWebProxySettings.Instance;
                    _transportType = transportType;
                    break;

                case TransportType.Mqtt_Tcp_Only:
                    _transportType = transportType;
                    break;

                case TransportType.Mqtt:
                    throw new ArgumentOutOfRangeException(nameof(transportType), transportType, "Must specify Mqtt_WebSocket_Only or Mqtt_Tcp_Only");

                default:
                    throw new ArgumentOutOfRangeException(nameof(transportType), transportType, "Unsupported Transport Type {0}".FormatInvariant(transportType));
            }

            CleanSession = DefaultCleanSession;
            ConnectArrivalTimeout = s_defaultConnectArrivalTimeout;
            DeviceReceiveAckCanTimeout = DefaultDeviceReceiveAckCanTimeout;
            DeviceReceiveAckTimeout = s_defaultDeviceReceiveAckTimeout;
            DupPropertyName = "mqtt-dup";
            HasWill = DefaultHasWill;
            KeepAliveInSeconds = DefaultKeepAliveInSeconds;
            MaxOutboundRetransmissionEnforced = DefaultMaxOutboundRetransmissionEnforced;
            MaxPendingInboundMessages = DefaultMaxPendingInboundMessages;
            PublishToServerQoS = DefaultPublishToServerQoS;
            ReceivingQoS = DefaultReceivingQoS;
            QoSPropertyName = "mqtt-qos";
            RetainPropertyName = "mqtt-retain";
            WillMessage = null;
            DefaultReceiveTimeout = s_defaultReceiveTimeout;
        }

        /// <summary>
        /// Indicates if certificate revocation check is enabled. The default value is <c>false</c>.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Performance",
            "CA1822:Mark members as static",
            Justification = "Cannot change public property in public facing classes.")]
        public bool CertificateRevocationCheck
        {
            get => TlsVersions.Instance.CertificateRevocationCheck;
            set => TlsVersions.Instance.CertificateRevocationCheck = value;
        }

        /// <summary>
        /// Indicates if a device can timeout while waiting for a acknowledgment from service.
        /// The default value is <c>false</c>.
        /// </summary>
        /// <remarks>
        /// This property is currently unused.
        /// </remarks>
        public bool DeviceReceiveAckCanTimeout { get; set; }

        /// <summary>
        /// The time a device will wait for an acknowledgment from service.
        /// The default is 5 minutes.
        /// </summary>
        /// <remarks>
        /// This property is currently unused.
        /// </remarks>
        public TimeSpan DeviceReceiveAckTimeout { get; set; }

        /// <summary>
        /// The QoS to be used when sending packets to service.
        /// The default value is <see cref="QualityOfService.AtLeastOnce"/>.
        /// </summary>
        public QualityOfService PublishToServerQoS { get; set; }

        /// <summary>
        /// The QoS to be used when subscribing to receive packets from the service.
        /// The default value is <see cref="QualityOfService.AtLeastOnce"/>.
        /// </summary>
        public QualityOfService ReceivingQoS { get; set; }

        /// <summary>
        /// The property on a message that indicates the publish packet has requested to be retained.
        /// </summary>
        /// <remarks>
        /// This property is currently unused.
        /// </remarks>
        public string RetainPropertyName { get; set; }

        /// <summary>
        /// The property on a message that indicates the publish packet is marked as a duplicate.
        /// </summary>
        /// <remarks>
        /// This property is currently unused.
        /// </remarks>
        public string DupPropertyName { get; set; }

        /// <summary>
        /// The property name setting the QoS for a packet.
        /// </summary>
        /// <remarks>
        /// This property is currently unused.
        /// </remarks>
        public string QoSPropertyName { get; set; }

        /// <summary>
        /// Indicates if max outbound retransmission is enforced.
        /// The default value is <c>false</c>.
        /// </summary>
        /// <remarks>
        /// This property is currently unused.
        /// </remarks>
        public bool MaxOutboundRetransmissionEnforced { get; set; }

        /// <summary>
        /// The maximum no. of inbound messages that are read from the channel.
        /// The default value is 50.
        /// </summary>
        public int MaxPendingInboundMessages { get; set; }

        /// <summary>
        /// The time to wait for receiving an acknowledgment for a CONNECT packet.
        /// The default is 60 seconds.
        /// </summary>
        /// <remarks>
        /// In the event that IoT Hub receives burst traffic, it will implement traffic shaping in order to process the incoming requests.
        /// In such cases, during client connection the CONNECT requests can have a delay in being acknowledged and processed by IoT Hub.
        /// The <c>ConnectArrivalTimeout</c> governs the duration the client will wait for a CONNACK packet before disconnecting and reopening the connection.
        /// To know more about IoT Hub's throttling limits and traffic shaping feature, see
        /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-quotas-throttling#operation-throttles"/>.
        /// </remarks>
        public TimeSpan ConnectArrivalTimeout { get; set; }

        /// <summary>
        /// Flag to specify if a subscription should persist across different sessions. The default value is false.
        /// </summary>
        /// <remarks>
        /// <para>If set to false: the device will receive messages that were sent to it while it was disconnected.</para>
        /// <para>If set to true: the device will receive only those messages that were sent to it
        /// after it successfully subscribed to the device bound message topic.</para>
        /// </remarks>
        public bool CleanSession { get; set; }

        /// <summary>
        /// The interval, in seconds, that the client establishes with the service, for sending keep-alive pings.
        /// The default is 300 seconds.
        /// </summary>
        /// <remarks>
        /// The client will send a ping request 4 times per keep-alive duration set.
        /// It will wait for 30 seconds for the ping response, else mark the connection as disconnected.
        /// Setting a very low keep-alive value can cause aggressive reconnects, and might not give the
        /// client enough time to establish a connection before disconnecting and reconnecting.
        /// </remarks>
        public int KeepAliveInSeconds { get; set; }

        /// <summary>
        /// Indicates whether the transport has a will message.
        /// </summary>
        /// <remarks>
        /// Setting a will message is a way for clients to notify other subscribed clients about ungraceful disconnects in an appropriate way.
        /// In response to the ungraceful disconnect, the service will send the last-will message to the configured telemetry channel.
        /// The telemetry channel can be either the default Events endpoint or a custom endpoint defined by IoT Hub routing.
        /// For more details, refer to https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-mqtt-support#using-the-mqtt-protocol-directly-as-a-device.
        /// </remarks>
        public bool HasWill { get; set; }

        /// <summary>
        /// The configured will message that is sent to the telemetry channel on an ungraceful disconnect.
        /// </summary>
        /// <remarks>
        /// The telemetry channel can be either the default Events endpoint or a custom endpoint defined by IoT Hub routing.
        /// For more details, refer to https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-mqtt-support#using-the-mqtt-protocol-directly-as-a-device.
        /// </remarks>
        public IWillMessage WillMessage { get; set; }

        /// <summary>
        /// The time to wait for a receive operation. The default value is 1 minute.
        /// </summary>
        /// <remarks>
        /// This property is currently unused.
        /// </remarks>
        public TimeSpan DefaultReceiveTimeout { get; set; }

        /// <summary>
        /// A callback for remote certificate validation.
        /// If incorrectly implemented, your device may fail to connect to IoTHub and/or be open to security vulnerabilities.
        /// </summary>
        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }

        /// <summary>
        /// The client certificate to be used for authenticating the TLS connection.
        /// </summary>
        public X509Certificate ClientCertificate { get; set; }

        /// <summary>
        /// The proxy settings to be used when communicating with IoT Hub.
        /// </summary>
        public IWebProxy Proxy { get; set; }

        /// <summary>
        /// The connection transport type.
        /// </summary>
        /// <returns></returns>
        public TransportType GetTransportType()
        {
            return _transportType;
        }

        /// <summary>
        /// Used by Edge runtime to specify an authentication chain for Edge-to-Edge connections.
        /// </summary>
        internal string AuthenticationChain { get; set; }

        /// <summary>
        /// The DotNetty event loop GracefulShutdown timeout.
        /// </summary>
        /// <remarks>
        /// The DotNetty Mqtt transport has a graceful shutdown of the event loop that can block the <see cref="DeviceClient.CloseAsync(System.Threading.CancellationToken)"/> method. This timeout allows you to configure how long the shutdown event will wait for before forcefully shutting down the event loop. Change this if you need your application to respond to the CloseAsync command faster.
        /// </remarks>
        public TimeSpan GracefulEventLoopShutdownTimeout { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Sets the MqttTransport to use a single Event Loop Group.
        /// </summary>
        /// <remarks>
        /// This setting is to retain legacy behavior that will use a single Event Loop Group which is the thread pool model for DotNetty. If this is set to false a new group will be created per instance of the Device Client.
        /// </remarks>
        public bool UseSingleEventLoopGroup { get; set; } = true;
    }
}
