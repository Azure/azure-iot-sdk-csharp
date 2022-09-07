// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Encoding;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Common.Exceptions;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Amqp
{
    internal class AmqpClientHelper
    {
        public delegate object ParseFunc<in T>(AmqpMessage amqpMessage, T data);

        public static Exception ToIotHubClientContract(Exception exception)
        {
            if (exception is TimeoutException)
            {
                return new IotHubServiceException(IotHubStatusCode.NetworkErrors, exception.Message);
            }
            else if (exception is UnauthorizedAccessException)
            {
                return new IotHubServiceException(IotHubStatusCode.IotHubUnauthorizedAccess, exception.Message);
            }
            else
            {
                if (exception is AmqpException amqpException)
                {
                    return ToIotHubClientContract(amqpException.Error);
                }

                return exception;
            }
        }

        public static void ValidateContentType(AmqpMessage amqpMessage, string expectedContentType)
        {
            string contentType = amqpMessage.Properties.ContentType.ToString();
            if (!string.Equals(contentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Unsupported content type: {contentType}.");
            }
        }

        public static async Task<T> GetObjectFromAmqpMessageAsync<T>(AmqpMessage amqpMessage)
        {
            using var reader = new StreamReader(amqpMessage.BodyStream, Encoding.UTF8);
            string jsonString = await reader.ReadToEndAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        public static Exception GetExceptionFromOutcome(Outcome outcome)
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

        public static Exception ToIotHubClientContract(Error error)
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
                retException = new IotHubServiceException(IotHubStatusCode.DeviceNotFound, message, innerException: null);
            }
            else if (error.Condition.Equals(AmqpErrorCode.NotImplemented))
            {
                retException = new NotSupportedException(message);
            }
            else if (error.Condition.Equals(IotHubAmqpErrorCode.MessageLockLostError))
            {
                retException = new IotHubServiceException(IotHubStatusCode.DeviceMessageLockLost, message);
            }
            else if (error.Condition.Equals(AmqpErrorCode.NotAllowed))
            {
                retException = new InvalidOperationException(message);
            }
            else if (error.Condition.Equals(AmqpErrorCode.UnauthorizedAccess))
            {
                retException = new IotHubServiceException(IotHubStatusCode.IotHubUnauthorizedAccess, message);
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
                retException = new MessageTooLargeException(message);
            }
            else if (error.Condition.Equals(AmqpErrorCode.ResourceLimitExceeded))
            {
                retException = new IotHubServiceException(IotHubStatusCode.DeviceMaximumQueueDepthExceeded, message);
            }
            else if (error.Condition.Equals(IotHubAmqpErrorCode.DeviceAlreadyExists))
            {
                retException = new IotHubServiceException(IotHubStatusCode.DeviceAlreadyExists, message, (Exception)null);
            }
            else if (error.Condition.Equals(IotHubAmqpErrorCode.DeviceContainerThrottled))
            {
                retException = new IotHubThrottledException(message, (Exception)null);
            }
            else if (error.Condition.Equals(IotHubAmqpErrorCode.QuotaExceeded))
            {
                retException = new QuotaExceededException(message, (Exception)null);
            }
            else if (error.Condition.Equals(IotHubAmqpErrorCode.PreconditionFailed))
            {
                retException = new PreconditionFailedException(message, (Exception)null);
            }
            else if (error.Condition.Equals(IotHubAmqpErrorCode.IotHubSuspended))
            {
                retException = new IotHubServiceException(IotHubStatusCode.HubSuspended, message);
            }
            else
            {
                retException = new IotHubServiceException(message);
            }

            if (trackingId != null
                && retException is IotHubServiceException exHub)
            {
                IotHubServiceException iotHubException = exHub;
                iotHubException.TrackingId = trackingId;
                // This is created but not assigned to `retException`. If we change that now, it might be a
                // breaking change. If not for v1, consider for #v2.
            }

            return retException;
        }

        public static ErrorContext GetErrorContextFromException(AmqpException exception)
        {
            Error error = exception.Error;
            AmqpSymbol amqpSymbol = error.Condition;
            string message = error.ToString();
            if (AmqpErrorCode.ConnectionForced.Equals(amqpSymbol)
                || AmqpErrorCode.ConnectionRedirect.Equals(amqpSymbol)
                || AmqpErrorCode.LinkRedirect.Equals(amqpSymbol)
                || AmqpErrorCode.WindowViolation.Equals(amqpSymbol)
                || AmqpErrorCode.ErrantLink.Equals(amqpSymbol)
                || AmqpErrorCode.HandleInUse.Equals(amqpSymbol)
                || AmqpErrorCode.UnattachedHandle.Equals(amqpSymbol)
                || AmqpErrorCode.DetachForced.Equals(amqpSymbol)
                || AmqpErrorCode.TransferLimitExceeded.Equals(amqpSymbol)
                || AmqpErrorCode.MessageSizeExceeded.Equals(amqpSymbol)
                || AmqpErrorCode.LinkRedirect.Equals(amqpSymbol)
                || AmqpErrorCode.Stolen.Equals(amqpSymbol))
            {
                return new ErrorContext(new IotHubServiceException(message, exception));
            }

            return new ErrorContext(new IOException(message, exception));
        }
    }
}
