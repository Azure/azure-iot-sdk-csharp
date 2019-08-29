// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Azure.Iot.DigitalTwin.Device.Helper;
using Azure.Iot.DigitalTwin.Device.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Pose;

namespace Azure.IoT.DigitalTwin.Device.Test
{
    [TestClass]
    public partial class DigitalTwinInterfaceClientTest
    {
        [TestMethod]
        public void TestConstructorWhenIdIsNull()
        {
            try
            {
                var client = new DigitalTwinInterfaceTestClient(null, "testInstanceName", Arg.Any<bool>(), Arg.Any<bool>());

                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(ArgumentNullException).FullName, ex.GetType().FullName);
                Assert.AreEqual("id", ((ArgumentNullException)ex).ParamName);
                Assert.IsTrue(ex.Message?.StartsWith("The parameter named id can't be null, empty string or white space.", StringComparison.Ordinal) ?? false);
            }
        }

        [TestMethod]
        public void TestConstructorWhenIdIsWhiteSpace()
        {
            try
            {
                var client = new DigitalTwinInterfaceTestClient(" ", "testInstanceName", Arg.Any<bool>(), Arg.Any<bool>());

                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(ArgumentNullException).FullName, ex.GetType().FullName);
                Assert.AreEqual("id", ((ArgumentNullException)ex).ParamName);
                Assert.IsTrue(ex.Message?.StartsWith("The parameter named id can't be null, empty string or white space.", StringComparison.Ordinal) ?? false);
            }
        }

        [TestMethod]
        public void TestConstructorWhenInstanceNameIsNull()
        {
            try
            {
                var client = new DigitalTwinInterfaceTestClient("urn:testId", null, Arg.Any<bool>(), Arg.Any<bool>());

                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(ArgumentNullException).FullName, ex.GetType().FullName);
                Assert.AreEqual("instanceName", ((ArgumentNullException)ex).ParamName);
                Assert.IsTrue(ex.Message?.StartsWith("The parameter named instanceName can't be null, empty string or white space.", StringComparison.Ordinal) ?? false);
            }
        }

        [TestMethod]
        public void TestConstructorWhenInstanceNameIsWhiteSpace()
        {
            try
            {
                var client = new DigitalTwinInterfaceTestClient("urn:testId", " ", Arg.Any<bool>(), Arg.Any<bool>());

                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(ArgumentNullException).FullName, ex.GetType().FullName);
                Assert.AreEqual("instanceName", ((ArgumentNullException)ex).ParamName);
                Assert.IsTrue(ex.Message?.StartsWith("The parameter named instanceName can't be null, empty string or white space.", StringComparison.Ordinal) ?? false);
            }
        }

        [TestMethod]
        public void TestConstructorWithValidInputs()
        {
            string id = "urn:testId";
            string instanceName = "testInstanceName";
            bool isCommandEnabled = Arg.Any<bool>();
            bool isPropertyUpdatedEnabled = Arg.Any<bool>();

            var client = new DigitalTwinInterfaceTestClient(id, instanceName, isCommandEnabled, isPropertyUpdatedEnabled);

            Assert.AreEqual(id, client.Id);
            Assert.AreEqual(instanceName, client.InstanceName);
            Assert.AreEqual(isCommandEnabled, client.IsCommandEnabled);
            Assert.AreEqual(isPropertyUpdatedEnabled, client.IsPropertyUpdatedEnabled);
        }

        [TestMethod]
        public void TestOnCommandRequest()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName", true, true);

            Task<DigitalTwinCommandResponse> response = client.OnCommandRequest(new DigitalTwinCommandRequest());

            Assert.AreEqual(404, response.Result.Status);
            Assert.IsNull(response.Result.Payload);
        }

        [TestMethod]
        public void TestOnPropertyUpdated()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName", true, true);

            Task updateTask = client.OnPropertyUpdated(new DigitalTwinPropertyUpdate());

            Assert.AreEqual(TaskStatus.RanToCompletion, updateTask.Status);
        }

        [TestMethod]
        public void TestReportPropertiesAsyncWhenPropertiesIsNull()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName", true, true);

            try
            {
                client.ReportPropertiesAsync(null).Wait();
            }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(AggregateException).FullName, ex.GetType().FullName);
                Assert.AreEqual(1, ((AggregateException)ex).InnerExceptions.Count);
                Exception innerEx = ((AggregateException)ex).InnerExceptions[0];

                Assert.AreEqual(typeof(ArgumentNullException).FullName, innerEx.GetType().FullName);
                Assert.AreEqual("properties", ((ArgumentException)innerEx).ParamName);
                Assert.IsTrue(innerEx.Message?.StartsWith("The parameter named properties can't be null.", StringComparison.Ordinal) ?? false);
                return;
            }

            Assert.Fail();
        }
    }
}
