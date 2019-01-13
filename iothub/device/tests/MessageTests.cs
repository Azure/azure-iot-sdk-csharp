// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Test
{
    using System;
    using System.IO;
    using System.Text;

    using Microsoft.Azure.Devices.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            var msg = new Message(Array.Empty<byte>());
            var stream = msg.GetBodyStream();
            Assert.IsNotNull(stream);
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            Assert.IsTrue(ms.GetBuffer().Length == 0);

            msg = new Message(Array.Empty<byte>());
            var bytes = msg.GetBytes();
            Assert.AreEqual(0, bytes.Length);
        }

        [TestMethod]
        public void RetrievingMessageBytesAfterGetBodyStreamTest()
        {
            var msg = new Message(Array.Empty<byte>());
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
            var msg = new Message(Array.Empty<byte>());
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
    }
}
