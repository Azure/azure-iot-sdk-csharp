// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Client.Transport.Amqp;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal class TransportHandlerFactory : ITransportHandlerFactory
    {
        public IDelegatingHandler Create(PipelineContext context)
        {
            IotHubClientTransportSettings transportSettings = context.IotHubClientTransportSettings;

            if (transportSettings is IotHubClientAmqpSettings iotHubClientAmqpSettings)
            {
                return new AmqpTransportHandler(
                    context,
                    iotHubClientAmqpSettings);
            }

            if (transportSettings is IotHubClientMqttSettings iotHubClientMqttSettings)
            {
                return new MqttTransportHandler(
                    context,
                    iotHubClientMqttSettings);
            }

            throw new InvalidOperationException($"Unsupported transport setting {context.IotHubClientTransportSettings.GetType()}");
        }
    }
}
