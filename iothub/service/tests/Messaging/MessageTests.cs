// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Tests.Messaging
{
    [TestClass]
    [TestCategory("Unit")]
    public class MessageTests
    {
        [TestMethod]
        public void ConstructorTakingPayloadTest()
        {
            string payloadString = "Hello, World!";
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadString);
            var msg = new OutgoingMessage(payloadBytes);
            msg.Payload.Should().BeEquivalentTo(payloadBytes);
            msg.HasPayload.Should().BeTrue();
        }

        [TestMethod]
        public void Message_Ctor_TakesNullObjectTest()
        {
            var msg = new OutgoingMessage(null);
            msg.Payload.Should().BeNull();
            msg.HasPayload.Should().BeFalse();
        }

        [TestMethod]
        public void Message_EmptyCtor_PayloadIsNull()
        {
            var msg = new OutgoingMessage();
            msg.Payload.Should().BeNull();
            msg.HasPayload.Should().BeFalse();
        }

        [TestMethod]
        public void Message_Properties_DefaultNotNull()
        {
            var msg = new OutgoingMessage();
            msg.Properties.Should().NotBeNull();
        }

        [TestMethod]
        public void Message_SystemProperties_DefaultNotNull()
        {
            var msg = new OutgoingMessage();
            msg.SystemProperties.Should().NotBeNull();
        }

        [TestMethod]
        public void Message_Construct()
        {
            string payload = Guid.NewGuid().ToString();
            string messageId = Guid.NewGuid().ToString();
            string userId = Guid.NewGuid().ToString();
            string to = Guid.NewGuid().ToString();
            string correlationId = Guid.NewGuid().ToString();
            string lockToken = Guid.NewGuid().ToString();
            const string messageSchema = "default@v1";
            const string contentType = "text/plain";
            const string contentEncoding = "utf-8";
            DateTimeOffset createdOnUtc = DateTimeOffset.UtcNow;
            DateTimeOffset enqueuedOnUtc = DateTimeOffset.UtcNow;
            DateTimeOffset expiresOnUtc = DateTimeOffset.MaxValue;

            var message = new OutgoingMessage(Encoding.UTF8.GetBytes(payload))
            {
                MessageId = messageId,
                UserId = userId,
                To = to,
                ExpiresOnUtc = expiresOnUtc,
                CorrelationId = correlationId,
                LockToken = lockToken,
                MessageSchema = messageSchema,
                ContentType = contentType,
                ContentEncoding = contentEncoding,
                Ack = DeliveryAcknowledgement.PositiveOnly,
                CreatedOnUtc = createdOnUtc,
                EnqueuedOnUtc = enqueuedOnUtc
            };

            message.MessageId.Should().Be(messageId);
            message.UserId.Should().Be(userId);
            message.To.Should().Be(to);
            message.ExpiresOnUtc.Should().Be(expiresOnUtc);
            message.CorrelationId.Should().Be(correlationId);
            message.LockToken.Should().Be(lockToken);
            message.MessageSchema.Should().Be(messageSchema);
            message.ContentType.Should().Be(contentType);
            message.Ack.Should().Be(DeliveryAcknowledgement.PositiveOnly);
            message.CreatedOnUtc.Should().Be(createdOnUtc);
            message.EnqueuedOnUtc.Should().Be(enqueuedOnUtc);
        }
    }
}
