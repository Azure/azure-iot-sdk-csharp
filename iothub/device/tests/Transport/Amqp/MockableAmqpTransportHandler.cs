// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Test.ConnectionString;
using Microsoft.Azure.Devices.Client.Transport.Amqp;

namespace Microsoft.Azure.Devices.Client.Test.Transport
{
    internal class MockableAmqpTransportHandler : AmqpTransportHandler
    {
        public MockableAmqpTransportHandler() : this(new PipelineContext(),
                IotHubConnectionStringExtensions.Parse(AmqpTransportHandlerTests.TestConnectionString),
                new AmqpTransportSettings())
        {
        }

        internal MockableAmqpTransportHandler(
            PipelineContext context,
            IotHubConnectionString connectionString,
            AmqpTransportSettings transportSettings,
            Func<MethodRequestInternal, Task> onMethodCallback = null,
            Action<TwinCollection> onDesiredStatePatchReceivedCallback = null,
            Func<string, Message, Task> onModuleMessageReceivedCallback = null,
            Func<Message, Task> onDeviceMessageReceivedCallback = null)
            : base(
                  context,
                  connectionString,
                  transportSettings,
                  onMethodCallback,
                  onDesiredStatePatchReceivedCallback,
                  onModuleMessageReceivedCallback,
                  onDeviceMessageReceivedCallback)
        {
            _amqpUnit = new MockableAmqpUnit();
        }
    }
}
