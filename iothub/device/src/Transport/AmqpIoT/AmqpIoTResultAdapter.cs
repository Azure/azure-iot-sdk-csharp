// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Amqp.Framing;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal enum AmqpIoTDisposeActions
    {
        Accepted,
        Released,
        Rejected
    }

    internal static class AmqpIoTResultAdapter
    {
        internal static Outcome GetResult(AmqpIoTDisposeActions amqpIoTConstants)
        {
            switch (amqpIoTConstants)
            {
                case AmqpIoTDisposeActions.Accepted:
                    return new Accepted();
                case AmqpIoTDisposeActions.Released:
                    return new Released();
                case AmqpIoTDisposeActions.Rejected:
                    return new Rejected();
                default:
                    throw new ArgumentOutOfRangeException(nameof(amqpIoTConstants));
            }
        }
    }
}
