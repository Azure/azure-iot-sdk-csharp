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
            ex.Message.Should().Be(AmqpIotErrorAdapter.UnknownError);
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ExceptionFromOutcome_Rejected()
        {
            Exception ex = AmqpIotErrorAdapter.GetExceptionFromOutcome(new Rejected());
            ex.Message.Should().Be(AmqpIotErrorAdapter.UnknownError);
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ExceptionFromOutcome_Released()
        {
            Exception ex = AmqpIotErrorAdapter.GetExceptionFromOutcome(new Released());
            ex.Message.Should().Be(AmqpIotErrorAdapter.LinkReleased);
        }

        [TestMethod]
        [DataRow("amqp:internal-error", IotHubClientErrorCode.NetworkErrors)]
        [DataRow("amqp:not-found", IotHubClientErrorCode.DeviceNotFound)]
        [DataRow("amqp:unauthorized-access", IotHubClientErrorCode.Unauthorized)]
        [DataRow("amqp:resource-limit-exceeded", IotHubClientErrorCode.QuotaExceeded)]
        [DataRow("amqp:precondition-failed", IotHubClientErrorCode.PreconditionFailed)]
        [DataRow("amqp:link:message-size-exceeded", IotHubClientErrorCode.MessageTooLarge)]
        [DataRow("amqp:transaction:rollback", IotHubClientErrorCode.NetworkErrors)]
        [DataRow("amqp:transaction:timeout", IotHubClientErrorCode.NetworkErrors)]
        [DataRow("amqp:not-found", IotHubClientErrorCode.DeviceNotFound)]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_AmqpException(string amqpErrorCode, IotHubClientErrorCode iotHubClientErrorCode)
        {
            var error = new Error
            {
                Condition = amqpErrorCode,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(new AmqpException(error));
            ex.ErrorCode.Should().Be(iotHubClientErrorCode);
        }

        [TestMethod]
        [DataRow("amqp:resource-limit-exceeded", IotHubClientErrorCode.QuotaExceeded)]
        [DataRow("amqp:link:message-size-exceeded", IotHubClientErrorCode.MessageTooLarge)]
        [DataRow("amqp:unauthorized-access", IotHubClientErrorCode.Unauthorized)]
        [DataRow("amqp:not-found", IotHubClientErrorCode.DeviceNotFound)]
        [DataRow(AmqpIotConstants.Vendor + ":timeout", IotHubClientErrorCode.Timeout)]
        [DataRow(AmqpIotConstants.Vendor + ":message-lock-lost", IotHubClientErrorCode.DeviceMessageLockLost)]
        [DataRow(AmqpIotConstants.Vendor + ":device-container-throttled", IotHubClientErrorCode.Throttled)]
        [DataRow(AmqpIotConstants.Vendor + ":iot-hub-suspended", IotHubClientErrorCode.Suspended)]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_Error(string amqpErrorCode, IotHubClientErrorCode iotHubClientErrorCode)
        {
            var error = new Error
            {
                Condition = amqpErrorCode,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(error);
            ex.ErrorCode.Should().Be(iotHubClientErrorCode);
        }

        [TestMethod]
        [DataRow("amqp:resource-locked")]
        [DataRow("amqp:connection:forced")]
        [DataRow("amqp:connection:framing-error")]
        [DataRow("amqp:connection:redirect")]
        [DataRow("amqp:session:window-violation")]
        [DataRow("amqp:session-errant-link")]
        [DataRow("amqp:session:handle-in-use")]
        [DataRow("amqp:session:unattached-handle")]
        [DataRow("amqp:link:detach-forced")]
        [DataRow("amqp:link:transfer-limit-exceeded")]
        [DataRow("amqp:link:redirect")]
        [DataRow("amqp:link:stolen")]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_IsTransient(string amqpErrorCode)
        {
            var error = new Error
            {
                Condition = amqpErrorCode,
            };
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract(new AmqpException(error));
            ex.IsTransient.Should().BeTrue();
        }

        [TestMethod]
        public void AmqpIotErrorAdapter_ToIotHubClientContract_Error_Null()
        {
            IotHubClientException ex = AmqpIotErrorAdapter.ToIotHubClientContract((Error) null);
            ex.ErrorCode.Should().Be(IotHubClientErrorCode.Unknown);
        }

        [TestMethod]
        public void AmqpIotExceptionAdapter_ConvertToIotHubException_DisconnectedException()
        {
            InvalidOperationException exception = new("message");
            using var socket = new ClientWebSocket();
            using var source = new ClientWebSocketTransport(socket, null, null);
            source.SafeClose();
            var ex = (IotHubClientException)AmqpIotExceptionAdapter.ConvertToIotHubException(exception, source);
            ex.ErrorCode.Should().Be(IotHubClientErrorCode.NetworkErrors);
        }

        [TestMethod]
        public void AmqpIotExceptionAdapter_ConvertToIotHubException_TimeoutException()
        {
            TimeoutException exception = new("message");
            var ex = (IotHubClientException)AmqpIotExceptionAdapter.ConvertToIotHubException(exception, null);
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
            var error = new Error
            {
                Condition = AmqpErrorCode.MessageSizeExceeded,
            };
            OperationCanceledException exception = new("message", new AmqpException(error));
            var ex = (IotHubClientException)AmqpIotExceptionAdapter.ConvertToIotHubException(exception, null);
            ex.ErrorCode.Should().Be(IotHubClientErrorCode.MessageTooLarge);
        }

        [TestMethod]
        public void AmqpIotExceptionAdapter_ConvertToIotHubException_AmqpException()
        {
            var error = new Error
            {
                Condition = AmqpErrorCode.MessageSizeExceeded,
            };
            AmqpException exception = new (error);
            var ex = (IotHubClientException)AmqpIotExceptionAdapter.ConvertToIotHubException(exception, null);
            ex.ErrorCode.Should().Be(IotHubClientErrorCode.MessageTooLarge);
        }
    }
}
