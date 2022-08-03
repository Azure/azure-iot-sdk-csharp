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
            IotHubConnectionInfo connInfo = context.IotHubConnectionInfo;
            InternalClient.OnMethodCalledDelegate onMethodCallback = context.MethodCallback;
            Action<TwinCollection> onDesiredStatePatchReceived = context.DesiredPropertyUpdateCallback;
            InternalClient.OnModuleEventMessageReceivedDelegate onModuleEventReceivedCallback = context.ModuleEventCallback;
            InternalClient.OnDeviceMessageReceivedDelegate onDeviceMessageReceivedCallback = context.DeviceEventCallback;

            if (connInfo.ClientOptions.TransportSettings is IotHubClientAmqpSettings iotHubClientAmqpSettings)
            {
                return new AmqpTransportHandler(
                    context,
                    iotHubClientAmqpSettings);
            }

            if (connInfo.ClientOptions.TransportSettings is IotHubClientMqttSettings iotHubClientMqttSettings)
            {
                return new MqttTransportHandler(
                    context,
                    connInfo,
                    iotHubClientMqttSettings,
                    new Func<MethodRequestInternal, Task>(onMethodCallback),
                    onDesiredStatePatchReceived,
                    new Func<string, Message, Task>(onModuleEventReceivedCallback),
                    new Func<Message, Task>(onDeviceMessageReceivedCallback));
            }

            if (connInfo.ClientOptions.TransportSettings is IotHubClientHttpSettings iotHubClientHttpSettings)
            {
                return new HttpTransportHandler(
                    context,
                    connInfo,
                    iotHubClientHttpSettings,
                    isClientPrimaryTransportHandler: true);
            }

            throw new InvalidOperationException($"Unsupported transport setting {connInfo.ClientOptions.TransportSettings.GetType()}");
        }
    }
}
