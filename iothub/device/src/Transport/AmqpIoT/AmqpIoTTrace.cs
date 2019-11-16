// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal static class AmqpIoTTrace
    {
        internal static void SetProvider(AmqpTrace amqpTrace)
        {
            AmqpTrace.Provider = amqpTrace;
        }
    }
}
