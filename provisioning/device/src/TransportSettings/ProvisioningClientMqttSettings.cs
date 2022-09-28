// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Security;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Contains MQTT transport-specific settings for the device and module clients.
    /// </summary>
    public class ProvisioningClientMqttSettings : ProvisioningClientTransportSettings
    {
        private const bool DefaultCleanSession = false;
        private readonly TimeSpan DefaultKeepAlive = TimeSpan.FromMinutes(2);
        private const QualityOfService DefaultPublishToServerQoS = QualityOfService.AtLeastOnce;
        private const QualityOfService DefaultReceivingQoS = QualityOfService.AtLeastOnce;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="transportProtocol">The transport protocol; defaults to TCP.</param>
        public ProvisioningClientMqttSettings(ProvisioningClientTransportProtocol transportProtocol = ProvisioningClientTransportProtocol.Tcp)
        {
            Protocol = transportProtocol;
            CleanSession = DefaultCleanSession;
            IdleTimeout = DefaultKeepAlive;
            PublishToServerQoS = DefaultPublishToServerQoS;
            ReceivingQoS = DefaultReceivingQoS;
        }

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
        /// Flag to specify if a subscription should persist across different sessions. The default value is false.
        /// </summary>
        /// <remarks>
        /// <para>If set to false: the device will receive messages that were sent to it while it was disconnected.</para>
        /// <para>If set to true: the device will receive only those messages that were sent to it
        /// after it successfully subscribed to the device bound message topic.</para>
        /// </remarks>
        public bool CleanSession { get; set; }

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
        /// A callback for remote certificate validation.
        /// If incorrectly implemented, your device may fail to connect to IoTHub and/or be open to security vulnerabilities.
        /// </summary>
        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }
    }
}
