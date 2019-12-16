// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.DigitalTwin.Client.Model;
using Xunit;

namespace Microsoft.Azure.Devices.DigitalTwin.Client.Test.Model
{
    public class DigitalTwinCommandResponseTest
    {
        [Fact]
        public void TestConstructor1()
        {
            var commandResp = new DigitalTwinCommandResponse(888, "TestPayLoad1");

            Assert.Equal(888, commandResp.Status);
            Assert.Equal("TestPayLoad1", commandResp.Payload);
        }

        [Fact]
        public void TestConstructor2()
        {
            var commandResp = new DigitalTwinCommandResponse();

            Assert.Equal(0, commandResp.Status);
            Assert.Null(commandResp.Payload);
        }

        [Fact]
        public void TestConstructor3()
        {
            var commandResp = new DigitalTwinCommandResponse(888);

            Assert.Equal(888, commandResp.Status);
            Assert.Null(commandResp.Payload);
        }

        [Fact]
        public void TestConstructorWithNullValues()
        {
            var commandResp = new DigitalTwinCommandResponse(0, null);

            Assert.Equal(0, commandResp.Status);
            Assert.Null(commandResp.Payload);
        }

        [Fact]
        public void TestEquals()
        {
            var c1 = new DigitalTwinCommandResponse(888, "TestPayLoad1");
            var c2 = new DigitalTwinCommandResponse(888, "TestPayLoad1");

            Assert.True(c1.GetHashCode() == c2.GetHashCode());
            Assert.True(c1 == c2);
            Assert.True(c1.Equals(c2));

            var c3 = new DigitalTwinCommandResponse(888, null);
            var c4 = new DigitalTwinCommandResponse(888, null);

            Assert.True(c3.GetHashCode() == c4.GetHashCode());
            Assert.True(c3 == c4);
            Assert.True(c3.Equals(c4));
        }

        [Fact]
        public void TestNotEquals()
        {
            var c1 = new DigitalTwinCommandResponse(888, null);
            var c2 = new DigitalTwinCommandResponse(888, "TestPayLoad");

            Assert.True(c1.GetHashCode() != c2.GetHashCode());
            Assert.True(c1 != c2);
            Assert.True(!c1.Equals(c2));

            var c3 = new DigitalTwinCommandResponse(888, "TestPayLoad");
            var c4 = new DigitalTwinCommandResponse(999, "TestPayLoad");

            Assert.True(c3.GetHashCode() != c4.GetHashCode());
            Assert.True(c3 != c4);
            Assert.True(!c3.Equals(c4));
        }

        [Fact]
        public void TestObjectEquals()
        {
            var c1 = new DigitalTwinCommandResponse(888, "TestPayLoad1");
            object c2 = new DigitalTwinCommandResponse(888, "TestPayLoad1");

            Assert.True(c1.Equals(c2));
        }

        [Fact]
        public void TestObjectNotEquals()
        {
            var c1 = new DigitalTwinCommandResponse(888, "TestPayLoad1");
            var c2 = new DigitalTwinCommandResponse(999, "TestPayLoad2");
            object c3 = new object();

            Assert.True(!c1.Equals(c2));
            Assert.True(!c1.Equals(c3));
        }
    }
}
