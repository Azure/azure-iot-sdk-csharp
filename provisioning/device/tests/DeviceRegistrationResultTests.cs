// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class DeviceRegistrationResultTests
    {
        [TestMethod]
        public void DeviceRegistrationResult_Payload_Null()
        {
            // arrange
            var result = new DeviceRegistrationResult
            {
                Payload = null,
            };

            // act - assert

            result.TryGetPayload(out object actual).Should().BeFalse();
            actual.Should().BeNull();
        }

        [TestMethod]
        public void DeviceRegistrationResult_Payload_DateTimeOffset()
        {
            // arrange

            const ProvisioningRegistrationStatus expectedStatus = ProvisioningRegistrationStatus.Assigned;
            DateTimeOffset expectedPayload = DateTimeOffset.UtcNow;
            var source = new DeviceRegistrationResult
            {
                Status = expectedStatus,
                Payload = new JRaw(JsonConvert.SerializeObject(expectedPayload)),
            };
            string body = JsonConvert.SerializeObject(source);

            // act
            DeviceRegistrationResult result = JsonConvert.DeserializeObject<DeviceRegistrationResult>(body);

            // assert

            result.Status.Should().Be(source.Status);
            result.TryGetPayload(out DateTimeOffset actual).Should().BeTrue();
            actual.Should().Be(expectedPayload);
        }

        [TestMethod]
        public void DeviceRegistrationResult_Payload_Int()
        {
            // arrange

            const ProvisioningRegistrationStatus expectedStatus = ProvisioningRegistrationStatus.Assigned;
            int expectedPayload = int.MaxValue;
            var source = new DeviceRegistrationResult
            {
                Status = expectedStatus,
                Payload = new JRaw(JsonConvert.SerializeObject(expectedPayload)),
            };
            string body = JsonConvert.SerializeObject(source);

            // act
            DeviceRegistrationResult result = JsonConvert.DeserializeObject<DeviceRegistrationResult>(body);

            // assert

            result.Status.Should().Be(source.Status);
            result.TryGetPayload(out int actual).Should().BeTrue();
            actual.Should().Be(expectedPayload);
        }

        [TestMethod]
        public void DeviceRegistrationResult_Payload_IntList()
        {
            // arrange

            const ProvisioningRegistrationStatus expectedStatus = ProvisioningRegistrationStatus.Assigned;
            var expectedPayload = new List<int> { 1, 2, 3 };
            var source = new DeviceRegistrationResult
            {
                Status = expectedStatus,
                Payload = new JRaw(JsonConvert.SerializeObject(expectedPayload)),
            };
            string body = JsonConvert.SerializeObject(source);

            // act
            DeviceRegistrationResult result = JsonConvert.DeserializeObject<DeviceRegistrationResult>(body);

            // assert

            result.Status.Should().Be(source.Status);
            result.TryGetPayload(out List<int> actual).Should().BeTrue();
            actual.Should().BeEquivalentTo(expectedPayload);
        }

        [TestMethod]
        public void DeviceRegistrationResult_Payload_Bool()
        {
            // arrange

            const ProvisioningRegistrationStatus expectedStatus = ProvisioningRegistrationStatus.Assigned;
            bool expectedPayload = true;
            var source = new DeviceRegistrationResult
            {
                Status = expectedStatus,
                Payload = new JRaw(JsonConvert.SerializeObject(expectedPayload)),
            };
            string body = JsonConvert.SerializeObject(source);

            // act
            DeviceRegistrationResult result = JsonConvert.DeserializeObject<DeviceRegistrationResult>(body);

            // assert

            result.Status.Should().Be(source.Status);
            result.TryGetPayload(out bool actual).Should().BeTrue();
            actual.Should().Be(expectedPayload);
        }

        [TestMethod]
        public void DeviceRegistrationResult_Payload_String()
        {
            // arrange

            const ProvisioningRegistrationStatus expectedStatus = ProvisioningRegistrationStatus.Assigned;
            string expectedPayload = "This is a testing string payload.";
            var source = new DeviceRegistrationResult
            {
                Status = expectedStatus,
                Payload = new JRaw(JsonConvert.SerializeObject(expectedPayload)),
            };
            string body = JsonConvert.SerializeObject(source);

            // act
            DeviceRegistrationResult result = JsonConvert.DeserializeObject<DeviceRegistrationResult>(body);

            // assert

            result.Status.Should().Be(source.Status);
            result.TryGetPayload(out string actual).Should().BeTrue();
            actual.Should().Be(expectedPayload);
        }

        [TestMethod]
        public void DeviceRegistrationResult_Payload_TimeSpan()
        {
            // arrange

            const ProvisioningRegistrationStatus expectedStatus = ProvisioningRegistrationStatus.Assigned;
            var expectedPayload = TimeSpan.FromSeconds(30);
            var source = new DeviceRegistrationResult
            {
                Status = expectedStatus,
                Payload = new JRaw(JsonConvert.SerializeObject(expectedPayload)),
            };
            string body = JsonConvert.SerializeObject(source);

            // act
            DeviceRegistrationResult result = JsonConvert.DeserializeObject<DeviceRegistrationResult>(body);

            // assert

            result.Status.Should().Be(source.Status);
            result.TryGetPayload(out TimeSpan actual).Should().BeTrue();
            actual.Should().Be(expectedPayload);
        }

        [TestMethod]
        public void DeviceRegistrationResult_Payload_CustomType()
        {
            // arrange

            const ProvisioningRegistrationStatus expectedStatus = ProvisioningRegistrationStatus.Assigned;
            var expectedPayload = new CustomType { CustomInt = 4, CustomString = "bar" };
            var source = new DeviceRegistrationResult
            {
                Status = expectedStatus,
                Payload = new JRaw(JsonConvert.SerializeObject(expectedPayload)),
            };
            string body = JsonConvert.SerializeObject(source);

            // act
            DeviceRegistrationResult result = JsonConvert.DeserializeObject<DeviceRegistrationResult>(body);

            // assert

            result.Status.Should().Be(source.Status);
            result.TryGetPayload(out CustomType actual).Should().BeTrue();
            actual.CustomInt.Should().Be(expectedPayload.CustomInt);
            actual.CustomString.Should().Be(expectedPayload.CustomString);
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
