﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
                return new IotHubClientException("AMQP resource is disconnected.", exception)
                {
                    ErrorCode = IotHubClientErrorCode.NetworkErrors,
                };
            }

            if (exception is TimeoutException)
            {
                return new IotHubClientException(exception.Message, exception)
                {
                    ErrorCode = IotHubClientErrorCode.NetworkErrors,
                };
            }

            if (exception is UnauthorizedAccessException)
            {
                return new IotHubClientException(exception.Message, exception)
                {
                    ErrorCode = IotHubClientErrorCode.Unauthorized,
                };
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
