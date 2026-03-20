// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class TelemetryMessageTests
    {
        [TestMethod]
        public void ConstructorTakingPayloadTest()
        {
            const string payloadString = "Hello, World!";
            var msg = new TelemetryMessage();
            msg.SetPayload(payloadString);
            Assert.AreEqual(Encoding.UTF8.GetString(msg.Payload), payloadString);
        }

        [TestMethod]
        public void ConstructorTakingBytePayloadTest()
        {
            Encoding encoder = Encoding.UTF32;
            const string payloadString = "Hello, World!";
            byte[] payloadBytes = encoder.GetBytes(payloadString);
            var msg = new TelemetryMessage(payloadBytes);
            Assert.IsTrue(Enumerable.SequenceEqual(payloadBytes, msg.Payload));

            byte[] actualPayload = msg.Payload;
            encoder.GetString(actualPayload).Should().BeEquivalentTo(payloadString);
        }

        [TestMethod]
        public void ConstructorTakingEmptyByteArrayTest()
        {
            var msg = new TelemetryMessage(null);
            msg.Payload.Should().BeNull();
        }

        [TestMethod]
        public void SettingMessageAsSecurityMessageTest()
        {
            var msg = new TelemetryMessage();
            msg.SetPayload("security message test");
            msg.IsSecurityMessage.Should().BeFalse();
            msg.SystemProperties.ContainsKey(MessageSystemPropertyNames.InterfaceId).Should().BeFalse();

            msg.SetAsSecurityMessage();
            msg.SystemProperties.ContainsKey(MessageSystemPropertyNames.InterfaceId).Should().BeTrue();
            msg.SystemProperties[MessageSystemPropertyNames.InterfaceId].Should().Be(CommonConstants.SecurityMessageInterfaceId);
            msg.IsSecurityMessage.Should().BeTrue();
        }

        [TestMethod]
        public void SetTelemetryMessageProperties()
        {
            // arrange and act
            var testMessage = new TelemetryMessage(Encoding.UTF8.GetBytes("test message"))
            {
                InputName = "endpoint1",
                MessageId = "123",
                CorrelationId = "1234",
                UserId = "id",
                CreatedOnUtc = new DateTimeOffset(DateTime.MinValue),
                BatchCreatedOnUtc = new DateTimeOffset(DateTime.MinValue),
                EnqueuedOnUtc = new DateTimeOffset(DateTime.MinValue),
                ExpiresOnUtc = new DateTimeOffset(DateTime.MinValue),
                ComponentName = "component",
                MessageSchema = "schema",
                ContentType = "type",
                ContentEncoding = "encoding",
                ConnectionDeviceId = "connectionDeviceId",
                ConnectionModuleId = "connectionModuleId",
            };

            var testMessage1 = new IncomingMessage(Encoding.UTF8.GetBytes("test message"));

            // assert
            testMessage.Payload.Should().NotBeNull();
            testMessage.InputName.Should().Be("endpoint1");
            testMessage.MessageId.Should().Be("123");
            testMessage.CorrelationId.Should().Be("1234");
            testMessage.UserId.Should().Be("id");
            testMessage.CreatedOnUtc.Should().Be(new DateTimeOffset(DateTime.MinValue));
            testMessage.BatchCreatedOnUtc.Should().Be(new DateTimeOffset(DateTime.MinValue));
            testMessage.EnqueuedOnUtc.Should().Be(new DateTimeOffset(DateTime.MinValue));
            testMessage.ExpiresOnUtc.Should().Be(new DateTimeOffset(DateTime.MinValue));
            testMessage.ComponentName.Should().Be("component");
            testMessage.MessageSchema.Should().Be("schema");
            testMessage.ContentType.Should().Be("type");
            testMessage.ContentEncoding.Should().Be("encoding");
            testMessage.Properties.Should().NotBeNull();
            testMessage.ConnectionDeviceId.Should().Be("connectionDeviceId");
            testMessage.ConnectionModuleId.Should().Be("connectionModuleId");

            testMessage1.InputName.Should().BeNull();
        }
    }
}