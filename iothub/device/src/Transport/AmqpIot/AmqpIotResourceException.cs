// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Exceptions;
using System;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal class AmqpIotResourceException : IotHubClientException
    {
        internal AmqpIotResourceException(bool isTransient = false)
            : base(isTransient)
        {
        }

        internal AmqpIotResourceException(string message, bool isTransient = false)
            : base(message, isTransient)
        {
        }

        internal AmqpIotResourceException(string message, Exception cause, bool isTransient = false)
            : base(message, cause, isTransient)
        {
        }

        internal AmqpIotResourceException()
            : base()
        {
        }

        internal AmqpIotResourceException(string message)
            : base(message)
        {
        }

        internal AmqpIotResourceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
