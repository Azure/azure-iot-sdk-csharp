using Microsoft.Azure.Amqp;
using System;
using System.Collections.Generic;
using System.Text;

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
