// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Api.Test
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
    }
}
