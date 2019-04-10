// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    using Microsoft.Azure.Amqp;
    using Microsoft.Azure.Devices.Client.Exceptions;
    using System;

    class AmqpExceptionMapper
    {
        public static Exception MapAmqpException(Exception exception)
        {
            if (exception is TimeoutException)
            {
                return new IotHubCommunicationException(exception.Message, exception);
            }
            else if (exception is UnauthorizedAccessException)
            {
                return new UnauthorizedException(exception.Message, exception);
            }
            else if (exception is AmqpException)
            {
                return AmqpErrorMapper.ToIotHubClientContract((exception as AmqpException).Error);
            }
            else
            {
                return exception;
            }
        }
    }
}
