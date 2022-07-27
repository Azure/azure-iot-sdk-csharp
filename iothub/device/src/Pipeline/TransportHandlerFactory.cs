// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Transport.Amqp;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal class TransportHandlerFactory : ITransportHandlerFactory
    {
        public IDelegatingHandler Create(PipelineContext context)
        {
            // ProtocolRoutingDelegatingHandler configures the TransportSettings configuration
            // which is different from TransportSettings[] element.
            TransportSettings transportSettings = context.TransportSettings;
            IotHubConnectionString connectionString = context.IotHubConnectionString;
            InternalClient.OnMethodCalledDelegate onMethodCallback = context.MethodCallback;
            Action<TwinCollection> onDesiredStatePatchReceived = context.DesiredPropertyUpdateCallback;
            InternalClient.OnModuleEventMessageReceivedDelegate onModuleEventReceivedCallback = context.ModuleEventCallback;
            InternalClient.OnDeviceMessageReceivedDelegate onDeviceMessageReceivedCallback = context.DeviceEventCallback;

            if (transportSettings is IotHubClientAmqpSettings)
            {
                return new AmqpTransportHandler(
                    context,
                    connectionString,
                    transportSettings as IotHubClientAmqpSettings,
                    new Func<MethodRequestInternal, Task>(onMethodCallback),
                    onDesiredStatePatchReceived,
                    new Func<string, Message, Task>(onModuleEventReceivedCallback),
                    new Func<Message, Task>(onDeviceMessageReceivedCallback));
            }

            if (transportSettings is IotHubClientMqttSettings)
            {
                return new MqttTransportHandler(
                    context,
                    connectionString,
                    transportSettings as IotHubClientMqttSettings,
                    new Func<MethodRequestInternal, Task>(onMethodCallback),
                    onDesiredStatePatchReceived,
                    new Func<string, Message, Task>(onModuleEventReceivedCallback),
                    new Func<Message, Task>(onDeviceMessageReceivedCallback));
            }

            if (transportSettings is IotHubClientHttpSettings)
            {
                return new HttpTransportHandler(
                    context,
                    connectionString,
                    transportSettings as IotHubClientHttpSettings,
                    isClientPrimaryTransportHandler: true);
            }

            throw new InvalidOperationException($"Unsupported transport setting {transportSettings.GetType()}");
        }
    }
}
