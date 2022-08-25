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
            IConnectionCredentials connectionCredentials,
            AdditionalClientInformation additionalClientInformation,
            IotHubClientMqttSettings mqttTransportSettings)
        {
            IWillMessage willMessage = mqttTransportSettings.HasWill ? _settings.WillMessage : null;

            return new MqttIotHubAdapter(
                connectionCredentials.DeviceId,
                connectionCredentials.ModuleId,
                connectionCredentials.HostName,
                connectionCredentials,
                mqttTransportSettings,
                willMessage,
                mqttIotHubEventHandler,
                additionalClientInformation);
        }
    }
}
