﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using FluentAssertions;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Api.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class MessageTests
    {
        private byte[] _emptyByteArray =
#if NET451
            new byte[0];
#else
            Array.Empty<byte>();

#endif

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
            Assert.IsTrue(ms.GetBuffer().Length == 0);

            msg = new Message((Stream)null);
            var bytes = msg.GetBytes();
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
            var msg = new Message(_emptyByteArray);
            var stream = msg.GetBodyStream();
            Assert.IsNotNull(stream);
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            Assert.IsTrue(ms.GetBuffer().Length == 0);

            msg = new Message(_emptyByteArray);
            var bytes = msg.GetBytes();
            Assert.AreEqual(0, bytes.Length);
        }

        [TestMethod]
        public void RetrievingMessageBytesAfterGetBodyStreamTest()
        {
            var msg = new Message(_emptyByteArray);
            msg.GetBodyStream();

            TestAssert.Throws<InvalidOperationException>(() => msg.GetBytes());
        }

        [TestMethod]
        public void RetrievingMessageBodyStreamAfterGetBytesTest()
        {
            var msg = new Message(_emptyByteArray);
            msg.GetBytes();

            TestAssert.Throws<InvalidOperationException>(() => msg.GetBodyStream());
        }

        [TestMethod]
        public void CallingGetBytesTwiceTest()
        {
            var msg = new Message(_emptyByteArray);
            msg.GetBytes();

            TestAssert.Throws<InvalidOperationException>(() => msg.GetBytes());
        }

        [TestMethod]
        public void CallingGetBodyStreamTwiceTest()
        {
            var msg = new Message(_emptyByteArray);
            msg.GetBodyStream();

            TestAssert.Throws<InvalidOperationException>(() => msg.GetBodyStream());
        }

        [TestMethod]
        public void DisposingOwnedStreamTest()
        {
            // ownStream = true
            var ms = new MemoryStream(Encoding.UTF8.GetBytes("Hello, World!"));
            var msg = new Message(ms, true);
            msg.Dispose();

            TestAssert.Throws<ObjectDisposedException>(() => ms.Write(Encoding.UTF8.GetBytes("howdy"), 0, 5));

            // ownStream = false
            ms = new MemoryStream(Encoding.UTF8.GetBytes("Hello, World!"));
            msg = new Message(ms, false);
            msg.Dispose();

            ms.Write(Encoding.UTF8.GetBytes("howdy"), 0, 5);
        }

        [TestMethod]
        public void HttpExceptionMappingTest_BulkRegistryOperationFailure()
        {
            var message = new HttpResponseMessage(HttpStatusCode.BadRequest);
            message.Headers.Add(CommonConstants.IotHubErrorCode, ErrorCode.BulkRegistryOperationFailure.ToString());
            bool isMappedToException = HttpClientHelper.IsMappedToException(message);
            Assert.IsFalse(isMappedToException, "BulkRegistryOperationFailures should not be mapped to exceptions");
        }

        [TestMethod]
        public void MessageShouldAlwaysSetMessageIdByDefault()
        {
            using var message = new Message(Encoding.UTF8.GetBytes("test message"));
            message.MessageId.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public void MessageShouldAllowMessageIdToBeUserSettable()
        {
            string messageId = Guid.NewGuid().ToString();
            using var message = new Message(Encoding.UTF8.GetBytes("test message"))
            {
                MessageId = messageId,
            };

            message.MessageId.Should().NotBeNullOrEmpty();
            message.MessageId.Should().Be(messageId, "MessageId should have the value set by user.");
        }

        [TestMethod]
        public void MessageShouldSetMessageIdToCorrelationIdByDefault()
        {
            string messageId = Guid.NewGuid().ToString();
            using var message = new Message(Encoding.UTF8.GetBytes("test message"))
            {
                MessageId = messageId,
            };

            message.CorrelationId.Should().NotBeNullOrEmpty();
            message.CorrelationId.Should().Be(messageId, "Default value of correlation Id should be the MessageId.");
        }

        [TestMethod]
        public void MessageShouldAllowCorrelationIdToBeUserSettable()
        {
            string messageId = Guid.NewGuid().ToString();
            string correlationId = Guid.NewGuid().ToString();
            using var message = new Message(Encoding.UTF8.GetBytes("test message"))
            {
                MessageId = messageId,
                CorrelationId = correlationId,
            };

            message.CorrelationId.Should().NotBeNullOrEmpty();
            message.CorrelationId.Should().NotBe(messageId, "CorrelationId should have the value set by user.");
            message.CorrelationId.Should().Be(correlationId, "CorrelationId should have the value set by user.");
        }
    }
}
