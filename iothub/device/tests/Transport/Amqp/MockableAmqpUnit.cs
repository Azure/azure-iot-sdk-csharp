// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Test.ConnectionString;
using Microsoft.Azure.Devices.Client.Transport.Amqp;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;

namespace Microsoft.Azure.Devices.Client.Test.Transport
{
    internal class MockableAmqpUnit : AmqpUnit
    {
        private static IotHubClientAmqpSettings s_transportSettings = new();

        public MockableAmqpUnit()
            : this(
                  new IotHubConnectionCredentials(AmqpTransportHandlerTests.TestConnectionString),
                  new AdditionalClientInformation(),
                  s_transportSettings,
                  new AmqpConnectionHolder(new IotHubConnectionCredentials(AmqpTransportHandlerTests.TestConnectionString), s_transportSettings))
        {
        }

        public MockableAmqpUnit(
            IConnectionCredentials connectionCredentials,
            AdditionalClientInformation additionalClientInformation,
            IotHubClientAmqpSettings amqpSettings,
            IAmqpConnectionHolder amqpConnectionHolder,
            Func<DirectMethodRequest, Task<DirectMethodResponse>> onMethodCallback = null,
            Action<Twin, string, TwinCollection, IotHubClientException> twinMessageListener = null,
            Func<Message, Task<MessageAcknowledgementType>> onModuleMessageReceivedCallback = null,
            Func<Message, Task<MessageAcknowledgementType>> onDeviceMessageReceivedCallback = null,
            Action onUnitDisconnected = null)
            : base(
                  connectionCredentials,
                  additionalClientInformation,
                  amqpSettings,
                  amqpConnectionHolder,
                  onMethodCallback,
                  twinMessageListener,
                  onModuleMessageReceivedCallback,
                  onDeviceMessageReceivedCallback,
                  onUnitDisconnected)
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
