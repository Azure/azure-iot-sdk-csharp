// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.Azure.IoT.DigitalTwin.Device.Model;
using Xunit;

namespace Microsoft.Azure.IoT.DigitalTwin.Device.Test.Model
{
    public class DigitalTwinAsyncCommandUpdateTest
    {
        [Fact]
        public void TestConstructor1()
        {
            var commandUpdate = new DigitalTwinAsyncCommandUpdate("testName1", "testRequestId1", 888);

            Assert.Equal("testName1", commandUpdate.Name);
            Assert.Equal("testRequestId1", commandUpdate.RequestId);
            Assert.Equal(888, commandUpdate.Status);
            Assert.Equal(null, commandUpdate.Payload);
        }

        [Fact]
        public void TestConstructor2()
        {
            var commandUpdate = new DigitalTwinAsyncCommandUpdate("testName2", "testRequestId2", 999, "TestPayload2");

            Assert.Equal("testName2", commandUpdate.Name);
            Assert.Equal("testRequestId2", commandUpdate.RequestId);
            Assert.Equal(999, commandUpdate.Status);
            Assert.Equal("TestPayload2", commandUpdate.Payload);
        }

        [Fact]
        public void TestConstructor3()
        {
            var commandUpdate = new DigitalTwinAsyncCommandUpdate();

            Assert.Null(commandUpdate.Name);
            Assert.Null(commandUpdate.RequestId);
            Assert.Equal(0, commandUpdate.Status);
            Assert.Null(commandUpdate.Payload);
        }

        [Fact]
        public void TestConstructorWithNull()
        {
            var commandUpdate = new DigitalTwinAsyncCommandUpdate(null, null, 0, null);

            Assert.Null(commandUpdate.Name);
            Assert.Null(commandUpdate.RequestId);
            Assert.Equal(0, commandUpdate.Status);
            Assert.Null(commandUpdate.Payload);
        }

        [Fact]
        public void TestEquals()
        {
            var c1 = new DigitalTwinAsyncCommandUpdate("testName1", "testRequestId1", 888);
            var c2 = new DigitalTwinAsyncCommandUpdate("testName1", "testRequestId1", 888);

            Assert.True(c1.GetHashCode() == c2.GetHashCode());
            Assert.True(c1 == c2);
            Assert.True(c1.Equals(c2));

            var c3 = new DigitalTwinAsyncCommandUpdate("testName1", "testRequestId1", 888, "payload1");
            var c4 = new DigitalTwinAsyncCommandUpdate("testName1", "testRequestId1", 888, "payload1");

            Assert.True(c3.GetHashCode() == c4.GetHashCode());
            Assert.True(c3 == c4);
            Assert.True(c3.Equals(c4));
        }

        [Fact]
        public void TestNotEquals()
        {
            var c1 = new DigitalTwinAsyncCommandUpdate("testName1", "testRequestId1", 888);
            var c2 = new DigitalTwinAsyncCommandUpdate("testName2", "testRequestId2", 999);

            Assert.True(c1.GetHashCode() != c2.GetHashCode());
            Assert.True(c1 != c2);
            Assert.True(!c1.Equals(c2));

            var c3 = new DigitalTwinAsyncCommandUpdate("testName1", "testRequestId1", 888, "payload1");
            var c4 = new DigitalTwinAsyncCommandUpdate("testName1", "testRequestId1", 888, "payload2");

            Assert.True(c3.GetHashCode() != c4.GetHashCode());
            Assert.True(c3 != c4);
            Assert.True(!c3.Equals(c4));
        }

        [Fact]
        public void TestObjectEquals()
        {
            var c1 = new DigitalTwinAsyncCommandUpdate("testName1", "testRequestId1", 888);
            object c2 = new DigitalTwinAsyncCommandUpdate("testName1", "testRequestId1", 888);

            Assert.True(c1.Equals(c2));
        }

        [Fact]
        public void TestObjectNotEquals()
        {
            var c1 = new DigitalTwinAsyncCommandUpdate("testName1", "testRequestId1", 888);
            var c2 = new DigitalTwinAsyncCommandUpdate("testName2", "testRequestId2", 999);
            object c3 = new object();

            Assert.True(!c1.Equals(c2));
            Assert.True(!c1.Equals(c3));
        }

        [Fact]
        public void TestValidateNullName()
        {
            var commandUpdate = new DigitalTwinAsyncCommandUpdate(null, "testRequestId", 999, "TestPayload");

            Exception ex = Assert.Throws<ArgumentNullException>(() => commandUpdate.Validate());
        }

        [Fact]
        public void TestValidateNullRequestId()
        {
            var commandUpdate = new DigitalTwinAsyncCommandUpdate("testName", null, 999, "TestPayload");

            Exception ex = Assert.Throws<ArgumentNullException>(() => commandUpdate.Validate());
        }

        [Fact]
        public void TestValidatePass()
        {
            var commandUpdate = new DigitalTwinAsyncCommandUpdate("testName", "testRequestId", 999, "TestPayload");
            commandUpdate.Validate();
        }
    }
}
