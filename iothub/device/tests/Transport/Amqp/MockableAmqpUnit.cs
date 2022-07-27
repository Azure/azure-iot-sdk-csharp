// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Test.ConnectionString;
using Microsoft.Azure.Devices.Client.Transport.Amqp;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;

namespace Microsoft.Azure.Devices.Client.Test.Transport
{
    internal class MockableAmqpUnit : AmqpUnit
    {
        public MockableAmqpUnit()
            : this(
                new DeviceIdentity(
                    IotHubConnectionStringExtensions.Parse(AmqpTransportHandlerTests.TestConnectionString),
                    new IotHubClientAmqpSettings(),
                    new ProductInfo(),
                    new IotHubClientOptions()),
                new AmqpConnectionHolder(
                    new DeviceIdentity(
                        IotHubConnectionStringExtensions.Parse(AmqpTransportHandlerTests.TestConnectionString),
                        new IotHubClientAmqpSettings(),
                        new ProductInfo(),
                        new IotHubClientOptions())))
        {
        }

        public MockableAmqpUnit(DeviceIdentity deviceIdentity,
            IAmqpConnectionHolder amqpConnectionHolder,
            Func<MethodRequestInternal, Task> onMethodCallback = null,
            Action<Twin, string, TwinCollection, IotHubException> twinMessageListener = null,
            Func<string, Message, Task> onModuleMessageReceivedCallback = null,
            Func<Message, Task> onDeviceMessageReceivedCallback = null,
            Action onUnitDisconnected = null)
            : base(deviceIdentity, amqpConnectionHolder, onMethodCallback, twinMessageListener, onModuleMessageReceivedCallback, onDeviceMessageReceivedCallback, onUnitDisconnected)
        {
        }

        public new async Task EnableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();
        }


        public new async Task EnableEventReceiveAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();
        }
    }
}
