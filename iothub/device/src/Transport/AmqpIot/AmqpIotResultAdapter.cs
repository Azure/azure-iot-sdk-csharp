// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Amqp.Framing;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal enum AmqpIotDisposeActions
    {
        Accepted,
        Released,
        Rejected
    }

    internal static class AmqpIotResultAdapter
    {
        internal static Outcome GetResult(AmqpIotDisposeActions amqpIotConstants)
        {
            return amqpIotConstants switch
            {
                AmqpIotDisposeActions.Accepted => new Accepted(),
                AmqpIotDisposeActions.Released => new Released(),
                AmqpIotDisposeActions.Rejected => new Rejected(),
                _ => throw new ArgumentOutOfRangeException(nameof(amqpIotConstants)),
            };
        }
    }
}
