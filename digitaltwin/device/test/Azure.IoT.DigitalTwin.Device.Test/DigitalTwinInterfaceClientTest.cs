// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Iot.DigitalTwin.Device;
using Azure.Iot.DigitalTwin.Device.Exceptions;
using Azure.Iot.DigitalTwin.Device.Helper;
using Azure.Iot.DigitalTwin.Device.Model;
using Microsoft.Azure.Devices.Client;
using NSubstitute;
using NSubstitute.Core;
using Xunit;

namespace Azure.IoT.DigitalTwin.Device.Test
{
    public class DigitalTwinInterfaceClientTest
    {
        [Fact]
        public void TestConstructorWhenIdIsNull()
        {
            try
            {
                var client = new DigitalTwinInterfaceTestClient(null, "testInstanceName", true, true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(ArgumentNullException).FullName, ex.GetType().FullName);
                Assert.Equal("id", ((ArgumentNullException)ex).ParamName);
                Assert.True(ex.Message?.StartsWith("The parameter named id can't be null, empty string or white space.", StringComparison.Ordinal) ?? false);
                return;
            }

            Assert.True(false);
        }

        [Fact]
        public void TestConstructorWhenIdIsWhiteSpace()
        {
            try
            {
                var client = new DigitalTwinInterfaceTestClient(" ", "testInstanceName", true, true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(ArgumentNullException).FullName, ex.GetType().FullName);
                Assert.Equal("id", ((ArgumentNullException)ex).ParamName);
                Assert.True(ex.Message?.StartsWith("The parameter named id can't be null, empty string or white space.", StringComparison.Ordinal) ?? false);
                return;
            }
        }

        [Theory]
        [MemberData(nameof(GetInvalidInterfaceIdData))]
        public void TestConstructorWhenIdFormatIsInvalid(string id)
        {
            try
            {
                var client = new DigitalTwinInterfaceTestClient(id, "testInstanceName", true, true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(ArgumentException).FullName, ex.GetType().FullName);
                Assert.Equal("id", ((ArgumentException)ex).ParamName);
                Assert.True(ex.Message?.StartsWith(DigitalTwinConstants.InvalidInterfaceIdErrorMessage, StringComparison.Ordinal) ?? false);
                return;
            }

            Assert.True(false, $"Expected to throw exception for invalid interface id {id}.");
        }

        [Fact]
        public void TestConstructorWhenInstanceNameIsNull()
        {
            try
            {
                var client = new DigitalTwinInterfaceTestClient("urn:testId", null, true, true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(ArgumentNullException).FullName, ex.GetType().FullName);
                Assert.Equal("instanceName", ((ArgumentNullException)ex).ParamName);
                Assert.True(ex.Message?.StartsWith("The parameter named instanceName can't be null, empty string or white space.", StringComparison.Ordinal) ?? false);
                return;
            }

            Assert.True(false);
        }

        [Fact]
        public void TestConstructorWhenInstanceNameIsWhiteSpace()
        {
            try
            {
                var client = new DigitalTwinInterfaceTestClient("urn:testId", " ", true, true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(ArgumentNullException).FullName, ex.GetType().FullName);
                Assert.Equal("instanceName", ((ArgumentNullException)ex).ParamName);
                Assert.True(ex.Message?.StartsWith("The parameter named instanceName can't be null, empty string or white space.", StringComparison.Ordinal) ?? false);
                return;
            }

            Assert.True(false);
        }

        [Theory]
        [MemberData(nameof(GetInvalidInterfaceInstanceNameData))]
        public void TestConstructorWhenInstanceNameFormatIsInvalid(string instanceName)
        {
            try
            {
                var client = new DigitalTwinInterfaceTestClient("urn:testId", instanceName, true, true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(ArgumentException).FullName, ex.GetType().FullName);
                Assert.Equal("instanceName", ((ArgumentException)ex).ParamName);
                Assert.True(ex.Message?.StartsWith(DigitalTwinConstants.InvalidInterfaceInstanceNameErrorMessage, StringComparison.Ordinal) ?? false);
                return;
            }

            Assert.True(false, $"Expected to throw exception for invalid interface name {instanceName}.");
        }

