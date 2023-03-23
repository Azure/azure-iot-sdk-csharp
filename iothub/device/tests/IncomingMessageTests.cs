// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class IncomingMessageTests
    {
        [TestMethod]
        public void IncomingMessage_ValidatePayloadString()
        {
            // arrange
            const string payload = "test message";

            // act
            var testMessage = new IncomingMessage(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)));

            // assert
            testMessage.TryGetPayload(out string actualPayload);
            actualPayload.Should().Be(payload);
        }

        [TestMethod]
        public void IncomingMessage_ValidatePayloadInt()
        {
            // arrange
            const int payload = 123;

            // act
            var testMessage = new IncomingMessage(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)));

            // assert
            testMessage.TryGetPayload(out int actualPayload);
            actualPayload.Should().Be(payload);
        }

        [TestMethod]
        public void IncomingMessage_ValidatePayloadDateTime()
        {
            // arrange
            DateTime payload = DateTime.Now;

            // act
            var testMessage = new IncomingMessage(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)));

            // assert
            testMessage.TryGetPayload(out DateTime actualPayload);
            actualPayload.Should().Be(payload);
        }

        [TestMethod]
        public void IncomingMessage_ValidatePayloadDateTimeOffset()
        {
            // arrange
            DateTimeOffset payload = DateTimeOffset.UtcNow;

            // act
            var testMessage = new IncomingMessage(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)));

            // assert
            testMessage.TryGetPayload(out DateTimeOffset actualPayload);
            actualPayload.Should().Be(payload);
        }

        [TestMethod]
        public void IncomingMessage_ValidatePayloadTimeSpan()
        {
            // arrange
            var payload = TimeSpan.FromSeconds(123);

            // act
            var testMessage = new IncomingMessage(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)));

            // assert
            testMessage.TryGetPayload(out TimeSpan actualPayload);
            actualPayload.Should().Be(payload);
        }

        [TestMethod]
        public void IncomingMessage_ValidateProperties()
        {
            // arrange and act
            var testMessage = new IncomingMessage(null)
            {
                InputName = "endpoint1",
                MessageId = "123",
                CorrelationId = "1234",
                SequenceNumber = 123,
                To = "destination",
                UserId = "id",
                CreatedOnUtc = new DateTimeOffset(DateTime.MinValue),
                EnqueuedOnUtc = new DateTimeOffset(DateTime.MinValue),
                ExpiresOnUtc = new DateTimeOffset(DateTime.MinValue),
                MessageSchema = "schema",
                ContentType = "type",
                ContentEncoding = "encoding",
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // assert
            testMessage.InputName.Should().Be("endpoint1");
            testMessage.MessageId.Should().Be("123");
            testMessage.CorrelationId.Should().Be("1234");
            testMessage.SequenceNumber.Should().Be(123);
            testMessage.To.Should().Be("destination");
            testMessage.UserId.Should().Be("id");
            testMessage.CreatedOnUtc.Should().Be(new DateTimeOffset(DateTime.MinValue));
            testMessage.EnqueuedOnUtc.Should().Be(new DateTimeOffset(DateTime.MinValue));
            testMessage.ExpiresOnUtc.Should().Be(new DateTimeOffset(DateTime.MinValue));
            testMessage.MessageSchema.Should().Be("schema");
            testMessage.ContentType.Should().Be("type");
            testMessage.ContentEncoding.Should().Be("encoding");
            testMessage.Properties.Should().NotBeNull();
            testMessage.PayloadConvention.Should().Be(DefaultPayloadConvention.Instance);
        }

        [TestMethod]
        public void IncomingMessage_ValidateBinaryPayload()
        {
            double payload = 45.67;
            var testMessage = new IncomingMessage(BitConverter.GetBytes(payload));
            testMessage.TryGetPayload(out double actualPayload);
            actualPayload.Should().Be(payload);
        }
    }
}
