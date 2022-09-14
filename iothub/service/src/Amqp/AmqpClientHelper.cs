// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Common.Exceptions;
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
                        IotHubErrorCode.Unknown,
                        true);

                case UnauthorizedAccessException:
                    return new IotHubServiceException(
                        exception.Message,
                        HttpStatusCode.Unauthorized,
                        IotHubErrorCode.IotHubUnauthorizedAccess,
                        false);

                case AmqpException amqpException:
                    Exception ex = ToIotHubClientContract(amqpException.Error);
                    if (ex is IotHubServiceException hubEx)
                    {
                        // pass amqpException as the inner exception of IotHubServiceException
                        return new IotHubServiceException(
                            hubEx.Message,
                            hubEx.StatusCode,
                            hubEx.ErrorCode,
                            hubEx.IsTransient,
                            string.Empty,
                            amqpException);
                    }
                    return ex;

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

        internal static Exception ToIotHubClientContract(Error error)
        {
            if (error == null)
            {
                return new IotHubServiceException("Unknown error.");
            }

            Exception retException;
            string message = error.Description;
            string trackingId = null;

            if (error.Info != null
                && error.Info.TryGetValue(AmqpsConstants.TrackingId, out trackingId))
            {
                message = $"{message}\r\nTracking Id:{trackingId}";
            }

            if (error.Condition.Equals(IotHubAmqpErrorCode.TimeoutError))
            {
                retException = new TimeoutException(message);
            }
            else if (error.Condition.Equals(AmqpErrorCode.NotFound))
            {
                retException = new IotHubServiceException(message, HttpStatusCode.NotFound, IotHubErrorCode.DeviceNotFound, false, innerException: null);
            }
            else if (error.Condition.Equals(AmqpErrorCode.NotImplemented))
            {
                retException = new NotSupportedException(message);
            }
            else if (error.Condition.Equals(AmqpErrorCode.NotAllowed))
            {
                retException = new InvalidOperationException(message);
            }
            else if (error.Condition.Equals(AmqpErrorCode.UnauthorizedAccess))
            {
                retException = new IotHubServiceException(message, HttpStatusCode.Unauthorized, IotHubErrorCode.IotHubUnauthorizedAccess, false);
            }
            else if (error.Condition.Equals(IotHubAmqpErrorCode.ArgumentError))
            {
                retException = new ArgumentException(message);
            }
            else if (error.Condition.Equals(IotHubAmqpErrorCode.ArgumentOutOfRangeError))
            {
                retException = new ArgumentOutOfRangeException(message);
            }
            else if (error.Condition.Equals(AmqpErrorCode.MessageSizeExceeded))
            {
                retException = new IotHubServiceException(message, HttpStatusCode.RequestEntityTooLarge, IotHubErrorCode.MessageTooLarge, false);
            }
            else if (error.Condition.Equals(AmqpErrorCode.ResourceLimitExceeded))
            {
                retException = new IotHubServiceException(message, HttpStatusCode.Forbidden, IotHubErrorCode.DeviceMaximumQueueDepthExceeded, false);
            }
            else if (error.Condition.Equals(IotHubAmqpErrorCode.DeviceAlreadyExists))
            {
                retException = new IotHubServiceException(message, HttpStatusCode.Conflict, IotHubErrorCode.DeviceAlreadyExists, false);
            }
            else if (error.Condition.Equals(IotHubAmqpErrorCode.DeviceContainerThrottled))
            {
                retException = new IotHubServiceException(message, (HttpStatusCode)429, IotHubErrorCode.ThrottlingException, true);
            }
            else if (error.Condition.Equals(IotHubAmqpErrorCode.QuotaExceeded))
            {
                retException = new IotHubServiceException(message, HttpStatusCode.Forbidden, IotHubErrorCode.IotHubQuotaExceeded, true);
            }
            else if (error.Condition.Equals(IotHubAmqpErrorCode.PreconditionFailed))
            {
                retException = new IotHubServiceException(message, HttpStatusCode.PreconditionFailed, IotHubErrorCode.PreconditionFailed, false);
            }
            else if (error.Condition.Equals(IotHubAmqpErrorCode.IotHubSuspended))
            {
                retException = new IotHubServiceException(message, HttpStatusCode.BadRequest, IotHubErrorCode.IotHubSuspended, false);
            }
            else
            {
                retException = new IotHubServiceException(message);
            }

            if (trackingId != null
                && retException is IotHubServiceException exHub)
            {
                exHub.TrackingId = trackingId;
            }

            return retException;
        }

        internal static ErrorContext GetErrorContextFromException(AmqpException exception)
        {
            return new ErrorContext(new IotHubServiceException(exception.Error.ToString(), exception));
        }
    }
}
