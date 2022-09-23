﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal class AmqpIotExceptionAdapter
    {
        internal static Exception ConvertToIotHubException(Exception exception)
        {
            if (exception is TimeoutException)
            {
                return new IotHubClientException(exception.Message, exception, true, IotHubStatusCode.NetworkErrors);
            }

            if (exception is UnauthorizedAccessException)
            {
                return new IotHubClientException(exception.Message, exception, false, IotHubStatusCode.Unauthorized);
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

        internal static Exception ConvertToIotHubException(Exception exception, AmqpObject source)
        {
            Exception ex = ConvertToIotHubException(exception);
            if (source.IsClosing() &&
                (ex is InvalidOperationException
                || ex is OperationCanceledException))
            {
                return new IotHubClientException("Amqp resource is disconnected.", null, true, IotHubStatusCode.NetworkErrors);
            }
            else
            {
                return ex;
            }
        }
    }
}
