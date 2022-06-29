// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information

using System.Threading.Tasks;
using System;
using Microsoft.Azure.Devices.Client.Transport.Amqp;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Client.Tests.ConnectionString;
using Moq;

namespace Microsoft.Azure.Devices.Client.Tests.Transport
{
    internal class MoqableAmqpTransportHandler : AmqpTransportHandler
    {
        public MoqableAmqpTransportHandler() : this(new PipelineContext(),
                IotHubConnectionStringExtensions.Parse(AmqpTransportHandlerTests.TestConnectionString),
                new AmqpTransportSettings(TransportType.Amqp_Tcp_Only))
        {
        }

        internal MoqableAmqpTransportHandler(
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
            _amqpUnit = new MoqableAmqpUnit();
        }
    }
}
