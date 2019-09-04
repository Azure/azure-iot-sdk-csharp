// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Iot.DigitalTwin.Device.Exceptions;
using Azure.Iot.DigitalTwin.Device.Helper;
using Azure.Iot.DigitalTwin.Device.Model;
using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

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
            }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(ArgumentNullException).FullName, ex.GetType().FullName);
                Assert.AreEqual("id", ((ArgumentNullException)ex).ParamName);
                Assert.IsTrue(ex.Message?.StartsWith("The parameter named id can't be null, empty string or white space.", StringComparison.Ordinal) ?? false);
                return;
            }

            Assert.Fail();
        }

        [TestMethod]
        public void TestConstructorWhenIdIsWhiteSpace()
        {
            try
            {
                var client = new DigitalTwinInterfaceTestClient(" ", "testInstanceName", Arg.Any<bool>(), Arg.Any<bool>());
            }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(ArgumentNullException).FullName, ex.GetType().FullName);
                Assert.AreEqual("id", ((ArgumentNullException)ex).ParamName);
                Assert.IsTrue(ex.Message?.StartsWith("The parameter named id can't be null, empty string or white space.", StringComparison.Ordinal) ?? false);
                return;
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(GetInvalidInterfaceIdData), DynamicDataSourceType.Method)]
        public void TestConstructorWhenIdFormatIsInvalid(string id)
        {
            try
            {
                var client = new DigitalTwinInterfaceTestClient(id, "testInstanceName", Arg.Any<bool>(), Arg.Any<bool>());
            }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(ArgumentException).FullName, ex.GetType().FullName);
                Assert.AreEqual("id", ((ArgumentException)ex).ParamName);
                Assert.IsTrue(ex.Message?.StartsWith(DigitalTwinConstants.InvalidInterfaceIdErrorMessage, StringComparison.Ordinal) ?? false);
                return;
            }

            Assert.Fail($"Expected to throw exception for invalid interface id {id}.");
        }

        [TestMethod]
        public void TestConstructorWhenInstanceNameIsNull()
        {
            try
            {
                var client = new DigitalTwinInterfaceTestClient("urn:testId", null, Arg.Any<bool>(), Arg.Any<bool>());
            }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(ArgumentNullException).FullName, ex.GetType().FullName);
                Assert.AreEqual("instanceName", ((ArgumentNullException)ex).ParamName);
                Assert.IsTrue(ex.Message?.StartsWith("The parameter named instanceName can't be null, empty string or white space.", StringComparison.Ordinal) ?? false);
                return;
            }

            Assert.Fail();
        }

        [TestMethod]
        public void TestConstructorWhenInstanceNameIsWhiteSpace()
        {
            try
            {
                var client = new DigitalTwinInterfaceTestClient("urn:testId", " ", Arg.Any<bool>(), Arg.Any<bool>());
            }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(ArgumentNullException).FullName, ex.GetType().FullName);
                Assert.AreEqual("instanceName", ((ArgumentNullException)ex).ParamName);
                Assert.IsTrue(ex.Message?.StartsWith("The parameter named instanceName can't be null, empty string or white space.", StringComparison.Ordinal) ?? false);
                return;
            }

            Assert.Fail();
        }

        [DataTestMethod]
        [DynamicData(nameof(GetInvalidInterfaceInstanceNameData), DynamicDataSourceType.Method)]
        public void TestConstructorWhenInstanceNameFormatIsInvalid(string instanceName)
        {
            try
            {
                var client = new DigitalTwinInterfaceTestClient("urn:testId", instanceName, Arg.Any<bool>(), Arg.Any<bool>());
            }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(ArgumentException).FullName, ex.GetType().FullName);
                Assert.AreEqual("instanceName", ((ArgumentException)ex).ParamName);
                Assert.IsTrue(ex.Message?.StartsWith(DigitalTwinConstants.InvalidInterfaceInstanceNameErrorMessage, StringComparison.Ordinal) ?? false);
                return;
            }

            Assert.Fail($"Expected to throw exception for invalid interface name {instanceName}.");
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
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName", Arg.Any<bool>(), Arg.Any<bool>());

            Task<DigitalTwinCommandResponse> response = client.OnCommandRequest(new DigitalTwinCommandRequest());

            Assert.AreEqual(404, response.Result.Status);
            Assert.IsNull(response.Result.Payload);
        }

        [TestMethod]
        public void TestOnPropertyUpdated()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName", Arg.Any<bool>(), Arg.Any<bool>());

            Task updateTask = client.OnPropertyUpdated(new DigitalTwinPropertyUpdate());

            Assert.AreEqual(TaskStatus.RanToCompletion, updateTask.Status);
        }

        [TestMethod]
        public void TestReportPropertiesAsyncWhenPropertiesIsNull()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName", Arg.Any<bool>(), Arg.Any<bool>());

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

        [TestMethod]
        public void TestReportPropertiesAsyncWhenInterfaceNotRegistered()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName", Arg.Any<bool>(), Arg.Any<bool>());

            try
            {
                IList<DigitalTwinPropertyReport> properties = new List<DigitalTwinPropertyReport>();
                properties.Add(new DigitalTwinPropertyReport("propertyName1", "propertyValue1"));
                client.ReportPropertiesAsync(properties).Wait();
            }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(AggregateException).FullName, ex.GetType().FullName);
                Assert.AreEqual(1, ((AggregateException)ex).InnerExceptions.Count);

                Exception innerEx = ((AggregateException)ex).InnerExceptions[0];
                Assert.AreEqual(typeof(DigitalTwinDeviceInterfaceNotRegisteredException).FullName, innerEx.GetType().FullName);
                Assert.IsTrue(innerEx.Message?.Equals("The interface instanceName is not registered.", StringComparison.Ordinal) ?? false);
                return;
            }

            Assert.Fail();
        }

        //[TestMethod]
        //public void TestReportPropertiesAsyncWillCallToDigitalTwinClient()
        //{
        //    var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName", Arg.Any<bool>(), Arg.Any<bool>());
        //    var digitalTwinClient = new DigitalTwinTestClient(null);
        //    client.Initialize(digitalTwinClient);

        //    IList<DigitalTwinPropertyReport> properties = new List<DigitalTwinPropertyReport>();
        //    properties.Add(new DigitalTwinPropertyReport("propertyName1", "\"propertyValue1\""));
        //    client.ReportPropertiesAsync(properties).Wait();
        //}

        private static IEnumerable<object[]> GetInvalidInterfaceIdData()
        {
            yield return new object[] { "urN:iwoerWE:RE309_4" };
            yield return new object[] { "ur:iwoerWER:RE309_4" };
            yield return new object[] { "urn:iwoerWER:RE309!4" };
            yield return new object[] { $"urn:{new string('A', 253)}" };
        }

        private static IEnumerable<object[]> GetInvalidInterfaceInstanceNameData()
        {
            yield return new object[] { "iwoerWE!" };
            yield return new object[] { "ur:iwoerWER_4" };
            yield return new object[] { $"{new string('A', 257)}" };
        }
    }
}
