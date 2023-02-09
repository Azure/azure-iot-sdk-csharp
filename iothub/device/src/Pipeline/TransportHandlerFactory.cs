// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Client.Transport.Amqp;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal sealed class TransportHandlerFactory : ITransportHandlerFactory
    {
        public IDelegatingHandler Create(PipelineContext context)
        {
            IotHubClientTransportSettings transportSettings = context.IotHubClientTransportSettings;

            return transportSettings switch
            {
                IotHubClientAmqpSettings iotHubClientAmqpSettings => new AmqpTransportHandler(
                    context,
                    iotHubClientAmqpSettings),

                IotHubClientMqttSettings iotHubClientMqttSettings => new MqttTransportHandler(
                    context,
                    iotHubClientMqttSettings),

                _ => throw new InvalidOperationException($"Unsupported transport setting {context.IotHubClientTransportSettings.GetType()}")
            };
        }
    }
}
