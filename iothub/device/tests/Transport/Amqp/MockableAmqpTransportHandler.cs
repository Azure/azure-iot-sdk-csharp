﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information

using Microsoft.Azure.Devices.Client.Transport;

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
                })
        {
        }

        internal MockableAmqpTransportHandler(PipelineContext context)
            : base(context)
        {
            _amqpUnit = new MockableAmqpUnit();
        }
    }
}
