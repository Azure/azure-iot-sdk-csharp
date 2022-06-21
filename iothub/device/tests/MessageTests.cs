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
        public void ConstructorTakingStreamTest()
        {
            var ms = new MemoryStream(Encoding.UTF8.GetBytes("Hello, World!"));
            var msg = new Message(ms);
            var stream = msg.GetBodyStream();
            Assert.AreSame(ms, stream);
        }

        [TestMethod]
        public void ConstructorTakingNullStreamTest()
        {
            var msg = new Message((Stream)null);
            var stream = msg.GetBodyStream();
            Assert.IsNotNull(stream);
            var ms = new MemoryStream();
            stream.CopyTo(ms);

            int buffLen = 0;
#if NETCOREAPP1_1
            ms.TryGetBuffer(out ArraySegment<byte> buffer);
            buffLen = buffer.Count;
#else
            byte[] buffer = ms.GetBuffer();
            buffLen = buffer.Length;
#endif
            Assert.AreEqual(0, buffLen);

            msg = new Message((Stream)null);
            byte[] bytes = msg.GetBytes();
            Assert.AreEqual(0, bytes.Length);
        }

        [TestMethod]
        public void ConstructorTakingByteArrayTest()
        {
            const string MsgContents = "Hello, World!";
            var msg = new Message(Encoding.UTF8.GetBytes(MsgContents));
            var stream = msg.GetBodyStream();
            var sr = new StreamReader(stream);
            Assert.AreEqual(sr.ReadToEnd(), MsgContents);

            msg = new Message(Encoding.UTF8.GetBytes(MsgContents));
            var msgBytes = msg.GetBytes();
            Assert.AreEqual(Encoding.UTF8.GetString(msgBytes), MsgContents);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructorTakingNullByteArrayTest()
        {
            new Message((byte[])null);
        }

        [TestMethod]
        public void ConstructorTakingEmptyByteArrayTest()
        {
            var msg = new Message(new byte[0]);
            var stream = msg.GetBodyStream();
            Assert.IsNotNull(stream);
            var ms = new MemoryStream();
            stream.CopyTo(ms);

            int buffLen = 0;
#if NETCOREAPP1_1
            ms.TryGetBuffer(out ArraySegment<byte> buffer);
            buffLen = buffer.Count;
#else
            byte[] buffer = ms.GetBuffer();
            buffLen = buffer.Length;
#endif
            Assert.AreEqual(0, buffLen);

            msg = new Message(new byte[0]);
            byte[] bytes = msg.GetBytes();
            Assert.AreEqual(0, bytes.Length);
        }

        [TestMethod]
        public void RetrievingMessageBytesAfterGetBodyStreamTest()
        {
            var msg = new Message(new byte[0]);
            msg.GetBodyStream();

            TestAssert.Throws<InvalidOperationException>(() => msg.GetBytes());
        }

        [TestMethod]
        public void RetrievingMessageBodyStreamAfterGetBytesTest()
        {
            var ms = new MemoryStream(Encoding.UTF8.GetBytes("Hello, World!"));
            var msg = new Message(ms);
            msg.GetBytes();
            msg.ResetBody();
            var stream = msg.GetBodyStream();

            Assert.AreSame(ms, stream);
        }

        [TestMethod]
        public void Message_CallingGetBytesTwiceWithoutReset_Fails()
        {
            const string MsgContents = "Hello, World!";

            var msg = new Message(Encoding.UTF8.GetBytes(MsgContents));
            msg.GetBytes();

            Assert.ThrowsException<InvalidOperationException>(() => { byte[] msgBytes = msg.GetBytes(); });
        }

        [TestMethod]
        public void CallingGetBodyStreamTwiceTest()
        {
            var msg = new Message(new byte[0]);
            msg.GetBodyStream();

            TestAssert.Throws<InvalidOperationException>(() => msg.GetBodyStream());
        }

        [TestMethod]
        public void DisposingOwnedStreamTest()
        {
            // SDK should dispose the stream.
            var ms = new MemoryStream(Encoding.UTF8.GetBytes("Hello, World!"));
            var msg = new Message(ms, StreamDisposalResponsibility.Sdk);
            msg.Dispose();

            TestAssert.Throws<ObjectDisposedException>(() => ms.Write(Encoding.UTF8.GetBytes("howdy"), 0, 5));

            // The calling application will dispose the stream.
            ms = new MemoryStream(Encoding.UTF8.GetBytes("Hello, World!"));
            msg = new Message(ms, StreamDisposalResponsibility.App);
            msg.Dispose();

            ms.Write(Encoding.UTF8.GetBytes("howdy"), 0, 5);
        }

        [TestMethod]
        public void SettingMessageAsSecurityMessageTest()
        {
            var msg = new Message(Encoding.UTF8.GetBytes("security message test"));

            Assert.IsFalse(msg.IsSecurityMessage);
            Assert.IsFalse(msg.SystemProperties.ContainsKey(MessageSystemPropertyNames.InterfaceId));

            msg.SetAsSecurityMessage();

            Assert.IsTrue(msg.SystemProperties.ContainsKey(MessageSystemPropertyNames.InterfaceId));
            Assert.AreEqual(CommonConstants.SecurityMessageInterfaceId, msg.SystemProperties[MessageSystemPropertyNames.InterfaceId]);
            Assert.IsTrue(msg.IsSecurityMessage);
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
            using var originalMessage = new Message(Encoding.UTF8.GetBytes(originalMessageContent))
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
            using var clonedMessage = originalMessage.CloneWithBody(Encoding.UTF8.GetBytes(clonedMessageContent));

            // assert
            clonedMessage.Properties.Count.Should().Be(2);
            clonedMessage.Properties[propName1].Should().Be(propValue1, "Cloned message should have the original message's properties.");
            clonedMessage.Properties[propName2].Should().Be(propValue2, "Cloned message should have the original message's properties.");
            clonedMessage.ContentEncoding.Should().Be(contentEncoding, "Cloned message should have the original message's system properties.");
            clonedMessage.ContentType.Should().Be(contentType, "Cloned message should have the original message's system properties.");
            clonedMessage.UserId.Should().Be(userId, "Cloned message should have the original message's system properties.");
            clonedMessage.MessageId.Should().Be(messageId, "Cloned message should have the original message's system properties.");

            using var originalContentReader = new StreamReader(originalMessage.BodyStream, Encoding.UTF8);
            string originalContent = originalContentReader.ReadToEnd();
            using var clonedContentReader = new StreamReader(clonedMessage.BodyStream, Encoding.UTF8);
            string clonedContent = clonedContentReader.ReadToEnd();
            clonedContent.Should().NotBe(originalContent, "Cloned message was initialized with a different content body.");
            clonedContent.Should().Be(clonedMessageContent, $"Cloned message was initialized with \"{clonedMessageContent}\" as content body.");
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
            using var originalMessage = new Message(Encoding.UTF8.GetBytes(originalMessageContent))
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
            using var clonedMessage = originalMessage.CloneWithBody(Encoding.UTF8.GetBytes(clonedMessageContent));

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