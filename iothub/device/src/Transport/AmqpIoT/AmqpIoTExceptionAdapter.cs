// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Shared;
using System.Security.Authentication;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal class AmqpIoTExceptionAdapter
    {
        public static bool HandleAmqpException(Exception ex, string newMessage)
        {
            if (ex is ObjectDisposedException ||
                ex is InvalidOperationException ||
                ex is TimeoutException)
            {
                throw new IotHubCommunicationException(newMessage, ex);
            }

            if (ex is AmqpException)
            {
                var amqpEx = (AmqpException)ex;
                if (amqpEx.Error.Condition.Equals(AmqpErrorCode.HandleInUse))
                {
                    if (Logging.IsEnabled) Logging.Fail(ex, $"AMQP protocol exception: {nameof(AmqpErrorCode.HandleInUse)}");
                    throw new IotHubException(newMessage, ex);
                }
                else if (amqpEx.Error.Condition.Equals(AmqpErrorCode.UnattachedHandle))
                {
                    if (Logging.IsEnabled) Logging.Fail(ex, $"AMQP protocol exception: {nameof(AmqpErrorCode.UnattachedHandle)}");
                    throw new IotHubException(newMessage, ex);
                }
                else if (amqpEx.Error.Condition.Equals(AmqpErrorCode.UnauthorizedAccess))
                {
                    throw new AuthenticationException(newMessage, ex);
                }
                else if (amqpEx.Error.Condition.Equals(AmqpErrorCode.MessageSizeExceeded))
                {
                    throw new MessageTooLargeException(newMessage, ex);
                }
                else
                {
                    throw new IotHubCommunicationException(newMessage, ex);
                }
            }

            return false;
        }

        public static Exception ConvertToIoTHubException(Exception exception)
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
