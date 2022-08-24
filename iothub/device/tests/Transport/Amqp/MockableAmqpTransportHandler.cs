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
        public MockableAmqpTransportHandler()
            : this(
                new PipelineContext
                {
                    IotHubConnectionCredentials = new IotHubConnectionCredentials(AmqpTransportHandlerTests.TestConnectionString),
                    IotHubClientTransportSettings = new IotHubClientAmqpSettings(),
                },
                new IotHubClientAmqpSettings())
        {
        }

        internal MockableAmqpTransportHandler(
            PipelineContext context,
            IotHubClientAmqpSettings transportSettings)
            : base(
                  context,
                  transportSettings)
        {
            _amqpUnit = new MockableAmqpUnit();
        }
    }
}
