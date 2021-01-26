// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Common.Client;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    internal static class AmqpErrorMapper
    {
        private const int MaxSizeInInfoMap = 32 * 1024;

        public static Tuple<string, string, string> GenerateError(Exception ex)
        {
            if (ex is DeviceNotFoundException deviceNotFoundException)
            {
                return Tuple.Create(AmqpErrorCode.NotFound.ToString(), deviceNotFoundException.Message, deviceNotFoundException.TrackingId);
            }

            if (ex is DeviceAlreadyExistsException deviceAlreadyExistsException)
            {
                return Tuple.Create(IotHubAmqpErrorCode.DeviceAlreadyExists.ToString(), deviceAlreadyExistsException.Message, deviceAlreadyExistsException.TrackingId);
            }

            if (ex is IotHubThrottledException deviceContainerThrottledException)
            {
                return Tuple.Create(IotHubAmqpErrorCode.DeviceContainerThrottled.ToString(), deviceContainerThrottledException.Message, deviceContainerThrottledException.TrackingId);
            }

            if (ex is QuotaExceededException quotaExceededException)
            {
                return Tuple.Create(IotHubAmqpErrorCode.QuotaExceeded.ToString(), quotaExceededException.Message, quotaExceededException.TrackingId);
            }

            if (ex is DeviceMessageLockLostException messageLockLostException)
            {
                return Tuple.Create(IotHubAmqpErrorCode.MessageLockLostError.ToString(), messageLockLostException.Message, messageLockLostException.TrackingId);
            }

            if (ex is MessageTooLargeException deviceMessageTooLargeException)
            {
                return Tuple.Create(AmqpErrorCode.MessageSizeExceeded.ToString(), deviceMessageTooLargeException.Message, deviceMessageTooLargeException.TrackingId);
            }

            if (ex is DeviceMaximumQueueDepthExceededException queueDepthExceededException)
            {
                return Tuple.Create(AmqpErrorCode.ResourceLimitExceeded.ToString(), queueDepthExceededException.Message, queueDepthExceededException.TrackingId);
            }

            if (ex is PreconditionFailedException preconditionFailedException)
            {
                return Tuple.Create(IotHubAmqpErrorCode.PreconditionFailed.ToString(), preconditionFailedException.Message, preconditionFailedException.TrackingId);
            }

            if (ex is IotHubSuspendedException iotHubSuspendedException)
            {
                return Tuple.Create(IotHubAmqpErrorCode.IotHubSuspended.ToString(), iotHubSuspendedException.Message, iotHubSuspendedException.TrackingId);
            }

            return Tuple.Create(AmqpErrorCode.InternalError.ToString(), ex.ToStringSlim(), (string)null);
        }

        public static AmqpException ToAmqpException(Exception exception)
        {
            return ToAmqpException(exception, false);
        }

        public static AmqpException ToAmqpException(Exception exception, bool includeStackTrace)
        {
            Error amqpError = ToAmqpError(exception, includeStackTrace);
            return new AmqpException(amqpError);
        }

        public static Error ToAmqpError(Exception exception)
        {
            return ToAmqpError(exception, false);
        }

        public static Error ToAmqpError(Exception exception, bool includeStackTrace)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            var error = new Error
            {
                Description = exception.Message
            };

            if (exception is AmqpException)
            {
                var amqpException = (AmqpException)exception;
                error.Condition = amqpException.Error.Condition;
                error.Info = amqpException.Error.Info;
            }
            else if (exception is UnauthorizedAccessException || exception is UnauthorizedException)
            {
                error.Condition = AmqpErrorCode.UnauthorizedAccess;
            }
            else if (exception is NotSupportedException)
            {
                error.Condition = AmqpErrorCode.NotImplemented;
            }
            else if (exception is DeviceNotFoundException)
            {
                error.Condition = AmqpErrorCode.NotFound;
            }
            else if (exception is IotHubNotFoundException)
            {
                error.Condition = IotHubAmqpErrorCode.IotHubNotFoundError;
            }
            else if (exception is DeviceMessageLockLostException)
            {
                error.Condition = IotHubAmqpErrorCode.MessageLockLostError;
            }
            else if (exception is MessageTooLargeException)
            {
                error.Condition = AmqpErrorCode.MessageSizeExceeded;
            }
            else if (exception is DeviceMaximumQueueDepthExceededException)
            {
                error.Condition = AmqpErrorCode.ResourceLimitExceeded;
            }
            else if (exception is TimeoutException)
            {
                error.Condition = IotHubAmqpErrorCode.TimeoutError;
            }
            else if (exception is InvalidOperationException)
            {
                error.Condition = AmqpErrorCode.NotAllowed;
            }
            else if (exception is ArgumentOutOfRangeException)
            {
                error.Condition = IotHubAmqpErrorCode.ArgumentOutOfRangeError;
            }
            else if (exception is ArgumentException)
            {
                error.Condition = IotHubAmqpErrorCode.ArgumentError;
            }
            else if (exception is PreconditionFailedException)
            {
                error.Condition = IotHubAmqpErrorCode.PreconditionFailed;
            }
            else if (exception is IotHubSuspendedException)
            {
                error.Condition = IotHubAmqpErrorCode.IotHubSuspended;
            }
            else if (exception is QuotaExceededException)
            {
                error.Condition = IotHubAmqpErrorCode.QuotaExceeded;
            }
            else if (exception is TimeoutException)
            {
                error.Condition = IotHubAmqpErrorCode.TimeoutError;
            }
            else
            {
                error.Condition = AmqpErrorCode.InternalError;
                error.Description = error.Description;
            }
            // we will always need this to add trackingId
            if (error.Info == null)
            {
                error.Info = new Fields();
            }

            string stackTrace;
            if (includeStackTrace && !string.IsNullOrEmpty(stackTrace = exception.StackTrace))
            {
                if (stackTrace.Length > MaxSizeInInfoMap)
                {
                    stackTrace = stackTrace.Substring(0, MaxSizeInInfoMap);
                }

                // error.Info came from AmqpException then it contains StackTraceName already.
                if (!error.Info.TryGetValue(IotHubAmqpProperty.StackTraceName, out string _))
                {
                    error.Info.Add(IotHubAmqpProperty.StackTraceName, stackTrace);
                }
            }

            error.Info.TryGetValue(IotHubAmqpProperty.TrackingId, out string trackingId);
#pragma warning disable CS0618 // Type or member is obsolete only for external dependency.
            trackingId = TrackingHelper.CheckAndAddGatewayIdToTrackingId(trackingId);
#pragma warning restore CS0618 // Type or member is obsolete only for external dependency.
            error.Info[IotHubAmqpProperty.TrackingId] = trackingId;

            return error;
        }

        public static Exception GetExceptionFromOutcome(Outcome outcome)
        {
            Exception retException;
            if (outcome == null)
            {
                retException = new IotHubException("Unknown error.");
                return retException;
            }

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
                retException = new IotHubException("Unknown error.");
            }

            return retException;
        }

        public static Exception ToIotHubClientContract(Error error)
        {
            Exception retException;
            if (error == null)
            {
                retException = new IotHubException("Unknown error.");
                return retException;
            }

            string message = error.Description;

            string trackingId = null;
            if (error.Info != null && error.Info.TryGetValue(IotHubAmqpProperty.TrackingId, out trackingId))
            {
                message = "{0}{1}{2}".FormatInvariant(message, Environment.NewLine, "Tracking Id:" + trackingId);
            }

            if (error.Condition.Equals(IotHubAmqpErrorCode.TimeoutError))
            {
                retException = new TimeoutException(message);
            }
            else if (error.Condition.Equals(AmqpErrorCode.NotFound))
            {
                retException = new DeviceNotFoundException(message, innerException: null);
            }
            else if (error.Condition.Equals(AmqpErrorCode.NotImplemented))
            {
                retException = new NotSupportedException(message);
            }
            else if (error.Condition.Equals(IotHubAmqpErrorCode.MessageLockLostError))
            {
                retException = new DeviceMessageLockLostException(message);
            }
            else if (error.Condition.Equals(AmqpErrorCode.NotAllowed))
            {
                retException = new InvalidOperationException(message);
            }
            else if (error.Condition.Equals(AmqpErrorCode.UnauthorizedAccess))
            {
                retException = new UnauthorizedException(message);
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
                retException = new DeviceMaximumQueueDepthExceededException(message);
            }
            else if (error.Condition.Equals(IotHubAmqpErrorCode.DeviceAlreadyExists))
            {
                retException = new DeviceAlreadyExistsException(message, (Exception)null);
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
                retException = new IotHubSuspendedException(message);
            }
            else
            {
                retException = new IotHubException(message);
            }

            if (trackingId != null && retException is IotHubException exception)
            {
                IotHubException iotHubException = exception;
                iotHubException.TrackingId = trackingId;
            }
            return retException;
        }
    }
}
