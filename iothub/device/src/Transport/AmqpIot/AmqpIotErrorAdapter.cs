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

            IotHubClientException retException;

            // Generic AMQP error
            if (Equals(AmqpErrorCode.InternalError, amqpSymbol))
            {
                retException = new IotHubClientException(message, IotHubClientErrorCode.NetworkErrors, amqpException);
            }
            else if (Equals(AmqpErrorCode.NotFound, amqpSymbol))
            {
                retException = new IotHubClientException(message, IotHubClientErrorCode.DeviceNotFound, amqpException);
            }
            else if (Equals(AmqpErrorCode.UnauthorizedAccess, amqpSymbol))
            {
                retException = new IotHubClientException(message, IotHubClientErrorCode.Unauthorized, amqpException);
            }
            else if (Equals(AmqpErrorCode.ResourceLimitExceeded, amqpSymbol))
            {
                retException = new IotHubClientException(message, IotHubClientErrorCode.DeviceMaximumQueueDepthExceeded, amqpException);
            }
            else if (Equals(AmqpErrorCode.ResourceLocked, amqpSymbol))
            {
                retException = new IotHubClientException(message, true, amqpException);
            }
            else if (Equals(AmqpErrorCode.PreconditionFailed, amqpSymbol))
            {
                retException = new IotHubClientException(message, IotHubClientErrorCode.PreconditionFailed, amqpException);
            }
            // AMQP Connection Error
            else if (Equals(AmqpErrorCode.ConnectionForced, amqpSymbol))
            {
                retException = new IotHubClientException(message, true, amqpException);
            }
            else if (Equals(AmqpErrorCode.FramingError, amqpSymbol))
            {
                retException = new IotHubClientException(message, true, amqpException);
            }
            else if (Equals(AmqpErrorCode.ConnectionRedirect, amqpSymbol))
            {
                retException = new IotHubClientException(message, true, amqpException);
            }
            // AMQP Session Error
            else if (Equals(AmqpErrorCode.WindowViolation, amqpSymbol))
            {
                retException = new IotHubClientException(message, true, amqpException);
            }
            else if (Equals(AmqpErrorCode.ErrantLink, amqpSymbol))
            {
                retException = new IotHubClientException(message, true, amqpException);
            }
            else if (Equals(AmqpErrorCode.HandleInUse, amqpSymbol))
            {
                retException = new IotHubClientException(message, true, amqpException);
            }
            else if (Equals(AmqpErrorCode.UnattachedHandle, amqpSymbol))
            {
                retException = new IotHubClientException(message, true, amqpException);
            }
            // AMQP Link Error
            else if (Equals(AmqpErrorCode.DetachForced, amqpSymbol))
            {
                retException = new IotHubClientException(message, true, amqpException);
            }
            else if (Equals(AmqpErrorCode.TransferLimitExceeded, amqpSymbol))
            {
                retException = new IotHubClientException(message, true, amqpException);
            }
            else if (Equals(AmqpErrorCode.MessageSizeExceeded, amqpSymbol))
            {
                retException = new IotHubClientException(message, IotHubClientErrorCode.MessageTooLarge, amqpException);
            }
            else if (Equals(AmqpErrorCode.LinkRedirect, amqpSymbol))
            {
                retException = new IotHubClientException(message, true, amqpException);
            }
            else if (Equals(AmqpErrorCode.Stolen, amqpSymbol))
            {
                retException = new IotHubClientException(message, true, amqpException);
            }
            // AMQP Transaction Error
            else if (Equals(AmqpErrorCode.TransactionRollback, amqpSymbol))
            {
                retException = new IotHubClientException(message, IotHubClientErrorCode.NetworkErrors, amqpException);
            }
            else if (Equals(AmqpErrorCode.TransactionTimeout, amqpSymbol))
            {
                retException = new IotHubClientException(message, IotHubClientErrorCode.NetworkErrors, amqpException);
            }
            else
            {
                retException = new IotHubClientException(message, false, amqpException);
            }

            return retException;
        }

        public static IotHubClientException ToIotHubClientContract(Error error)
        {
            IotHubClientException retException;
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

            if (error.Condition.Equals(TimeoutError))
            {
                retException = new IotHubClientException(message, IotHubClientErrorCode.Timeout);
            }
            else if (error.Condition.Equals(AmqpErrorCode.NotFound))
            {
                retException = new IotHubClientException(message, IotHubClientErrorCode.DeviceNotFound);
            }
            else if (error.Condition.Equals(MessageLockLostError))
            {
                retException = new IotHubClientException(message, IotHubClientErrorCode.DeviceMessageLockLost);
            }
            else if (error.Condition.Equals(AmqpErrorCode.UnauthorizedAccess))
            {
                retException = new IotHubClientException(message, IotHubClientErrorCode.Unauthorized);
            }
            else if (error.Condition.Equals(AmqpErrorCode.MessageSizeExceeded))
            {
                retException = new IotHubClientException(message, IotHubClientErrorCode.MessageTooLarge);
            }
            else if (error.Condition.Equals(AmqpErrorCode.ResourceLimitExceeded))
            {
                retException = new IotHubClientException(message, IotHubClientErrorCode.QuotaExceeded);
            }
            else if (error.Condition.Equals(DeviceContainerThrottled))
            {
                retException = new IotHubClientException(message, IotHubClientErrorCode.Throttled);
            }
            else if (error.Condition.Equals(IotHubSuspended))
            {
                retException = new IotHubClientException("IoT hub {0} is suspended".FormatInvariant(message), IotHubClientErrorCode.Suspended);
            }
            else
            {
                retException = new IotHubClientException(message);
            }

            if (trackingId != null)
            {
                retException.TrackingId = trackingId;
            }

            return retException;
        }
    }
}
