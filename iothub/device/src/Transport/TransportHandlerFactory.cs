// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Client.Transport.Amqp;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal class TransportHandlerFactory : ITransportHandlerFactory
    {
        public IDelegatingHandler Create(IPipelineContext context)
        {
            // ProtocolRoutingDelegatingHandler configures the ITransportSettings configuration
            // which is different from ITransportSettings[] element.
            ITransportSettings transportSetting = context.Get<ITransportSettings>();
            IotHubConnectionString connectionString = context.Get<IotHubConnectionString>();
            InternalClient.OnMethodCalledDelegate onMethodCallback = context.Get<InternalClient.OnMethodCalledDelegate>();
            Action<TwinCollection> onDesiredStatePatchReceived = context.Get<Action<TwinCollection>>();
            InternalClient.OnModuleEventMessageReceivedDelegate onModuleEventReceivedCallback = context.Get<InternalClient.OnModuleEventMessageReceivedDelegate>();
            InternalClient.OnDeviceMessageReceivedDelegate onDeviceMessageReceivedCallback = context.Get<InternalClient.OnDeviceMessageReceivedDelegate>();

            switch (transportSetting.GetTransportType())
            {
                case TransportType.Amqp_WebSocket_Only:
                case TransportType.Amqp_Tcp_Only:
                    return new AmqpTransportHandler(
                        context,
                        connectionString,
                        transportSetting as AmqpTransportSettings,
                        new Func<MethodRequestInternal, Task>(onMethodCallback),
                        onDesiredStatePatchReceived,
                        new Func<string, Message, Task>(onModuleEventReceivedCallback),
                        new Func<Message, Task>(onDeviceMessageReceivedCallback));

                case TransportType.Http1:
                    return new HttpTransportHandler(
                        context,
                        connectionString,
                        transportSetting as Http1TransportSettings,
                        isClientPrimaryTransportHandler: true);

                case TransportType.Mqtt_Tcp_Only:
                case TransportType.Mqtt_WebSocket_Only:
                    return new MqttTransportHandler(
                        context,
                        connectionString,
                        transportSetting as MqttTransportSettings,
                        new Func<MethodRequestInternal, Task>(onMethodCallback),
                        onDesiredStatePatchReceived,
                        new Func<string, Message, Task>(onModuleEventReceivedCallback),
                        new Func<Message, Task>(onDeviceMessageReceivedCallback));

                default:
                    throw new InvalidOperationException($"Unsupported transport setting {transportSetting}");
            }
        }
    }
}
