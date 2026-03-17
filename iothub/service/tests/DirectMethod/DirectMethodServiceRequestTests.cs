// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Microsoft.Azure.Devices.Tests.DirectMethod
{
    [TestClass]
    [TestCategory("Unit")]
    public class DirectMethodServiceRequestTests
    {
        [TestMethod]
        public void DirectMethodServiceRequest_Ctor_ThrowsOnNull()
        {
            Action act = () => _ = new DirectMethodServiceRequest(null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" \t\r\n")]
        public void DirectMethodServiceRequest_Ctor_ThrowsOnEmptyOrWhiteSpace(string methodName)
        {
            Action act = () => _ = new DirectMethodServiceRequest(methodName);
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void DirectMethodServiceRequest_Ctor_SetsMethodName()
        {
            // arrange
            const string expected = nameof(expected);

            // act
            var dmcr = new DirectMethodServiceRequest(expected);

            // assert
            dmcr.MethodName.Should().Be(expected);
        }

        [TestMethod]
        public void DirectMethodServiceRequest_ConnectionResponseTimeout()
        {
            // arrange
            var expectedTimeout = 1;
            var dcmr = new DirectMethodServiceRequest("setTelemetryInterval")
            {
                ConnectTimeoutInSeconds = expectedTimeout,
                ResponseTimeoutInSeconds = expectedTimeout,
            };

            dcmr.SetPayload("test");

            // act + assert
            dcmr.ConnectTimeoutInSeconds.Should().Be(expectedTimeout);
            dcmr.ResponseTimeoutInSeconds.Should().Be(expectedTimeout);
            dcmr.Payload!.Value.GetString().Should().Be("test");

            dcmr.ResponseTimeoutInSeconds.Should().Be(1);
            dcmr.ConnectTimeoutInSeconds.Should().Be(1);
        }

        [TestMethod]
        public void DirectMethodServiceRequest_ConnectionResponseTimeout_ShouldBeNull()
        {
            // arrange
            var expectedTimeout = TimeSpan.FromSeconds(1);
            var dcmr = new DirectMethodServiceRequest("123");
            dcmr.SetPayload("test");
            dcmr.ResponseTimeoutInSeconds.Should().Be(null);
            dcmr.ConnectTimeoutInSeconds.Should().Be(null);
        }

        [TestMethod]
        public void DirectMethodServiceRequest_SerializesCorrectly()
        {
            // arrange
            var directMethodServiceRequest = new DirectMethodServiceRequest(JsonSerializer.Serialize("testMethod"));

            directMethodServiceRequest.SetPayload("testPayload");

            // act
            string serializedDirectMethodServiceRequest = JsonSerializer.Serialize(directMethodServiceRequest);
            DirectMethodServiceRequest deserializedRequest = JsonSerializer.Deserialize<DirectMethodServiceRequest>(serializedDirectMethodServiceRequest);

            // assert
            deserializedRequest.Should().BeEquivalentTo(directMethodServiceRequest);
            deserializedRequest.Payload.Should().BeEquivalentTo(Encoding.UTF8.GetBytes(JsonSerializer.Serialize("testPayload")));
        }
    }
}
