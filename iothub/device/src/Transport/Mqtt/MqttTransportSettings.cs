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

        /// <summary>
        /// Used by Edge runtime to specify an authentication chain for Edge-to-Edge connections
        /// </summary>
        internal string AuthenticationChain { get; set; }

        private const bool DefaultCleanSession = false;
        private const bool DefaultDeviceReceiveAckCanTimeout = false;
        private const bool DefaultHasWill = false;
        private const bool DefaultMaxOutboundRetransmissionEnforced = false;
        private const int DefaultKeepAliveInSeconds = 300;
        private const int DefaultReceiveTimeoutInSeconds = 60;
        private const int DefaultMaxPendingInboundMessages = 50;
        private const QualityOfService DefaultPublishToServerQoS = QualityOfService.AtLeastOnce;
        private const QualityOfService DefaultReceivingQoS = QualityOfService.AtLeastOnce;
        private static readonly TimeSpan DefaultConnectArrivalTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan DefaultDeviceReceiveAckTimeout = TimeSpan.FromSeconds(300);

        /// <summary>
        /// To enable certificate revocation check. Default to be false.
        /// </summary>
#pragma warning disable IDE1006 // Naming Styles
        public bool CertificateRevocationCheck
        {
            get => TlsVersions.Instance.CertificateRevocationCheck;
            set => TlsVersions.Instance.CertificateRevocationCheck = value;
        }
#pragma warning restore IDE1006 // Naming Styles

        /// <summary>
        /// Creates an instance based on the specified type options
        /// </summary>
        /// <param name="transportType">The transport type, of MQTT websocket only, or MQTT TCP only</param>
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
            ConnectArrivalTimeout = DefaultConnectArrivalTimeout;
            DeviceReceiveAckCanTimeout = DefaultDeviceReceiveAckCanTimeout;
            DeviceReceiveAckTimeout = DefaultDeviceReceiveAckTimeout;
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
            DefaultReceiveTimeout = TimeSpan.FromSeconds(DefaultReceiveTimeoutInSeconds);
        }

        public bool DeviceReceiveAckCanTimeout { get; set; }

        public TimeSpan DeviceReceiveAckTimeout { get; set; }

        public QualityOfService PublishToServerQoS { get; set; }

        public QualityOfService ReceivingQoS { get; set; }

        public string RetainPropertyName { get; set; }

        public string DupPropertyName { get; set; }

        public string QoSPropertyName { get; set; }

        public bool MaxOutboundRetransmissionEnforced { get; set; }

        public int MaxPendingInboundMessages { get; set; }

        public TimeSpan ConnectArrivalTimeout { get; set; }

        public bool CleanSession { get; set; }

        public int KeepAliveInSeconds { get; set; }

        /// <summary>
        /// Whether the transport has a will message
        /// </summary>
        public bool HasWill { get; set; }

        /// <summary>
        /// The configured will message
        /// </summary>
        public IWillMessage WillMessage { get; set; }

        public TimeSpan DefaultReceiveTimeout { get; set; }

        /// <summary>
        /// A callback for remote certificate validation.
        /// If incorrectly implemented, your device may fail to connect to IoTHub and/or be open to security vulnerabilities.
        /// </summary>
        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }

        /// <summary>
        /// The client certificate
        /// </summary>
        public X509Certificate ClientCertificate { get; set; }

        /// <summary>
        /// A proxy to use - optional
        /// </summary>
        public IWebProxy Proxy { get; set; }

        /// <summary>
        /// The conftransport type
        /// </summary>
        /// <returns></returns>
        public TransportType GetTransportType()
        {
            return _transportType;
        }
    }
}
