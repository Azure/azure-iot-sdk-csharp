// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal class AmqpIoTExceptionAdapter
    {
        internal static Exception ConvertToIoTHubException(Exception exception)
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
                    return AmqpIoTErrorAdapter.ToIotHubClientContract(amqpException);
                }

                return exception;
            }
        }

        internal static Exception ConvertToIoTHubException(Exception exception, AmqpObject source)
        {
            Exception e = ConvertToIoTHubException(exception);
            if (source.IsClosing() && (e is InvalidOperationException || e is OperationCanceledException))
            {
                return new IotHubCommunicationException("Amqp resource is disconnected.");
            }
            else
            {
                return e;
            }
        }
    }

}
