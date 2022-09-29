// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal interface IAmqpUnitManager
    {
        AmqpUnit CreateAmqpUnit(
            IConnectionCredentials connectionCredentials,
            AdditionalClientInformation additionalClientInformation,
            IotHubClientAmqpSettings amqpSettings,
            Func<DirectMethodRequest, Task> onMethodCallback,
            Action<Twin, string, TwinCollection, IotHubClientException> twinMessageListener,
            Func<IncomingMessage, Task<MessageAcknowledgement>> onMessageReceivedCallback,
            Action onUnitDisconnected);

        void RemoveAmqpUnit(AmqpUnit amqpUnit);
    }
}
