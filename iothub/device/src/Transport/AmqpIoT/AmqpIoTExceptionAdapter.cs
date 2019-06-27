// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Shared;
using System.Security.Authentication;
using System.Net.Sockets;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal class AmqpIoTExceptionAdapter
    {
        public static bool HandleAmqpException(Exception ex, string newMessage)
        {
            // Exceptions that are thrown by the Amqp library to signal state changes. E.g. a pending operation 
            // is terminated because the link/session is closing or a new operation is started on a link/session
            // that is in an invalid state (not yet opened or already closed); operation timed out.
            if (ex is ObjectDisposedException ||
                ex is InvalidOperationException ||
                ex is TimeoutException || 
                ex is OperationCanceledException)
            {
                // Recommend retry.
                throw new IotHubCommunicationException(newMessage, ex);
            }

            // For backward compatibility - we will handle this in the upper layers.
            // Thrown by SASL authentication.
            if (ex is UnauthorizedAccessException)
            {
                return false;
            }

            // Thrown by AMQP's TcpTransport.
            if (ex is SocketException)
            {
                // Recommend retry.
                throw new IotHubCommunicationException(newMessage, ex);
            }

            // Exceptions that are thrown by the Amqp library for protocol-level faults.
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
                    // Recommend retry.
                    throw new IotHubCommunicationException(newMessage, ex);
                }
            }


            // AMQP Library exceptions that we will not handle:
            // - Argument*Exception
            // - NotSupportedException: unknown protocol extensions
            // - CallbackException
            // - SerializationException
            // - AssertionFailedException / FatalException
            // - 
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
