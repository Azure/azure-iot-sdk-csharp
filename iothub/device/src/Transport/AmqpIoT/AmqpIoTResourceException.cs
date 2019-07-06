using Microsoft.Azure.Devices.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal class AmqpIoTResourceException : IotHubException
    {
        internal AmqpIoTResourceException(bool isTransient = false) : base(isTransient)
        {
        }

        internal AmqpIoTResourceException(string message, bool isTransient = false) : base(message, isTransient)
        {
        }

        internal AmqpIoTResourceException(string message, Exception cause, bool isTransient = false) : base(message, cause, isTransient)
        {
        }
    }
}
