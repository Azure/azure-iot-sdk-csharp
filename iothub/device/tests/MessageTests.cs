// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test
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
        }

        [TestMethod]
        public void ConstructorTakingEmptyByteArrayTest()
        {
            var msg = new Message(Array.Empty<byte>());
            msg.Payload.Should().NotBeNull();
            msg.Payload.Length.Should().Be(0);
        }

        [TestMethod]
        public void SettingMessageAsSecurityMessageTest()
        {
            var msg = new TelemetryMessage("security message test");

            msg.IsSecurityMessage.Should().BeFalse();
            msg.SystemProperties.ContainsKey(MessageSystemPropertyNames.InterfaceId).Should().BeFalse();

            msg.SetAsSecurityMessage();

            msg.SystemProperties.ContainsKey(MessageSystemPropertyNames.InterfaceId).Should().BeTrue();
            msg.SystemProperties[MessageSystemPropertyNames.InterfaceId].Should().Be(CommonConstants.SecurityMessageInterfaceId);
            msg.IsSecurityMessage.Should().BeTrue();
        }

        [TestMethod]
        public void CloneWithBodyTest()
        {
            // arrange
            string contentEncoding = "gzip";
            string contentType = "text/plain";
            string userId = "JohnDoe";
            string messageId = Guid.NewGuid().ToString();
            string propName1 = "test1";
            string propValue1 = "test_v_1";
            string propName2 = "test2";
            string propValue2 = "test_v_2";
            string originalMessageContent = "Original copy";
            var originalMessage = new TelemetryMessage(originalMessageContent)
            {
                MessageId = messageId,
                ContentEncoding = contentEncoding,
                ContentType = contentType,
                UserId = userId,
                Properties =
                {
                    { propName1, propValue1 },
                    { propName2, propValue2 },
                },
            };

            // act
            string clonedMessageContent = "Cloned version";
            var clonedMessage = originalMessage.CloneWithBody(clonedMessageContent);

            // assert
            clonedMessage.Properties.Count.Should().Be(2);
            clonedMessage.Properties[propName1].Should().Be(propValue1, "Cloned message should have the original message's properties.");
            clonedMessage.Properties[propName2].Should().Be(propValue2, "Cloned message should have the original message's properties.");
            clonedMessage.ContentEncoding.Should().Be(contentEncoding, "Cloned message should have the original message's system properties.");
            clonedMessage.ContentType.Should().Be(contentType, "Cloned message should have the original message's system properties.");
            clonedMessage.UserId.Should().Be(userId, "Cloned message should have the original message's system properties.");
            clonedMessage.MessageId.Should().Be(messageId, "Cloned message should have the original message's system properties.");

            clonedMessage.Payload.Should().NotBeEquivalentTo(originalMessage.Payload, "Cloned message was initialized with a different content body.");
            clonedMessage.Payload.Should().BeEquivalentTo(clonedMessageContent, $"Cloned message was initialized with \"{clonedMessageContent}\" as content body.");
        }

        [TestMethod]
        public void CloneWithBodyWithNullTest()
        {
            // arrange
            string contentEncoding = "gzip";
            string propName1 = "test1";
            string propValue1 = "test_v_1";
            string propName2 = "test2";
            string originalMessageContent = "Original copy";
            var originalMessage = new TelemetryMessage(originalMessageContent)
            {
                ContentEncoding = contentEncoding,
                ContentType = null,
                Properties =
                {
                    { propName1, propValue1 },
                    { propName2, null },
                },
            };

            // act
            string clonedMessageContent = "Cloned version";
            var clonedMessage = originalMessage.CloneWithBody(clonedMessageContent);

            // assert
            clonedMessage.Properties.Count.Should().Be(2);
            clonedMessage.Properties[propName1].Should().Be(propValue1, "Cloned message should have the original message's properties.");
            clonedMessage.Properties[propName2].Should().BeNull("Cloned message should have the original message's properties.");
            clonedMessage.ContentEncoding.Should().Be(contentEncoding, "Cloned message should have the original message's system properties.");
            clonedMessage.SystemProperties.Keys.Should().Contain(MessageSystemPropertyNames.ContentType);
            clonedMessage.ContentType.Should().BeNull("Cloned message should have the original message's system properties.");
            clonedMessage.MessageId.Should().BeNull("Cloned message should have the original message's system properties.");
        }
    }
}