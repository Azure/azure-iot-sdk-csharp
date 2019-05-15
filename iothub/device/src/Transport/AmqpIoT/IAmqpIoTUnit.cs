// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal interface IAmqpIoTUnit
    {
        event EventHandler OnUnitDisconnected;

        Task CloseAsync(TimeSpan timeout);
        Task DisableMethodsAsync(TimeSpan timeout);
        void Dispose();
        Task<AmqpIoTOutcome> DisposeMessageAsync(string lockToken, AmqpIoTDisposeActions disposeAction, TimeSpan timeout);
        Task EnableEventReceiveAsync(TimeSpan timeout);
        Task EnableMethodsAsync(TimeSpan timeout);
        Task EnsureTwinLinksAreOpenedAsync(TimeSpan timeout);
        bool IsUsable();
        void OnConnectionDisconnected();
        void OnEventsReceived(AmqpMessage amqpMessage);
        Task OpenAsync(TimeSpan timeout);
        Task<Message> ReceiveMessageAsync(TimeSpan timeout);
        Task<AmqpIoTOutcome> SendEventAsync(AmqpIoTMessage message, TimeSpan timeout);
        Task SendEventsAsync(IEnumerable<Message> messages, TimeSpan operationTimeout);
        Task<AmqpIoTOutcome> SendMessageAsync(AmqpMessage message, TimeSpan timeout);
        Task<AmqpIoTOutcome> SendMethodResponseAsync(AmqpIoTMessage amqpIoTMessage, TimeSpan timeout);
        Task<AmqpIoTOutcome> SendTwinMessageAsync(AmqpIoTMessage amqpIoTmessage, TimeSpan timeout);
        int SetNotUsable();
    }
}