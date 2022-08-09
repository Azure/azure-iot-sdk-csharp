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
            ClientConfiguration clientConfiguration = context.ClientConfiguration;
            Func<MethodRequestInternal, Task> onMethodCallback = context.MethodCallback;
            Action<TwinCollection> onDesiredStatePatchReceived = context.DesiredPropertyUpdateCallback;
            Func<string, Message, Task> onModuleEventReceivedCallback = context.ModuleEventCallback;
            Func<Message, Task> onDeviceMessageReceivedCallback = context.DeviceEventCallback;

            if (clientConfiguration.ClientOptions.TransportSettings is IotHubClientAmqpSettings iotHubClientAmqpSettings)
            {
                return new AmqpTransportHandler(
                    context,
                    iotHubClientAmqpSettings,
                    onMethodCallback,
                    onDesiredStatePatchReceived,
                    onModuleEventReceivedCallback,
                    onDeviceMessageReceivedCallback);
            }

            if (clientConfiguration.ClientOptions.TransportSettings is IotHubClientMqttSettings iotHubClientMqttSettings)
            {
                return new MqttTransportHandler(
                    context,
                    iotHubClientMqttSettings,
                    onMethodCallback,
                    onDesiredStatePatchReceived,
                    onModuleEventReceivedCallback,
                    onDeviceMessageReceivedCallback);
            }

            if (clientConfiguration.ClientOptions.TransportSettings is IotHubClientHttpSettings iotHubClientHttpSettings)
            {
                return new HttpTransportHandler(
                    context,
                    iotHubClientHttpSettings,
                    isClientPrimaryTransportHandler: true);
            }

            throw new InvalidOperationException($"Unsupported transport setting {clientConfiguration.ClientOptions.TransportSettings.GetType()}");
        }
    }
}
