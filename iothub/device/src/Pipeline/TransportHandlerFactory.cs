// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Client.Transport.Amqp;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal class TransportHandlerFactory : ITransportHandlerFactory
    {
        public IDelegatingHandler Create(PipelineContext context)
        {
            // ProtocolRoutingDelegatingHandler configures the ITransportSettings configuration
            // which is different from ITransportSettings[] element.
            ITransportSettings transportSetting = context.TransportSettings;
            IotHubConnectionString connectionString = context.IotHubConnectionString;
            InternalClient.OnMethodCalledDelegate onMethodCallback = context.MethodCallback;
            Action<IDictionary<string, object>> onDesiredStatePatchReceived = context.DesiredPropertyUpdateCallback;
            InternalClient.OnModuleEventMessageReceivedDelegate onModuleEventReceivedCallback = context.ModuleEventCallback;
            InternalClient.OnDeviceMessageReceivedDelegate onDeviceMessageReceivedCallback = context.DeviceEventCallback;

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
