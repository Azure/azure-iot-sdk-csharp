// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal class AmqpIotExceptionAdapter
    {
        internal static Exception ConvertToIotHubException(Exception exception)
        {
            if (exception is TimeoutException)
            {
                return new IotHubCommunicationException(exception.Message, exception);
            }
            else if (exception is UnauthorizedAccessException)
            {
                return new UnauthorizedException(exception.Message, exception);
            }
            else if (exception is OperationCanceledException
                && exception.InnerException is AmqpException innerAmqpException
                && innerAmqpException != null)
            {
                var innerAmqpException = exception.InnerException as AmqpException;
                return innerAmqpException == null
                    ? exception
                    : AmqpIotErrorAdapter.ToIotHubClientContract(innerAmqpException);
            }
            else
            {
                var amqpException = exception as AmqpException;
                return amqpException == null
                    ? exception
                    : AmqpIotErrorAdapter.ToIotHubClientContract(amqpException);
            }
        }

        internal static Exception ConvertToIotHubException(Exception exception, AmqpObject source)
        {
            Exception e = ConvertToIotHubException(exception);
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
