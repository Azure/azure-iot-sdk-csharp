// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.WebSockets;
using FluentAssertions;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class AmqpIotErrorAdapterTests
    {
        [TestMethod]
        public void AmqpIotErrorAdapter_ExceptionFromOutcome_Null()
        {
            Exception ex = AmqpIotErrorAdapter.GetExceptionFromOutcome(null);
            ex.Should().BeEquivalentTo(new IotHubClientException("Unknown error."));
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ExceptionFromOutcome_Rejected()
        {
            Exception ex = AmqpIotErrorAdapter.GetExceptionFromOutcome(new Rejected());
            ex.Should().BeEquivalentTo(new IotHubClientException("Unknown error."));

        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ExceptionFromOutcome_Released()
        {
            Exception ex = AmqpIotErrorAdapter.GetExceptionFromOutcome(new Released());
            ex.Should().BeEquivalentTo(new OperationCanceledException("AMQP link released."));
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_InternalError()
        {
            Error error = new ()
            {
                Condition = AmqpErrorCode.InternalError,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(new AmqpException(error));
            ex.ErrorCode.Should().Be(IotHubClientErrorCode.NetworkErrors);
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_NotFound()
        {
            Error error = new ()
            {
                Condition = AmqpErrorCode.NotFound,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(new AmqpException(error));
            ex.ErrorCode.Should().Be(IotHubClientErrorCode.DeviceNotFound);
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_UnauthorizedAccess()
        {
            Error error = new ()
            {
                Condition = AmqpErrorCode.UnauthorizedAccess,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(new AmqpException(error));
            ex.ErrorCode.Should().Be(IotHubClientErrorCode.Unauthorized);
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_ResourceLimitExceeded()
        {
            Error error = new ()
            {
                Condition = AmqpErrorCode.ResourceLimitExceeded,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(new AmqpException(error));
            ex.ErrorCode.Should().Be(IotHubClientErrorCode.QuotaExceeded);
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_PreconditionFailed()
        {
            Error error = new ()
            {
                Condition = AmqpErrorCode.PreconditionFailed,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(new AmqpException(error));
            ex.ErrorCode.Should().Be(IotHubClientErrorCode.PreconditionFailed);
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_ResourceLocked()
        {
            Error error = new ()
            {
                Condition = AmqpErrorCode.ResourceLocked,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(new AmqpException(error));
            ex.IsTransient.Should().BeTrue();
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_ConnectionForced()
        {
            Error error = new ()
            {
                Condition = AmqpErrorCode.ConnectionForced,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(new AmqpException(error));
            ex.IsTransient.Should().BeTrue();
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_FramingError()
        {
            Error error = new ()
            {
                Condition = AmqpErrorCode.FramingError,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(new AmqpException(error));
            ex.IsTransient.Should().BeTrue();
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_ConnectionRedirect()
        {
            Error error = new ()
            {
                Condition = AmqpErrorCode.ConnectionRedirect,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(new AmqpException(error));
            ex.IsTransient.Should().BeTrue();
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_WindowViolation()
        {
            Error error = new()
            {
                Condition = AmqpErrorCode.WindowViolation,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(new AmqpException(error));
            ex.IsTransient.Should().BeTrue();
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_ErrantLink()
        {
            Error error = new()
            {
                Condition = AmqpErrorCode.ErrantLink,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(new AmqpException(error));
            ex.IsTransient.Should().BeTrue();
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_HandleInUse()
        {
            Error error = new()
            {
                Condition = AmqpErrorCode.HandleInUse,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(new AmqpException(error));
            ex.IsTransient.Should().BeTrue();
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_UnattachedHandle()
        {
            Error error = new()
            {
                Condition = AmqpErrorCode.UnattachedHandle,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(new AmqpException(error));
            ex.IsTransient.Should().BeTrue();
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_DetachForced()
        {
            Error error = new()
            {
                Condition = AmqpErrorCode.DetachForced,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(new AmqpException(error));
            ex.IsTransient.Should().BeTrue();
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_TransferLimitExceeded()
        {
            Error error = new()
            {
                Condition = AmqpErrorCode.TransferLimitExceeded,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(new AmqpException(error));
            ex.IsTransient.Should().BeTrue();
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_LinkRedirect()
        {
            Error error = new()
            {
                Condition = AmqpErrorCode.LinkRedirect,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(new AmqpException(error));
            ex.IsTransient.Should().BeTrue();
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_Stolen()
        {
            Error error = new()
            {
                Condition = AmqpErrorCode.Stolen,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(new AmqpException(error));
            ex.IsTransient.Should().BeTrue();
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_MessageSizeExceeded()
        {
            Error error = new()
            {
                Condition = AmqpErrorCode.MessageSizeExceeded,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(new AmqpException(error));
            ex.ErrorCode.Should().Be(IotHubClientErrorCode.MessageTooLarge);
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_TransactionRollback()
        {
            Error error = new()
            {
                Condition = AmqpErrorCode.TransactionRollback,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(new AmqpException(error));
            ex.ErrorCode.Should().Be(IotHubClientErrorCode.NetworkErrors);
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_TransactionTimeout()
        {
            Error error = new()
            {
                Condition = AmqpErrorCode.TransactionTimeout,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(new AmqpException(error));
            ex.ErrorCode.Should().Be(IotHubClientErrorCode.NetworkErrors);
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_Error_Null()
        {
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract((Error) null);
            ex.Should().BeEquivalentTo(new IotHubClientException("Unknown error."));
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_Error_TransactionTimeout()
        {
            Error error = new()
            {
                Condition = AmqpIotConstants.Vendor + ":timeout",
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(error);
            ex.ErrorCode.Should().Be(IotHubClientErrorCode.Timeout);
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_Error_TimeoutError()
        {
            Error error = new()
            {
                Condition = AmqpErrorCode.NotFound,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(error);
            ex.ErrorCode.Should().Be(IotHubClientErrorCode.DeviceNotFound);
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_Error_MessageLockLostError()
        {
            Error error = new()
            {
                Condition = AmqpIotConstants.Vendor + ":message-lock-lost",
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(error);
            ex.ErrorCode.Should().Be(IotHubClientErrorCode.DeviceMessageLockLost);
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_Error_UnauthorizedAccess()
        {
            Error error = new()
            {
                Condition = AmqpErrorCode.UnauthorizedAccess,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(error);
            ex.ErrorCode.Should().Be(IotHubClientErrorCode.Unauthorized);
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_Error_MessageSizeExceeded()
        {
            Error error = new()
            {
                Condition = AmqpErrorCode.MessageSizeExceeded,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(error);
            ex.ErrorCode.Should().Be(IotHubClientErrorCode.MessageTooLarge);
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_Error_ResourceLimitExceeded()
        {
            Error error = new()
            {
                Condition = AmqpErrorCode.ResourceLimitExceeded,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(error);
            ex.ErrorCode.Should().Be(IotHubClientErrorCode.QuotaExceeded);
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_Error_DeviceContainerThrottled()
        {
            Error error = new()
            {
                Condition = AmqpIotConstants.Vendor + ":device-container-throttled",
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(error);
            ex.ErrorCode.Should().Be(IotHubClientErrorCode.Throttled);
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_Error_IotHubSuspended()
        {
            Error error = new()
            {
                Condition = AmqpIotConstants.Vendor + ":iot-hub-suspended",
                Info = new Fields
                {
                    { AmqpIotConstants.TrackingId, "1" }
                },
        };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(error);
            ex.ErrorCode.Should().Be(IotHubClientErrorCode.Suspended);
        }

        [TestMethod]
        public void AmqpIotExceptionAdapter_ConvertToIotHubException_InvalidOperationException()
        {
            InvalidOperationException exception = new("message");
            using var socket = new ClientWebSocket();
            using var source = new ClientWebSocketTransport(socket, null, null);
            source.SafeClose();
            Exception ex = AmqpIotExceptionAdapter.ConvertToIotHubException(exception, source);
            ex.Should().BeEquivalentTo(new IotHubClientException("AMQP resource is disconnected.", IotHubClientErrorCode.NetworkErrors, exception));
        }

        [TestMethod]
        public void AmqpIotExceptionAdapter_ConvertToIotHubException_TimeoutException()
        {
            TimeoutException exception = new("message");
            IotHubClientException ex = (IotHubClientException)AmqpIotExceptionAdapter.ConvertToIotHubException(exception, null);
            ex.ErrorCode.Should().Be(IotHubClientErrorCode.NetworkErrors);
        }

        [TestMethod]
        public void AmqpIotExceptionAdapter_ConvertToIotHubException_UnauthorizedAccessException()
        {
            UnauthorizedAccessException exception = new("message");
            IotHubClientException ex = (IotHubClientException)AmqpIotExceptionAdapter.ConvertToIotHubException(exception, null);
            ex.ErrorCode.Should().Be(IotHubClientErrorCode.Unauthorized);
        }

        [TestMethod]
        public void AmqpIotExceptionAdapter_ConvertToIotHubException_OperationCanceledException()
        {
            Error error = new()
            {
                Condition = AmqpErrorCode.MessageSizeExceeded,
            };
            OperationCanceledException exception = new("message", new AmqpException(error));
            IotHubClientException ex = (IotHubClientException)AmqpIotExceptionAdapter.ConvertToIotHubException(exception, null);
            ex.ErrorCode.Should().Be(IotHubClientErrorCode.MessageTooLarge);
        }

        [TestMethod]
        public void AmqpIotExceptionAdapter_ConvertToIotHubException_AmqpException()
        {
            Error error = new()
            {
                Condition = AmqpErrorCode.MessageSizeExceeded,
            };
            AmqpException exception = new (error);
            IotHubClientException ex = (IotHubClientException)AmqpIotExceptionAdapter.ConvertToIotHubException(exception, null);
            ex.ErrorCode.Should().Be(IotHubClientErrorCode.MessageTooLarge);
        }
    }
}
