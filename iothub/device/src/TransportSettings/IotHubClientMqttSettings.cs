// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Net.Security;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Contains MQTT transport-specific settings for the device and module clients.
    /// </summary>
    public sealed class IotHubClientMqttSettings : IotHubClientTransportSettings
    {
        /// <summary>
        /// Creates an instance of this class with a default protocol of TCP.
        /// </summary>
        /// <param name="transportProtocol">The transport protocol.</param>
        public IotHubClientMqttSettings(IotHubClientTransportProtocol transportProtocol = IotHubClientTransportProtocol.Tcp)
        {
            Protocol = transportProtocol;
        }

        /// <summary>
        /// The QoS to be used when sending packets to service.
        /// </summary>
        /// <remarks>
        /// The default value is <see cref="QualityOfService.AtLeastOnce"/>.
        /// </remarks>
        public QualityOfService PublishToServerQoS { get; set; } = QualityOfService.AtLeastOnce;

        /// <summary>
        /// The QoS to be used when subscribing to receive packets from the service.
        /// </summary>
        /// <remarks>
        /// The default value is <see cref="QualityOfService.AtLeastOnce"/>.
        /// </remarks>
        public QualityOfService ReceivingQoS { get; set; } = QualityOfService.AtLeastOnce;

        /// <summary>
        /// Flag to specify if a subscription should persist across different sessions. The default value is false.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If set to false, the device will receive messages that were sent to it while it was disconnected.
        /// </para>
        /// <para>
        /// If set to true, the device will receive only those messages that were sent to it after it successfully
        /// subscribed to the device bound message topic.
        /// </para>
        /// </remarks>
        public bool CleanSession { get; set; } // false by default

        /// <summary>
        /// Specify client-side heartbeat interval.
        /// The interval, that the client establishes with the service, for sending keep alive pings.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default value is 2 minutes.
        /// </para>
        /// <para>
        /// The client will consider the connection as disconnected if the keep alive ping fails.
        /// Setting a very low idle timeout value can cause aggressive reconnects, and might not give the
        /// client enough time to establish a connection before disconnecting and reconnecting.
        /// </para>
        /// </remarks>
        public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// A keep-alive for the transport layer in sending ping/pong control frames when using web sockets.
        /// </summary>
        /// <remarks>
        /// This value is different from the protocol-level keep-alive packets that are sent over the overlaying MQTT transport protocol.
        /// </remarks>
        /// <seealso href="https://docs.microsoft.com/dotnet/api/system.net.websockets.clientwebsocketoptions.keepaliveinterval"/>
        public TimeSpan? WebSocketKeepAlive { get; set; }

        /// <summary>
        /// The configured will message that is sent to the telemetry channel on an ungraceful disconnect.
        /// </summary>
        /// <remarks>
        /// Setting a will message is a way for clients to notify other subscribed clients about ungraceful disconnects in an appropriate way.
        /// In response to the ungraceful disconnect, the service will send the last-will message to the configured telemetry channel.
        /// The telemetry channel can be either the default Events endpoint or a custom endpoint defined by IoT hub routing.
        /// For more details, refer to <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-mqtt-support#using-the-mqtt-protocol-directly-as-a-device"/>.
        /// </remarks>
        public IWillMessage WillMessage { get; set; }

        /// <summary>
        /// A callback for remote certificate validation.
        /// </summary>
        /// <remarks>
        /// If incorrectly implemented, your device may fail to connect to IoT hub and/or be open to security vulnerabilities.
        /// </remarks>
        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }

        /// <summary>
        /// C2D message queue size when callback has not been set.
        /// </summary>
        /// <remarks>
        /// If C2D message callback is not set, messages from service are received and stored in a queue until a callback is set. 
        /// If the number of messages sent is greater than queue size, older messages are removed as latest messages are added.
        /// </remarks>
        public int MessageQueueSize { get; set; } = 10;

        /// <summary>
        /// Used by Edge runtime to specify an authentication chain for Edge-to-Edge connections.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string AuthenticationChain { get; set; }

        internal override IotHubClientTransportSettings Clone()
        {
            return new IotHubClientMqttSettings(Protocol)
            {
                Proxy = Proxy,
                SslProtocols = SslProtocols,
                CertificateRevocationCheck = CertificateRevocationCheck,
                PublishToServerQoS = PublishToServerQoS,
                ReceivingQoS = ReceivingQoS,
                CleanSession = CleanSession,
                IdleTimeout = IdleTimeout,
                WebSocketKeepAlive = WebSocketKeepAlive,
                WillMessage = WillMessage,
                RemoteCertificateValidationCallback = RemoteCertificateValidationCallback,
                AuthenticationChain = AuthenticationChain,
                MessageQueueSize = MessageQueueSize,
            };
        }
    }
}
