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
            using (var original = new Message(Encoding.UTF8.GetBytes("Original copy")))
            {
                original.Properties["test1"] = "test_v_1";
                original.Properties["test2"] = "test_v_2";

                original.ContentEncoding = "gzip";
                original.ContentType = "text/plain";
                original.UserId = "JohnDoe";

                using (var clone = original.CloneWithBody(Encoding.UTF8.GetBytes("Cloned version")))
                { 
                    Assert.AreEqual("test_v_1", clone.Properties["test1"]);
                    Assert.AreEqual("test_v_2", clone.Properties["test2"]);

                    Assert.AreEqual("gzip", clone.ContentEncoding);
                    Assert.AreEqual("text/plain", clone.ContentType);
                    Assert.AreEqual("JohnDoe", clone.UserId);

                    var clonedContent = default(string);

                    using (var reader = new StreamReader(clone.BodyStream, Encoding.UTF8))
                    {
                        clonedContent = reader.ReadToEnd();
                    }
                
                    Assert.AreEqual("Cloned version", clonedContent);
                }
            }
        }

        [TestMethod]
        public void CloneWithBodyWithNullTest()
        {
            using (var original = new Message(Encoding.UTF8.GetBytes("Original copy")))
            {
                original.Properties["test1"] = "test_v_1";
                original.Properties["test2"] = null;

                original.ContentEncoding = "gzip";
                original.ContentType = null;

                using (var clone = original.CloneWithBody(Encoding.UTF8.GetBytes("Cloned version")))
                {                    
                    Assert.AreEqual("test_v_1", clone.Properties["test1"]);
                    Assert.AreEqual(null, clone.Properties["test2"]);
                    Assert.AreEqual(2, clone.Properties.Count);

                    Assert.AreEqual("gzip", clone.ContentEncoding);                    
                    Assert.IsTrue(clone.SystemProperties.Keys.Contains(MessageSystemPropertyNames.ContentType));
                    Assert.IsNull(clone.SystemProperties[MessageSystemPropertyNames.ContentType]);
                    Assert.IsFalse(clone.SystemProperties.Keys.Contains(MessageSystemPropertyNames.UserId));
                }
            }
        }
    }
}