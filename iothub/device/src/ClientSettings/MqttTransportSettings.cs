// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using DotNetty.Codecs.Mqtt.Packets;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Contains MQTT transport-specific settings for the device and module clients.
    /// </summary>
    public class MqttTransportSettings : ITransportSettings
    {
        private const bool DefaultCleanSession = false;
        private const bool DefaultHasWill = false;
        private const int DefaultKeepAliveInSeconds = 300;
        private const int DefaultMaxPendingInboundMessages = 50;
        private const QualityOfService DefaultPublishToServerQoS = QualityOfService.AtLeastOnce;
        private const QualityOfService DefaultReceivingQoS = QualityOfService.AtLeastOnce;

        // The CONNACK timeout has been chosen to be 60 seconds to be in alignment with the service implemented timeout for processing connection requests.
        private static readonly TimeSpan s_defaultConnectArrivalTimeout = TimeSpan.FromSeconds(60);

        private static readonly TimeSpan s_defaultReceiveTimeout = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="transportProtocol">The transport protocol; defaults to TCP.</param>
        public MqttTransportSettings(TransportProtocol transportProtocol = TransportProtocol.Tcp)
        {
            Protocol = transportProtocol;
            if (Protocol == TransportProtocol.WebSocket)
            {
                Proxy = DefaultWebProxySettings.Instance;
            }

            CleanSession = DefaultCleanSession;
            ConnectArrivalTimeout = s_defaultConnectArrivalTimeout;
            HasWill = DefaultHasWill;
            KeepAliveInSeconds = DefaultKeepAliveInSeconds;
            MaxPendingInboundMessages = DefaultMaxPendingInboundMessages;
            PublishToServerQoS = DefaultPublishToServerQoS;
            ReceivingQoS = DefaultReceivingQoS;
            WillMessage = null;
            DefaultReceiveTimeout = s_defaultReceiveTimeout;
        }

        /// <inheritdoc/>
        public TransportProtocol Protocol { get; }

        /// <inheritdoc/>
        public X509Certificate2 ClientCertificate { get; set; }

        /// <summary>
        /// Indicates if certificate revocation check is enabled. The default value is <c>false</c>.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Performance",
            "CA1822:Mark members as static",
            Justification = "Cannot change public property in public facing classes.")]
        public bool CertificateRevocationCheck { get; set; }

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
        /// The maximum no. of inbound messages that are read from the channel.
        /// The default value is 50.
        /// </summary>
        public int MaxPendingInboundMessages { get; set; }

        /// <summary>
        /// The time to wait for receiving an acknowledgment for a CONNECT packet.
        /// The default is 60 seconds.
        /// </summary>
        /// <remarks>
        /// In the event that IoT hub receives burst traffic, it will implement traffic shaping in order to process the incoming requests.
        /// In such cases, during client connection the CONNECT requests can have a delay in being acknowledged and processed by IoT hub.
        /// The <c>ConnectArrivalTimeout</c> governs the duration the client will wait for a CONNACK packet before disconnecting and reopening the connection.
        /// To know more about IoT hub's throttling limits and traffic shaping feature, see
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
        /// The interval, in seconds, that the client establishes with the service, for sending protocol-level keep-alive pings.
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
        /// A keep-alive for the transport layer in sending ping/pong control frames when using web sockets.
        /// </summary>
        /// <remarks>
        /// This value is different from the protocol-level keep-alive packets that are sent over the overlaying MQTT transport protocol.
        /// </remarks>
        /// <seealso href="https://docs.microsoft.com/dotnet/api/system.net.websockets.clientwebsocketoptions.keepaliveinterval"/>
        public TimeSpan? WebSocketKeepAlive { get; set; }

        /// <summary>
        /// Indicates whether the transport has a will message.
        /// </summary>
        /// <remarks>
        /// Setting a will message is a way for clients to notify other subscribed clients about ungraceful disconnects in an appropriate way.
        /// In response to the ungraceful disconnect, the service will send the last-will message to the configured telemetry channel.
        /// The telemetry channel can be either the default Events endpoint or a custom endpoint defined by IoT hub routing.
        /// For more details, refer to https://docs.microsoft.com/azure/iot-hub/iot-hub-mqtt-support#using-the-mqtt-protocol-directly-as-a-device.
        /// </remarks>
        public bool HasWill { get; set; }

        /// <summary>
        /// The configured will message that is sent to the telemetry channel on an ungraceful disconnect.
        /// </summary>
        /// <remarks>
        /// The telemetry channel can be either the default Events endpoint or a custom endpoint defined by IoT hub routing.
        /// For more details, refer to https://docs.microsoft.com/azure/iot-hub/iot-hub-mqtt-support#using-the-mqtt-protocol-directly-as-a-device.
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

        /// <inheritdoc/>
        public IWebProxy Proxy { get; set; }

        /// <summary>
        /// Used by Edge runtime to specify an authentication chain for Edge-to-Edge connections.
        /// </summary>
        internal string AuthenticationChain { get; set; }

        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString()
        {
            return $"{GetType().Name}/{Protocol}";
        }

        /// <summary>
        /// The acceptable versions of TLS to use when the SDK must be explicit.
        /// </summary>
        public SslProtocols MinimumTlsVersions { get; set; } = SslProtocols.Tls12;

        /// <summary>
        /// The version of TLS to use by default.
        /// </summary>
        /// <remarks>
        /// Defaults to "None", which means let the OS decide the proper TLS version (SChannel in Windows / OpenSSL in Linux).
        /// </remarks>
        public SslProtocols Preferred { get; set; } = SslProtocols.None;

#pragma warning disable CA5397 // Do not use deprecated SslProtocols values
        private const SslProtocols AllowedProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
        private const SslProtocols PreferredProtocol = SslProtocols.Tls12;

        /// <summary>
        /// Sets the minimum acceptable versions of TLS.
        /// </summary>
        /// <remarks>
        /// Will ignore a version less than TLS 1.0.
        ///
        /// Affects:
        /// 1. MinimumTlsVersions property
        /// 2. Preferred property
        /// 3. For .NET framework 4.5.1 over HTTPS or websocket, as it does not offer a "SystemDefault" option, the version must be explicit.
        /// </remarks>
        public void SetMinimumTlsVersions(SslProtocols protocols = SslProtocols.None)
        {
            // sanitize to only those that are allowed
            protocols &= AllowedProtocols;

            if (protocols == SslProtocols.None)
            {
                Preferred = SslProtocols.None;
                MinimumTlsVersions = PreferredProtocol;
                return;
            }

            // ensure the preferred TLS version is included
            if (((protocols & SslProtocols.Tls) != 0
                    || (protocols & SslProtocols.Tls11) != 0)
                && (protocols & PreferredProtocol) == 0)
            {
                protocols ^= PreferredProtocol;
            }

            MinimumTlsVersions = Preferred = protocols;
        }

#pragma warning restore CA5397 // Do not use deprecated SslProtocols values
    }
}
