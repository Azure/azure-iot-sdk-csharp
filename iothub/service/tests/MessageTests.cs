// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using FluentAssertions;
using Microsoft.Azure.Devices.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Api.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class MessageTests
    {
        private byte[] _emptyByteArray = Array.Empty<byte>();

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
            var msg = new Message(new byte[0]);
            Assert.IsNotNull(msg.Payload);
            Assert.AreEqual(0, msg.Payload.Length);
        }
    }
}
