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
    public class MqttTransportSettings : ITransportSettings
    {
        private readonly TransportType _transportType;

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
        public bool CertificateRevocationCheck = false;

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

        public bool HasWill { get; set; }

        public IWillMessage WillMessage { get; set; }

        public TransportType GetTransportType()
        {
            return _transportType;
        }

        public TimeSpan DefaultReceiveTimeout { get; set; }

        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }

        public X509Certificate ClientCertificate { get; set; }

        public IWebProxy Proxy { get; set; }

    }
}
