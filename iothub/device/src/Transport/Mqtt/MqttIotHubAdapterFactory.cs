// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    internal class MqttIotHubAdapterFactory
    {
        private readonly MqttTransportSettings _settings;

        public MqttIotHubAdapterFactory(MqttTransportSettings settings)
        {
            _settings = settings;
        }

        public MqttIotHubAdapter Create(
            IMqttIotHubEventHandler mqttIotHubEventHandler,
            IotHubConnectionString iotHubConnectionString,
            MqttTransportSettings mqttTransportSettings,
            ProductInfo productInfo,
            ClientOptions options)
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
