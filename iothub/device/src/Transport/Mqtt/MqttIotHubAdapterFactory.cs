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
            IotHubConnectionInfo iotHubConnectionString,
            IotHubClientMqttSettings mqttTransportSettings,
            ProductInfo productInfo,
            IotHubClientOptions options)
        {
            IWillMessage willMessage = mqttTransportSettings.HasWill ? _settings.WillMessage : null;

            return new MqttIotHubAdapter(
                iotHubConnectionString.DeviceId,
                iotHubConnectionString.ModuleId,
                iotHubConnectionString.HostName,
                mqttTransportSettings.ClientCertificate != null ? null : iotHubConnectionString,
                mqttTransportSettings,
                willMessage,
                mqttIotHubEventHandler,
                productInfo,
                options);
        }
    }
}
