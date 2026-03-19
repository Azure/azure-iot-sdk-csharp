// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information

using Microsoft.Azure.Devices.Client.Transport;
using Moq;

namespace Microsoft.Azure.Devices.Client.Tests.Transport
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
                new Mock<IDelegatingHandler>().Object)
        {
        }

        internal MockableAmqpTransportHandler(PipelineContext context, IDelegatingHandler nextHandler)
            : base(context, nextHandler)
        {
            _amqpUnit = new MockableAmqpUnit();
        }
    }
}