        [Fact]
        public void TestConstructorWithValidInputs()
        {
            string id = "urn:testId";
            string instanceName = "testInstanceName";
            bool isCommandEnabled = Arg.Any<bool>();
            bool isPropertyUpdatedEnabled = Arg.Any<bool>();

            var client = new DigitalTwinInterfaceTestClient(id, instanceName, isCommandEnabled, isPropertyUpdatedEnabled);

            Assert.Equal(id, client.Id);
            Assert.Equal(instanceName, client.InstanceName);
            Assert.Equal(isCommandEnabled, client.IsCommandEnabled);
            Assert.Equal(isPropertyUpdatedEnabled, client.IsPropertyUpdatedEnabled);
        }

        [Fact]
        public void TestOnCommandRequest()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName", true, true);

            Task<DigitalTwinCommandResponse> response = client.OnCommandRequest(new DigitalTwinCommandRequest());

            Assert.Equal(404, response.Result.Status);
            Assert.Null(response.Result.Payload);
        }

        [Fact]
        public void TestOnPropertyUpdated()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName", true, true);

            Task updateTask = client.OnPropertyUpdated(new DigitalTwinPropertyUpdate());

            Assert.Equal(TaskStatus.RanToCompletion, updateTask.Status);
        }

        [Fact]
        public void OnRegistrationCompleted()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName", true, true);

            client.OnRegistrationCompleted2();
        }

        [Fact]
        public void TestReportPropertiesAsyncWhenPropertiesIsNull()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName", true, true);

            try
            {
                client.ReportPropertiesAsync(null).Wait();
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(AggregateException).FullName, ex.GetType().FullName);
                Assert.Equal(1, ((AggregateException)ex).InnerExceptions.Count);

                Exception innerEx = ((AggregateException)ex).InnerExceptions[0];
                Assert.Equal(typeof(ArgumentNullException).FullName, innerEx.GetType().FullName);
                Assert.Equal("properties", ((ArgumentException)innerEx).ParamName);
                Assert.True(innerEx.Message?.StartsWith("The parameter named properties can't be null.", StringComparison.Ordinal) ?? false);
                return;
            }

            Assert.True(false);
        }

        [Fact]
        public void TestReportPropertiesAsyncWhenInterfaceNotRegistered()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName", true, true);

            try
            {
                IList<DigitalTwinPropertyReport> properties = new List<DigitalTwinPropertyReport>();
                properties.Add(new DigitalTwinPropertyReport("propertyName1", "propertyValue1"));
                client.ReportPropertiesAsync(properties).Wait();
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(AggregateException).FullName, ex.GetType().FullName);
                Assert.Equal(1, ((AggregateException)ex).InnerExceptions.Count);

                Exception innerEx = ((AggregateException)ex).InnerExceptions[0];
                Assert.Equal(typeof(DigitalTwinDeviceInterfaceNotRegisteredException).FullName, innerEx.GetType().FullName);
                Assert.True(innerEx.Message?.Equals("The interface instanceName is not registered.", StringComparison.Ordinal) ?? false);
                return;
            }

            Assert.True(false);
        }

        [Fact]
        public void TestReportPropertiesAsyncWillCallToDigitalTwinClient()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName", true, true);

            var properties = new List<DigitalTwinPropertyReport>();
            properties.Add(new DigitalTwinPropertyReport("propertyName1", "propertyValue1"));

            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString("HostName=zzz.azure-devices.net;DeviceId=aaaa;SharedAccessKey=WWWWWWWWWWWWWWWW/WWWWWWWWWWWWWWWWWWWWWWWWWW=");
            var digitalTwinClientMock = Substitute.For<DigitalTwinClient>(deviceClient);

            digitalTwinClientMock.ReportPropertiesAsync("instanceName", properties, CancellationToken.None).ReturnsForAnyArgs(Task.CompletedTask);
            client.Initialize(digitalTwinClientMock);

            client.ReportPropertiesAsync(properties).Wait();

            digitalTwinClientMock.Received().ReportPropertiesAsync("instanceName", properties, CancellationToken.None).Wait();
        }

        public static IEnumerable<object[]> GetInvalidInterfaceIdData =>
            new List<object[]>
            {
                new object[] { "urN:iwoerWE:RE309_4" },
                new object[] { "ur:iwoerWER:RE309_4" },
                new object[] { "urn:iwoerWER:RE309!4" },
                new object[] { $"urn:{new string('A', 253)}" },
            };

        public static IEnumerable<object[]> GetInvalidInterfaceInstanceNameData =>
            new List<object[]>
            {
                new object[] { "iwoerWE!" },
                new object[] { "ur:iwoerWER_4" },
                new object[] { $"{new string('A', 257)}" },
            };
    }
}
