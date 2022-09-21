// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal class AmqpIotExceptionAdapter
    {
        internal static Exception ConvertToIotHubException(Exception exception, AmqpObject source)
        {
            if (source != null 
                && source.IsClosing() 
                && exception is InvalidOperationException)
            {
                return new IotHubClientException("AMQP resource is disconnected.", trackingId: null, IotHubStatusCode.NetworkErrors, exception);
            }

            if (exception is TimeoutException)
            {
                return new IotHubClientException(exception.Message, IotHubStatusCode.NetworkErrors, exception);
            }

            if (exception is UnauthorizedAccessException)
            {
                return new IotHubClientException(exception.Message, IotHubStatusCode.Unauthorized, exception);
            }

            if (exception is OperationCanceledException
                && exception.InnerException is AmqpException innerAmqpException
                && innerAmqpException != null)
            {
                return AmqpIotErrorAdapter.ToIotHubClientContract(innerAmqpException);
            }

            if (exception is AmqpException amqpException
                && amqpException != null)
            {
                return AmqpIotErrorAdapter.ToIotHubClientContract(amqpException);
            }

            return exception;
        }
    }
}
