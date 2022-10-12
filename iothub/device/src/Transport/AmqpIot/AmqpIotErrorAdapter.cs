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
                retException = new(message, IotHubClientErrorCode.NetworkErrors, amqpException);
            }
            else if (Equals(AmqpErrorCode.NotFound, amqpSymbol))
            {
                retException = new(message, IotHubClientErrorCode.DeviceNotFound, amqpException);
            }
            else if (Equals(AmqpErrorCode.UnauthorizedAccess, amqpSymbol))
            {
                retException = new(message, IotHubClientErrorCode.Unauthorized, amqpException);
            }
            else if (Equals(AmqpErrorCode.ResourceLimitExceeded, amqpSymbol))
            {
                retException = new(message, IotHubClientErrorCode.DeviceMaximumQueueDepthExceeded, amqpException);
            }
            else if (Equals(AmqpErrorCode.ResourceLocked, amqpSymbol))
            {
                retException = new(message, true, amqpException);
            }
            else if (Equals(AmqpErrorCode.PreconditionFailed, amqpSymbol))
            {
                retException = new(message, IotHubClientErrorCode.PreconditionFailed, amqpException);
            }
            // AMQP Connection Error
            else if (Equals(AmqpErrorCode.ConnectionForced, amqpSymbol))
            {
                retException = new(message, true, amqpException);
            }
            else if (Equals(AmqpErrorCode.FramingError, amqpSymbol))
            {
                retException = new(message, true, amqpException);
            }
            else if (Equals(AmqpErrorCode.ConnectionRedirect, amqpSymbol))
            {
                retException = new(message, true, amqpException);
            }
            // AMQP Session Error
            else if (Equals(AmqpErrorCode.WindowViolation, amqpSymbol))
            {
                retException = new(message, true, amqpException);
            }
            else if (Equals(AmqpErrorCode.ErrantLink, amqpSymbol))
            {
                retException = new(message, true, amqpException);
            }
            else if (Equals(AmqpErrorCode.HandleInUse, amqpSymbol))
            {
                retException = new(message, true, amqpException);
            }
            else if (Equals(AmqpErrorCode.UnattachedHandle, amqpSymbol))
            {
                retException = new(message, true, amqpException);
            }
            // AMQP Link Error
            else if (Equals(AmqpErrorCode.DetachForced, amqpSymbol))
            {
                retException = new(message, true, amqpException);
            }
            else if (Equals(AmqpErrorCode.TransferLimitExceeded, amqpSymbol))
            {
                retException = new(message, true, amqpException);
            }
            else if (Equals(AmqpErrorCode.MessageSizeExceeded, amqpSymbol))
            {
                retException = new(message, IotHubClientErrorCode.MessageTooLarge, amqpException);
            }
            else if (Equals(AmqpErrorCode.LinkRedirect, amqpSymbol))
            {
                retException = new(message, true, amqpException);
            }
            else if (Equals(AmqpErrorCode.Stolen, amqpSymbol))
            {
                retException = new(message, true, amqpException);
            }
            // AMQP Transaction Error
            else if (Equals(AmqpErrorCode.TransactionRollback, amqpSymbol))
            {
                retException = new(message, IotHubClientErrorCode.NetworkErrors, amqpException);
            }
            else if (Equals(AmqpErrorCode.TransactionTimeout, amqpSymbol))
            {
                retException = new(message, IotHubClientErrorCode.NetworkErrors, amqpException);
            }
            else
            {
                retException = new(message, false, amqpException);
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
                retException = new(message, IotHubClientErrorCode.Timeout);
            }
            else if (error.Condition.Equals(AmqpErrorCode.NotFound))
            {
                retException = new(message, IotHubClientErrorCode.DeviceNotFound);
            }
            else if (error.Condition.Equals(MessageLockLostError))
            {
                retException = new(message, IotHubClientErrorCode.DeviceMessageLockLost);
            }
            else if (error.Condition.Equals(AmqpErrorCode.UnauthorizedAccess))
            {
                retException = new(message, IotHubClientErrorCode.Unauthorized);
            }
            else if (error.Condition.Equals(AmqpErrorCode.MessageSizeExceeded))
            {
                retException = new(message, IotHubClientErrorCode.MessageTooLarge);
            }
            else if (error.Condition.Equals(AmqpErrorCode.ResourceLimitExceeded))
            {
                // Note: The DeviceMaximumQueueDepthExceededException is not supposed to be thrown here as it is being mapped to the incorrect error code
                // Error code 403004 is only applicable to C2D (Service client); see https://docs.microsoft.com/azure/iot-hub/iot-hub-troubleshoot-error-403004-devicemaximumqueuedepthexceeded
                // Error code 403002 is applicable to D2C (Device client); see https://docs.microsoft.com/azure/iot-hub/iot-hub-troubleshoot-error-403002-iothubquotaexceeded
                // We have opted not to change the exception type thrown here since it will be a breaking change, alternatively, we are adding the correct exception type
                // as the inner exception.
                retException = new(
                    $"Please check the inner exception for more information.\n " +
                    $"The correct exception type is `{IotHubClientErrorCode.QuotaExceeded}` " +
                    $"but since that is a breaking change to the current behavior in the SDK, you can refer to the inner exception " +
                    $"for more information. Exception message: {message}",
                    IotHubClientErrorCode.DeviceMaximumQueueDepthExceeded,
                    new IotHubClientException(message, IotHubClientErrorCode.QuotaExceeded));
            }
            else if (error.Condition.Equals(DeviceContainerThrottled))
            {
                retException = new(message, IotHubClientErrorCode.Throttled);
            }
            else if (error.Condition.Equals(IotHubSuspended))
            {
                retException = new("IoT hub {0} is suspended".FormatInvariant(message), IotHubClientErrorCode.Suspended);
            }
            else
            {
                retException = new(message);
            }

            if (trackingId != null)
            {
                retException.TrackingId = trackingId;
            }

            return retException;
        }
    }
}
