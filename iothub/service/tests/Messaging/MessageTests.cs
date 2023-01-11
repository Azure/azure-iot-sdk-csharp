// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

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
            var msg = new Message(payloadBytes);
            msg.Payload.Should().BeEquivalentTo(payloadBytes);
            msg.HasPayload.Should().BeTrue();
        }

        [TestMethod]
        public void ConstructorTakingEmptyByteArrayTest()
        {
            var msg = new Message(Array.Empty<byte>());
            msg.Payload.Should().NotBeNull();
            msg.Payload.Length.Should().Be(0);
        }

        [TestMethod]
        public void Message_DefaultPayload()
        {
            var msg = new Message();
            msg.Payload.Should().NotBeNull();
            msg.Payload.Should().BeEquivalentTo(Array.Empty<byte>());
            msg.Payload.Length.Should().Be(0);
            msg.HasPayload.Should().BeFalse();
        }

        [TestMethod]
        public void Message_PropertiesNotNull()
        {
            var msg = new Message();
            msg.Properties.Should().NotBeNull();
        }

        [TestMethod]
        public void Message_SystemPropertiesNotNull()
        {
            var msg = new Message();
            msg.SystemProperties.Should().NotBeNull();
        }

        [TestMethod]
        public void Message_Construct()
        {
            string payload = Guid.NewGuid().ToString();
            string messageId = Guid.NewGuid().ToString();
            string p1Value = Guid.NewGuid().ToString();
            string userId = Guid.NewGuid().ToString();
            string to = Guid.NewGuid().ToString();
            string correlationId = Guid.NewGuid().ToString();
            string lockToken = Guid.NewGuid().ToString();
            string messageSchema = "default@v1";
            string contentType = "text/plain";
            string contentEncoding = "utf-8";
            DateTimeOffset createdOnUtc = DateTimeOffset.UtcNow;
            DateTimeOffset enqueuedOnUtc = DateTimeOffset.UtcNow;
            DateTimeOffset expiresOnUtc = DateTimeOffset.MaxValue;

            var message = new Message(Encoding.UTF8.GetBytes(payload))
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
