// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
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
#pragma warning disable CA2000 // disposed by base class
                new AmqpConnectionHolder(new IotHubConnectionCredentials(AmqpTransportHandlerTests.TestConnectionString), s_transportSettings))
#pragma warning restore CA2000
        {
        }

        public MockableAmqpUnit(
            IConnectionCredentials connectionCredentials,
            AdditionalClientInformation additionalClientInformation,
            IotHubClientAmqpSettings amqpSettings,
            IAmqpConnectionHolder amqpConnectionHolder,
            Func<DirectMethodRequest, Task> onMethodCallback = null,
            Action<AmqpMessage, string, IotHubClientException> twinMessageListener = null,
            Func<IncomingMessage, Task<MessageAcknowledgement>> onMessageReceivedCallback = null,
            Action onUnitDisconnected = null)
            : base(
                connectionCredentials,
                additionalClientInformation,
                amqpSettings,
                amqpConnectionHolder,
                onMethodCallback,
                twinMessageListener,
                onMessageReceivedCallback,
                onUnitDisconnected)
        {
        }

        public new static async Task EnableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();
        }
    }
}
