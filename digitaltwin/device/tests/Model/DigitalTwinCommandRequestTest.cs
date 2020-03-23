// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.DigitalTwin.Client.Model;
using Xunit;

namespace Microsoft.Azure.Devices.DigitalTwin.Client.Test.Model
{
    [Trait("TestCategory", "Unit")]
    [Trait("TestCategory", "PnP")]
    public class DigitalTwinCommandRequestTest
    {
        [Fact]
        public void TestConstructor1()
        {
            var commandReq = new DigitalTwinCommandRequest("testName1", "testRequestId1", "TestPayLoad1");

            Assert.Equal("testName1", commandReq.Name);
            Assert.Equal("testRequestId1", commandReq.RequestId);
            Assert.Equal("TestPayLoad1", commandReq.Payload);
        }

        [Fact]
        public void TestConstructor2()
        {
            var commandReq = new DigitalTwinCommandRequest();

            Assert.Null(commandReq.Name);
            Assert.Null(commandReq.RequestId);
            Assert.Null(commandReq.Payload);
        }

        [Fact]
        public void TestConstructorWithNull()
        {
            var commandReq = new DigitalTwinCommandRequest(null, null, null);

            Assert.Null(commandReq.Name);
            Assert.Null(commandReq.RequestId);
            Assert.Null(commandReq.Payload);
        }

        [Fact]
        public void TestEquals()
        {
            var c1 = new DigitalTwinCommandRequest("testName1", "testRequestId1", "TestPayLoad1");
            var c2 = new DigitalTwinCommandRequest("testName1", "testRequestId1", "TestPayLoad1");

            Assert.True(c1.GetHashCode() == c2.GetHashCode());
            Assert.True(c1 == c2);
            Assert.True(c1.Equals(c2));

            var c3 = new DigitalTwinCommandRequest("testName1", "testRequestId1", null);
            var c4 = new DigitalTwinCommandRequest("testName1", "testRequestId1", null);

            Assert.True(c3.GetHashCode() == c4.GetHashCode());
            Assert.True(c3 == c4);
            Assert.True(c3.Equals(c4));
        }

        [Fact]
        public void TestNotEquals()
        {
            var c1 = new DigitalTwinCommandRequest("testName1", "testRequestId1", "TestPayLoad1");
            var c2 = new DigitalTwinCommandRequest("testName2", "testRequestId2", "TestPayLoad2");

            Assert.True(c1.GetHashCode() != c2.GetHashCode());
            Assert.True(c1 != c2);
            Assert.True(!c1.Equals(c2));

            var c3 = new DigitalTwinCommandRequest("testName1", "testRequestId1", null);
            var c4 = new DigitalTwinCommandRequest("testName1", "testRequestId1", "TestPayLoad2");

            Assert.True(c3.GetHashCode() != c4.GetHashCode());
            Assert.True(c3 != c4);
            Assert.True(!c3.Equals(c4));
        }

        [Fact]
        public void TestObjectEquals()
        {
            var c1 = new DigitalTwinCommandRequest("testName1", "testRequestId1", "TestPayLoad1");
            object c2 = new DigitalTwinCommandRequest("testName1", "testRequestId1", "TestPayLoad1");

            Assert.True(c1.Equals(c2));
        }

        [Fact]
        public void TestObjectNotEquals()
        {
            var c1 = new DigitalTwinCommandRequest("testName1", "testRequestId1", "TestPayLoad1");
            var c2 = new DigitalTwinCommandRequest("testName2", "testRequestId2", "TestPayLoad2");
            object c3 = new object();

            Assert.True(!c1.Equals(c2));
            Assert.True(!c1.Equals(c3));
        }
    }
}
