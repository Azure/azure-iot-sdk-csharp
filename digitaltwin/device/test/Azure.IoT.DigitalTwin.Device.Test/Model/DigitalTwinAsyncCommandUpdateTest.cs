using Azure.Iot.DigitalTwin.Device.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.IoT.DigitalTwin.Device.Test.Model
{
    [TestClass]
    public class DigitalTwinAsyncCommandUpdateTest
    {
        [TestMethod]
        public void TestConstructor1()
        {
            var commandUpate = new DigitalTwinAsyncCommandUpdate("testName1", "testRequestId1", 888);

            Assert.AreEqual("testName1", commandUpate.Name);
            Assert.AreEqual("testRequestId1", commandUpate.RequestId);
            Assert.AreEqual(888, commandUpate.Status);
            Assert.AreEqual(string.Empty, commandUpate.Payload);
        }

        [TestMethod]
        public void TestConstructor2()
        {
            var commandUpate = new DigitalTwinAsyncCommandUpdate("testName2", "testRequestId2", 999, "TestPayload2");

            Assert.AreEqual("testName2", commandUpate.Name);
            Assert.AreEqual("testRequestId2", commandUpate.RequestId);
            Assert.AreEqual(999, commandUpate.Status);
            Assert.AreEqual("TestPayload2", commandUpate.Payload);
        }

        [TestMethod]
        public void TestConstructor3()
        {
            var commandUpate = new DigitalTwinAsyncCommandUpdate();

            Assert.AreEqual(null, commandUpate.Name);
            Assert.AreEqual(null, commandUpate.RequestId);
            Assert.AreEqual(0, commandUpate.Status);
            Assert.AreEqual(null, commandUpate.Payload);
        }

        [TestMethod]
        public void TestConstructorWithNullValues()
        {
            var commandUpate = new DigitalTwinAsyncCommandUpdate(null, null, 0, null);

            Assert.AreEqual(null, commandUpate.Name);
            Assert.AreEqual(null, commandUpate.RequestId);
            Assert.AreEqual(0, commandUpate.Status);
            Assert.AreEqual(null, commandUpate.Payload);
        }
    }
}
