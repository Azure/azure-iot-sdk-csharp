// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Encoding;
using Microsoft.Azure.Amqp.Framing;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal static class AmqpIotErrorAdapter
    {
        public static readonly AmqpSymbol TimeoutName = AmqpIotConstants.Vendor + ":timeout";
        public static readonly AmqpSymbol StackTraceName = AmqpIotConstants.Vendor + ":stack-trace";

        // Error codes
        public static readonly AmqpSymbol DeadLetterName = AmqpIotConstants.Vendor + ":dead-letter";

        public const string DeadLetterReasonHeader = "DeadLetterReason";
        public const string DeadLetterErrorDescriptionHeader = "DeadLetterErrorDescription";
        public static readonly AmqpSymbol TimeoutError = AmqpIotConstants.Vendor + ":timeout";
        public static readonly AmqpSymbol MessageLockLostError = AmqpIotConstants.Vendor + ":message-lock-lost";
        public static readonly AmqpSymbol IotHubNotFoundError = AmqpIotConstants.Vendor + ":iot-hub-not-found-error";
        public static readonly AmqpSymbol ArgumentError = AmqpIotConstants.Vendor + ":argument-error";
        public static readonly AmqpSymbol ArgumentOutOfRangeError = AmqpIotConstants.Vendor + ":argument-out-of-range";
        public static readonly AmqpSymbol DeviceContainerThrottled = AmqpIotConstants.Vendor + ":device-container-throttled";
        public static readonly AmqpSymbol IotHubSuspended = AmqpIotConstants.Vendor + ":iot-hub-suspended";

        public static Exception GetExceptionFromOutcome(Outcome outcome)
        {
            Exception retException;
            if (outcome == null)
            {
                retException = new IotHubClientException("Unknown error.");
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
                retException = new IotHubClientException("Unknown error.");
            }

            return retException;
        }

        public static IotHubClientException ToIotHubClientContract(AmqpException amqpException)
        {
            Error error = amqpException.Error;
            AmqpSymbol amqpSymbol = error.Condition;
            string message = error.ToString();

            IotHubClientErrorCode errorCode = IotHubClientErrorCode.Unknown;
            bool isTransient = false;

            // Generic AMQP error
            if (Equals(AmqpErrorCode.InternalError, amqpSymbol))
            {
                errorCode = IotHubClientErrorCode.NetworkErrors;
                isTransient = true;
            }
            else if (Equals(AmqpErrorCode.NotFound, amqpSymbol))
            {
                errorCode = IotHubClientErrorCode.DeviceNotFound;
            }
            else if (Equals(AmqpErrorCode.UnauthorizedAccess, amqpSymbol))
            {
                errorCode = IotHubClientErrorCode.Unauthorized;
            }
            else if (Equals(AmqpErrorCode.ResourceLimitExceeded, amqpSymbol))
            {
                errorCode = IotHubClientErrorCode.QuotaExceeded;
            }
            else if (Equals(AmqpErrorCode.PreconditionFailed, amqpSymbol))
            {
                errorCode = IotHubClientErrorCode.PreconditionFailed;
            }
            else if (Equals(AmqpErrorCode.ResourceLocked, amqpSymbol))
            {
                isTransient = true;
            }
            // AMQP Connection Error
            else if (Equals(AmqpErrorCode.ConnectionForced, amqpSymbol)
                || Equals(AmqpErrorCode.FramingError, amqpSymbol)
                || Equals(AmqpErrorCode.ConnectionRedirect, amqpSymbol))
            {
                isTransient = true;
            }
            // AMQP Session Error
            else if (Equals(AmqpErrorCode.WindowViolation, amqpSymbol)
                || Equals(AmqpErrorCode.ErrantLink, amqpSymbol)
                || Equals(AmqpErrorCode.HandleInUse, amqpSymbol)
                || Equals(AmqpErrorCode.UnattachedHandle, amqpSymbol))
            {
                isTransient = true;
            }
            // AMQP Link Error
            else if (Equals(AmqpErrorCode.DetachForced, amqpSymbol)
                || Equals(AmqpErrorCode.TransferLimitExceeded, amqpSymbol)
                || Equals(AmqpErrorCode.LinkRedirect, amqpSymbol)
                || Equals(AmqpErrorCode.Stolen, amqpSymbol))
            {
                isTransient = true;
            }
            else if (Equals(AmqpErrorCode.MessageSizeExceeded, amqpSymbol))
            {
                errorCode = IotHubClientErrorCode.MessageTooLarge;
            }
            // AMQP Transaction Error
            else if (Equals(AmqpErrorCode.TransactionRollback, amqpSymbol)
                || Equals(AmqpErrorCode.TransactionTimeout, amqpSymbol))
            {
                errorCode = IotHubClientErrorCode.NetworkErrors;
                isTransient = true;
            }

            return new IotHubClientException(message, errorCode, amqpException)
            {
                IsTransient = isTransient
            };
        }

        public static IotHubClientException ToIotHubClientContract(Error error)
        {
            if (error == null)
            {
                return new IotHubClientException("Unknown error.");
            }

            string message = error.Description;

            string trackingId = null;
            if (error.Info != null
                && error.Info.TryGetValue(AmqpIotConstants.TrackingId, out trackingId))
            {
                message = $"{message}\r\nTracking Id:{trackingId}";
            }

            IotHubClientErrorCode errorCode = IotHubClientErrorCode.Unknown;

            if (error.Condition.Equals(TimeoutError))
            {
                errorCode = IotHubClientErrorCode.Timeout;
            }
            else if (error.Condition.Equals(AmqpErrorCode.NotFound))
            {
                errorCode = IotHubClientErrorCode.DeviceNotFound;
            }
            else if (error.Condition.Equals(MessageLockLostError))
            {
                errorCode = IotHubClientErrorCode.DeviceMessageLockLost;
            }
            else if (error.Condition.Equals(AmqpErrorCode.UnauthorizedAccess))
            {
                errorCode = IotHubClientErrorCode.Unauthorized;
            }
            else if (error.Condition.Equals(AmqpErrorCode.MessageSizeExceeded))
            {
                errorCode = IotHubClientErrorCode.MessageTooLarge;
            }
            else if (error.Condition.Equals(AmqpErrorCode.ResourceLimitExceeded))
            {
                errorCode = IotHubClientErrorCode.QuotaExceeded;
            }
            else if (error.Condition.Equals(DeviceContainerThrottled))
            {
                errorCode = IotHubClientErrorCode.Throttled;
            }
            else if (error.Condition.Equals(IotHubSuspended))
            {
                errorCode = IotHubClientErrorCode.Suspended;
                message = $"IoT hub {message} is suspended";
            }

            var retException = new IotHubClientException(message, errorCode);

            if (trackingId != null)
            {
                retException.TrackingId = trackingId;
            }

            return retException;
        }
    }
}
