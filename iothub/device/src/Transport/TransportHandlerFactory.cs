// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client.Extensions;
    using Microsoft.Azure.Devices.Shared;
#if !NETMF
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
#endif

    class TransportHandlerFactory : ITransportHandlerFactory
    {
        public IDelegatingHandler Create(IPipelineContext context)
        {
            var connectionString = context.Get<IotHubConnectionString>();
            var transportSetting = context.Get<ITransportSettings[]>();
            var onMethodCallback = context.Get<DeviceClient.OnMethodCalledDelegate>();
            var onDesiredStatePatchReceived = context.Get<Action<TwinCollection>>();
            var OnConnectionClosedCallback = context.Get<DeviceClient.OnConnectionClosedDelegate>();
            var OnConnectionOpenedCallback = context.Get<DeviceClient.OnConnectionOpenedDelegate>();
            var onReceiveCallback = context.Get<DeviceClient.OnReceiveEventMessageCalledDelegate>();

            switch (transportSetting[0].GetTransportType())
            {
                case TransportType.Amqp_WebSocket_Only:
                case TransportType.Amqp_Tcp_Only:
                    return new AmqpTransportHandler(
                        context, connectionString, transportSetting[0] as AmqpTransportSettings,
                        new Action<object, ConnectionEventArgs>(OnConnectionOpenedCallback),
                        new Func<object, ConnectionEventArgs, Task>(OnConnectionClosedCallback),
                        new Func<MethodRequestInternal, Task>(onMethodCallback), onDesiredStatePatchReceived,
                        new Func<string, Message, Task>(onReceiveCallback));
                case TransportType.Http1:
                    return new HttpTransportHandler(context, connectionString, transportSetting[0] as Http1TransportSettings);
#if !NETMF
                case TransportType.Mqtt_Tcp_Only:
                case TransportType.Mqtt_WebSocket_Only:
                    return new MqttTransportHandler(
                        context, connectionString, transportSetting[0] as MqttTransportSettings,
                        new Action<object, ConnectionEventArgs>(OnConnectionOpenedCallback),
                        new Func<object, ConnectionEventArgs, Task>(OnConnectionClosedCallback),
                        new Func<MethodRequestInternal, Task>(onMethodCallback), onDesiredStatePatchReceived,
                        new Func<string, Message, Task>(onReceiveCallback));
#endif
                default:
                    throw new InvalidOperationException("Unsupported Transport Setting {0}".FormatInvariant(transportSetting));
            }
        }
    }
}
