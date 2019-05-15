// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    using System;
    using Microsoft.Azure.Devices.Client.Exceptions;
    using Microsoft.Azure.Amqp;

    internal class AmqpIoTExceptionAdapter
    {
        public static Exception ToIotHubClientContract(Exception exception)
        {
            if (exception is TimeoutException)
            {
                return new IotHubCommunicationException(exception.Message, exception);
            }
            else if (exception is UnauthorizedAccessException)
            {
                return new UnauthorizedException(exception.Message, exception);
            }
            else
            {
                var amqpException = exception as AmqpException;
                if (amqpException != null)
                {
                    return AmqpIoTErrorAdapter.ToIotHubClientContract(amqpException.Error);
                }

                return exception;
            }
        }
    }
}
