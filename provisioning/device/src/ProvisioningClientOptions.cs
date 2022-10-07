// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Options that allow configuration of the provisioning device client instance during initialization.
    /// </summary>
    public class ProvisioningClientOptions
    {
        /// <summary>
        /// Creates an instances of this class with the default transport settings.
        /// </summary>
        /// <remarks>
        /// The default transport protocol is MQTT over TCP.
        /// </remarks>
        public ProvisioningClientOptions()
            : this(new ProvisioningClientMqttSettings())
        {
        }

        /// <summary>
        /// Creates an instance of this class with the specified transport settings.
        /// </summary>
        /// <param name="transportSettings">The transport settings to use (i.e., <see cref="ProvisioningTransportHandlerAmqp"/>,
        /// <see cref="ProvisioningTransportHandlerMqtt"/>).</param>
        /// <exception cref="ArgumentNullException">When <paramref name="transportSettings"/> is null.</exception>
        public ProvisioningClientOptions(ProvisioningClientTransportSettings transportSettings)
        {
            TransportSettings = transportSettings ?? throw new ArgumentNullException(nameof(transportSettings));
            if (transportSettings is not ProvisioningClientMqttSettings
                && transportSettings is not ProvisioningClientAmqpSettings)
            {
                throw new ArgumentException("Transport settings must be MQTT, AMQP.", nameof(transportSettings));
            }
        }

        /// <summary>
        /// The transport settings to use (i.e., <see cref="ProvisioningClientMqttSettings"/>,
        /// <see cref="ProvisioningClientAmqpSettings"/>).
        /// </summary>
        public ProvisioningClientTransportSettings TransportSettings { get; }

        /// <summary>
        /// Specifies additional information that will be appended to the user-agent string that is sent to IoT hub.
        /// </summary>
        public string AdditionalUserAgentInfo
        {
            get => UserAgentInfo.Extra;
            set => UserAgentInfo.Extra = value;
        }

        internal virtual ProductInfo UserAgentInfo { get; } = new();

        internal ProvisioningClientOptions Clone()
        {
            ProvisioningClientTransportSettings transport = TransportSettings.Clone();

            return new ProvisioningClientOptions(transport)
            {
                AdditionalUserAgentInfo = AdditionalUserAgentInfo,
            };
        }
    }
}
