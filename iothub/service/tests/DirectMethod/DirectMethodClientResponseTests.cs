// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Tests.DirectMethod
{
    [TestClass]
    [TestCategory("Unit")]
    public class DirectMethodClientResponseTests
    {
        [TestMethod]
        public void DirectMethodClientResponse_Get_PayloadAsString()
        {
            // arrage
            const int expectedStatus = 200;
            DateTimeOffset expectedPayload = DateTimeOffset.UtcNow;
            var source = new DirectMethodClientResponse
            {
                Status = expectedStatus,
                JsonPayload = new JRaw(JsonConvert.SerializeObject(expectedPayload)),
            };

            // act
            string payloadAsStringDmcr = source.PayloadAsString;
            string payloadAsStringManualConvert = source.JsonPayload.ToString();

            // assert
            payloadAsStringDmcr.Should().Be(payloadAsStringManualConvert);
        }

        [TestMethod]
        public void DirectMethodClientResponse_Payload_DateTimeOffset()
        {
            // arrange
            const int expectedStatus = 200;
            DateTimeOffset expectedPayload = DateTimeOffset.UtcNow;
            var source = new DirectMethodClientResponse
            {
                Status = expectedStatus,
                JsonPayload = new JRaw(JsonConvert.SerializeObject(expectedPayload)),
            };
            string body = JsonConvert.SerializeObject(source);

            // act
            DirectMethodClientResponse dmcr = JsonConvert.DeserializeObject<DirectMethodClientResponse>(body);

            // assert
            dmcr.Status.Should().Be(expectedStatus);
            dmcr.TryGetPayload(out DateTimeOffset actual).Should().BeTrue();
            actual.Should().Be(expectedPayload);
        }

        [TestMethod]
        public void DirectMethodClientResponse_Payload_Int()
        {
            // arrange
            const int expectedStatus = 200;
            int expectedPayload = int.MaxValue;
            var source = new DirectMethodClientResponse
            {
                Status = expectedStatus,
                JsonPayload = new JRaw(JsonConvert.SerializeObject(expectedPayload)),
            };
            string body = JsonConvert.SerializeObject(source);

            // act
            DirectMethodClientResponse dmcr = JsonConvert.DeserializeObject<DirectMethodClientResponse>(body);

            // assert
            dmcr.Status.Should().Be(expectedStatus);
            dmcr.TryGetPayload(out int actual).Should().BeTrue();
            actual.Should().Be(expectedPayload);
        }

        [TestMethod]
        public void DirectMethodClientResponse_Payload_IntList()
        {
            // arrange
            const int expectedStatus = 200;
            var expectedPayload = new List<int> { 1, 2, 3 };
            var source = new DirectMethodClientResponse
            {
                Status = expectedStatus,
                JsonPayload = new JRaw(JsonConvert.SerializeObject(expectedPayload)),
            };
            string body = JsonConvert.SerializeObject(source);

            // act
            DirectMethodClientResponse dmcr = JsonConvert.DeserializeObject<DirectMethodClientResponse>(body);

            // assert
            dmcr.Status.Should().Be(expectedStatus);
            dmcr.TryGetPayload(out List<int> actual).Should().BeTrue();
            actual.Should().BeEquivalentTo(expectedPayload);
        }

        [TestMethod]
        public void DirectMethodClientResponse_Payload_Bool()
        {
            // arrange
            const int expectedStatus = 200;
            bool expectedPayload = true;
            var source = new DirectMethodClientResponse
            {
                Status = expectedStatus,
                JsonPayload = new JRaw(JsonConvert.SerializeObject(expectedPayload)),
            };
            string body = JsonConvert.SerializeObject(source);

            // act
            DirectMethodClientResponse dmcr = JsonConvert.DeserializeObject<DirectMethodClientResponse>(body);

            // assert
            dmcr.Status.Should().Be(expectedStatus);
            dmcr.TryGetPayload(out bool actual).Should().BeTrue();
            actual.Should().Be(expectedPayload);
        }

        [TestMethod]
        public void DirectMethodClientResponse_Payload_String()
        {
            // arrange
            const int expectedStatus = 200;
            string expectedPayload = "The quick brown fox jumped over the lazy dog.";
            var source = new DirectMethodClientResponse
            {
                Status = expectedStatus,
                JsonPayload = new JRaw(JsonConvert.SerializeObject(expectedPayload)),
            };
            string body = JsonConvert.SerializeObject(source);

            // act
            DirectMethodClientResponse dmcr = JsonConvert.DeserializeObject<DirectMethodClientResponse>(body);

            // assert
            dmcr.Status.Should().Be(expectedStatus);
            dmcr.TryGetPayload(out string actual).Should().BeTrue();
            actual.Should().Be(expectedPayload);
        }

        [TestMethod]
        public void DirectMethodClientResponse_Payload_TimeSpan()
        {
            // arrange
            const int expectedStatus = 200;
            var expectedPayload = TimeSpan.FromSeconds(30);
            var source = new DirectMethodClientResponse
            {
                Status = expectedStatus,
                JsonPayload = new JRaw(JsonConvert.SerializeObject(expectedPayload)),
            };
            string body = JsonConvert.SerializeObject(source);

            // act
            DirectMethodClientResponse dmcr = JsonConvert.DeserializeObject<DirectMethodClientResponse>(body);

            // assert
            dmcr.Status.Should().Be(expectedStatus);
            dmcr.TryGetPayload(out TimeSpan actual).Should().BeTrue();
            actual.Should().Be(expectedPayload);
        }

        [TestMethod]
        public void DirectMethodClientResponse_Payload_CustomType()
        {
            // arrange
            const int expectedStatus = 200;
            var expectedPayload = new CustomType { CustomInt = 4, CustomString = "bar" };
            var source = new DirectMethodClientResponse
            {
                Status = expectedStatus,
                JsonPayload = new JRaw(JsonConvert.SerializeObject(expectedPayload)),
            };
            string body = JsonConvert.SerializeObject(source);

            // act
            DirectMethodClientResponse dmcr = JsonConvert.DeserializeObject<DirectMethodClientResponse>(body);

            // assert
            dmcr.Status.Should().Be(expectedStatus);
            dmcr.TryGetPayload(out CustomType actual).Should().BeTrue();
            actual.CustomInt.Should().Be(expectedPayload.CustomInt);
            actual.CustomString.Should().Be(expectedPayload.CustomString);
        }

        [TestMethod]
        public void DirectMethodClientResponse_Payload_Null()
        {
            // arrange
            const int expectedStatus = 200;
            var source = new DirectMethodClientResponse
            {
                Status = expectedStatus,
                JsonPayload = null,
            };

            // act and assert
            source.TryGetPayload(out string _).Should().BeFalse();
        }

        [TestMethod]
        public void DirectMethodClientResponse_Payload_ThrowsException()
        {
            // arrange
            const int expectedStatus = 200;
            var source = new DirectMethodClientResponse
            {
                Status = expectedStatus,
                JsonPayload = new JRaw(JsonConvert.SerializeObject(TimeSpan.FromSeconds(30)))
            };

            // act and assert
            // deliberately throw serialzation exception to ensure TryGetPayload() returns false
            source.TryGetPayload(out string[] _).Should().BeFalse();
        }

        private class CustomType
        {
            [JsonProperty("customInt")]
            public int CustomInt { get; set; }

            [JsonProperty("customString")]
            public string CustomString { get; set; }
        }
    }
}
