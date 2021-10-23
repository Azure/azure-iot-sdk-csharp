// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Test.ConnectionString;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.Azure.Devices.Client.Transport.Amqp;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Test.Transport
{
    internal class MoqableAmqpUnit : AmqpUnit
    {
        public MoqableAmqpUnit() : this(new DeviceIdentity(IotHubConnectionStringExtensions.Parse(AmqpTransportHandlerTests.TestConnectionString), new AmqpTransportSettings(TransportType.Amqp_Tcp_Only), new ProductInfo(), new ClientOptions()),
            new AmqpConnectionHolder(new DeviceIdentity(IotHubConnectionStringExtensions.Parse(AmqpTransportHandlerTests.TestConnectionString), new AmqpTransportSettings(TransportType.Amqp_Tcp_Only), new ProductInfo(), new ClientOptions())))
        {
        }

        public MoqableAmqpUnit(DeviceIdentity deviceIdentity,
            IAmqpConnectionHolder amqpConnectionHolder,
            Func<MethodRequestInternal, Task> onMethodCallback = null,
            Action<Twin, string, TwinCollection, IotHubException> twinMessageListener = null,
            Func<string, Message, Task> onModuleMessageReceivedCallback = null,
            Func<Message, Task> onDeviceMessageReceivedCallback = null,
            Action onUnitDisconnected = null)
            : base(deviceIdentity, amqpConnectionHolder, onMethodCallback, twinMessageListener, onModuleMessageReceivedCallback, onDeviceMessageReceivedCallback, onUnitDisconnected)
        {
        }

        public new async Task EnableReceiveMessageAsync(TimeSpan timeout)
        {
            await Task.Yield();
        }


        public new async Task EnableEventReceiveAsync(TimeSpan timeout)
        {
            await Task.Yield();
        }
    }
}
