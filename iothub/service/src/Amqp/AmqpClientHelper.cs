// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Amqp
{
    /// <summary>
    /// Miscellaneous helpers for AMQP operations.
    /// </summary>
    internal static class AmqpClientHelper
    {
        internal delegate object ParseFunc<in T>(AmqpMessage amqpMessage, T data);

        internal static Exception ToIotHubClientContract(Exception exception)
        {
            switch (exception)
            {
                case TimeoutException:
                    return new IotHubServiceException(
                        exception.Message,
                        HttpStatusCode.RequestTimeout,
                        IotHubServiceErrorCode.Unknown);

                case UnauthorizedAccessException:
                    return new IotHubServiceException(
                        exception.Message,
                        HttpStatusCode.Unauthorized,
                        IotHubServiceErrorCode.IotHubUnauthorizedAccess);

                case AmqpException amqpException:
                    return ToIotHubClientContract(amqpException.Error, amqpException);

                default:
                    return exception;
            }
        }

        internal static void ValidateContentType(AmqpMessage amqpMessage, string expectedContentType)
        {
            string contentType = amqpMessage.Properties.ContentType.ToString();
            if (!string.Equals(contentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Unsupported content type: {contentType}.");
            }
        }

        internal static async Task<T> GetObjectFromAmqpMessageAsync<T>(AmqpMessage amqpMessage)
        {
            using var reader = new StreamReader(amqpMessage.BodyStream, Encoding.UTF8);
            string jsonString = await reader.ReadToEndAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        internal static Exception GetExceptionFromOutcome(Outcome outcome)
        {
            if (outcome == null)
            {
                return new IotHubServiceException("Unknown error.");
            }

            Exception retException;
            if (outcome.DescriptorCode == Rejected.Code)
            {
                var rejected = (Rejected)outcome;
                retException = ToIotHubClientContract(rejected.Error);
            }
            else if (outcome.DescriptorCode == Released.Code)
            {
                retException = new OperationCanceledException("AMQP link released.");
            }
            else
            {
                retException = new IotHubServiceException("Unknown error.");
            }

            return retException;
        }

        internal static IotHubServiceException ToIotHubClientContract(Error error, Exception innerException = null)
        {
            if (error == null)
            {
                return new IotHubServiceException("Unknown error.");
            }

            IotHubServiceException retException;
            string message = error.Description;
            string trackingId = null;

            if (error.Info != null
                && error.Info.TryGetValue(AmqpsConstants.TrackingId, out trackingId))
            {
                message = $"{message}\r\nTracking Id:{trackingId}";
            }

            if (error.Condition.Equals(IotHubAmqpErrorCode.TimeoutError))
            {
                retException = new(message, HttpStatusCode.RequestTimeout, IotHubServiceErrorCode.Unknown, null, innerException);
            }
            else if (error.Condition.Equals(AmqpErrorCode.NotFound))
            {
                retException = new(message, HttpStatusCode.NotFound, IotHubServiceErrorCode.DeviceNotFound, null, innerException);
            }
            else if (error.Condition.Equals(AmqpErrorCode.UnauthorizedAccess))
            {
                retException = new(message, HttpStatusCode.Unauthorized, IotHubServiceErrorCode.IotHubUnauthorizedAccess, null, innerException);
            }
            else if (error.Condition.Equals(AmqpErrorCode.MessageSizeExceeded))
            {
                retException = new(message, HttpStatusCode.RequestEntityTooLarge, IotHubServiceErrorCode.MessageTooLarge, null, innerException);
            }
            else if (error.Condition.Equals(AmqpErrorCode.ResourceLimitExceeded))
            {
                retException = new(message, HttpStatusCode.Forbidden, IotHubServiceErrorCode.DeviceMaximumQueueDepthExceeded, null, innerException);
            }
            else if (error.Condition.Equals(IotHubAmqpErrorCode.DeviceAlreadyExists))
            {
                retException = new(message, HttpStatusCode.Conflict, IotHubServiceErrorCode.DeviceAlreadyExists, null, innerException);
            }
            else if (error.Condition.Equals(IotHubAmqpErrorCode.DeviceContainerThrottled))
            {
                retException = new(message, (HttpStatusCode)429, IotHubServiceErrorCode.ThrottlingException, null, innerException);
            }
            else if (error.Condition.Equals(IotHubAmqpErrorCode.QuotaExceeded))
            {
                retException = new(message, HttpStatusCode.Forbidden, IotHubServiceErrorCode.IotHubQuotaExceeded, null, innerException);
            }
            else if (error.Condition.Equals(IotHubAmqpErrorCode.PreconditionFailed))
            {
                retException = new(message, HttpStatusCode.PreconditionFailed, IotHubServiceErrorCode.PreconditionFailed, null, innerException);
            }
            else if (error.Condition.Equals(IotHubAmqpErrorCode.IotHubSuspended))
            {
                retException = new(message, HttpStatusCode.BadRequest, IotHubServiceErrorCode.IotHubSuspended, null, innerException);
            }
            else
            {
                retException = new(message, innerException);
            }

            if (trackingId != null)
            {
                retException.TrackingId = trackingId;
            }

            return retException;
        }

        internal static ErrorContext GetErrorContextFromException(AmqpException exception)
        {
            return new ErrorContext(new IotHubServiceException(exception.Error.ToString(), exception));
        }
    }
}
