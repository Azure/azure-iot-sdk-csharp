// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    internal class MqttIotHubAdapterFactory
    {
        private readonly IotHubClientMqttSettings _settings;

        public MqttIotHubAdapterFactory(IotHubClientMqttSettings settings)
        {
            _settings = settings;
        }

        public MqttIotHubAdapter Create(
            IMqttIotHubEventHandler mqttIotHubEventHandler,
            IClientConfiguration clientConfiguration,
            IotHubClientMqttSettings mqttTransportSettings)
        {
            IWillMessage willMessage = mqttTransportSettings.HasWill ? _settings.WillMessage : null;

            return new MqttIotHubAdapter(
                clientConfiguration.DeviceId,
                clientConfiguration.ModuleId,
                clientConfiguration.GatewayHostName,
                mqttTransportSettings.ClientCertificate != null ? null : clientConfiguration,
                mqttTransportSettings,
                willMessage,
                mqttIotHubEventHandler,
                clientConfiguration.ClientOptions);
        }
    }
}
