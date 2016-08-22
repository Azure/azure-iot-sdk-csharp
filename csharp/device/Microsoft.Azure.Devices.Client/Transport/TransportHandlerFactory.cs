// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    using System;
    using Microsoft.Azure.Devices.Client.Extensions;

#if !WINDOWS_UWP && !NETMF
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
#endif
    class TransportHandlerFactory : ITransportHandlerFactory
    {
        public IDelegatingHandler Create(IPipelineContext context)
        {
            var connectionString = context.Get<IotHubConnectionString>();
            var transportSetting = context.Get<ITransportSettings>();

            switch (transportSetting.GetTransportType())
            {
                case TransportType.Amqp_WebSocket_Only:
                case TransportType.Amqp_Tcp_Only:
                    return new AmqpTransportHandler(context, connectionString, transportSetting as AmqpTransportSettings);
                case TransportType.Http1:
                    return new HttpTransportHandler(context, connectionString, transportSetting as Http1TransportSettings);
#if !WINDOWS_UWP && !NETMF
                    case TransportType.Mqtt:
                        return new MqttTransportHandler(context, connectionString, transportSetting as MqttTransportSettings);
#endif
                default:
                    throw new InvalidOperationException("Unsupported Transport Setting {0}".FormatInvariant(transportSetting));
            }
        }
    }
}