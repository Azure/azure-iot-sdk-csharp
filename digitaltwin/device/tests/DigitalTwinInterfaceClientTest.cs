// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.IoT.DigitalTwin.Device;
using Microsoft.Azure.IoT.DigitalTwin.Device.Exceptions;
using Microsoft.Azure.IoT.DigitalTwin.Device.Helper;
using Microsoft.Azure.IoT.DigitalTwin.Device.Model;
using Microsoft.Azure.Devices.Client;
using NSubstitute;
using Xunit;

namespace Microsoft.Azure.IoT.DigitalTwin.Device.Test
{
    public class DigitalTwinInterfaceClientTest
    {
        [Fact]
        public void TestConstructorWhenIdIsNull()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new DigitalTwinInterfaceTestClient(null, "testInstanceName"));

            Assert.Equal("id", ex.ParamName);
            Assert.StartsWith("The parameter named id can't be null, empty string or white space.", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void TestConstructorWhenIdIsWhiteSpace()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new DigitalTwinInterfaceTestClient(" ", "testInstanceName"));

            Assert.Equal("id", ex.ParamName);
            Assert.StartsWith("The parameter named id can't be null, empty string or white space.", ex.Message, StringComparison.Ordinal);
        }

        [Theory]
        [MemberData(nameof(GetInvalidInterfaceIdData))]
        public void TestConstructorWhenIdFormatIsInvalid(string id)
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(() => new DigitalTwinInterfaceTestClient(id, "testInstanceName"));

            Assert.Equal("id", ex.ParamName);
            Assert.StartsWith(DigitalTwinConstants.InvalidInterfaceIdErrorMessage, ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void TestConstructorWhenInstanceNameIsNull()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new DigitalTwinInterfaceTestClient("urn:testId", null));

            Assert.Equal("instanceName", ex.ParamName);
            Assert.StartsWith("The parameter named instanceName can't be null, empty string or white space.", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void TestConstructorWhenInstanceNameIsWhiteSpace()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new DigitalTwinInterfaceTestClient("urn:testId", " "));

            Assert.Equal("instanceName", ex.ParamName);
            Assert.StartsWith("The parameter named instanceName can't be null, empty string or white space.", ex.Message, StringComparison.Ordinal);
        }

        [Theory]
        [MemberData(nameof(GetInvalidInterfaceInstanceNameData))]
        public void TestConstructorWhenInstanceNameFormatIsInvalid(string instanceName)
        {
                var ex = Assert.Throws<ArgumentException>(() => new DigitalTwinInterfaceTestClient("urn:testId", instanceName));

                Assert.Equal(ex.ParamName, "instanceName");
                Assert.StartsWith(DigitalTwinConstants.InvalidInterfaceInstanceNameErrorMessage, ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void TestConstructorWithValidInputs()
        {
            string id = "urn:testId";
            string instanceName = "testInstanceName";
            bool isCommandEnabled = true;
            bool isPropertyUpdatedEnabled = true;

            var client = new DigitalTwinInterfaceTestClient(id, instanceName);

            Assert.Equal(id, client.Id);
            Assert.Equal(instanceName, client.InstanceName);
        }

        [Fact]
        public void TestOnCommandRequest()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName");

            Task<DigitalTwinCommandResponse> response = client.OnCommandRequest(new DigitalTwinCommandRequest());

            Assert.Equal(404, response.Result.Status);
            Assert.Null(response.Result.Payload);
        }

        [Fact]
        public void TestOnPropertyUpdated()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName");

            Task updateTask = client.OnPropertyUpdated(new DigitalTwinPropertyUpdate());

            Assert.Equal(TaskStatus.RanToCompletion, updateTask.Status);
        }

        [Fact]
        public void TestOnRegistrationCompleted()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName");
            ILogging logger = Substitute.For<ILogging>();
            Logging.Instance = logger;

            client.OnRegistrationCompleted();

            logger.Received().LogInformational("DigitalTwinInterfaceClient registered.", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>());
        }

        [Fact]
        public async Task TestReportPropertiesAsyncWhenPropertiesIsNull()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName");

            ArgumentNullException ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.ReportPropertiesAsync(null)).ConfigureAwait(false);
            Assert.Equal("properties", ex.ParamName);
            Assert.StartsWith("The parameter named properties can't be null.", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task TestReportPropertiesAsyncWhenInterfaceNotRegistered()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName");
            IList<DigitalTwinPropertyReport> properties = new List<DigitalTwinPropertyReport>();
            properties.Add(new DigitalTwinPropertyReport("propertyName1", "propertyValue1"));

            DigitalTwinDeviceInterfaceNotRegisteredException ex =
                await Assert.ThrowsAsync<DigitalTwinDeviceInterfaceNotRegisteredException>(() => client.ReportPropertiesAsync(properties)).ConfigureAwait(false);
            Assert.Equal("The interface instanceName is not registered.", ex.Message);
        }

        [Fact]
        public async Task TestReportPropertiesAsyncCallDigitalTwinClient()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName");
            var properties = new List<DigitalTwinPropertyReport>();
            properties.Add(new DigitalTwinPropertyReport("propertyName1", "propertyValue1"));
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString("HostName=zzz.azure-devices.net;DeviceId=aaaa;SharedAccessKey=WWWWWWWWWWWWWWWW/WWWWWWWWWWWWWWWWWWWWWWWWWW=");
            var digitalTwinClientMock = Substitute.For<DigitalTwinClient>(deviceClient);
            digitalTwinClientMock.ReportPropertiesAsync("instanceName", properties, CancellationToken.None).ReturnsForAnyArgs(Task.CompletedTask);
            client.Initialize(digitalTwinClientMock);

            await client.ReportPropertiesAsync(properties).ConfigureAwait(false);

            digitalTwinClientMock.Received().ReportPropertiesAsync("instanceName", properties, CancellationToken.None).Wait();
        }

        [Fact]
        public async Task TestSendTelemetryAsyncWhenTelemetryNameIsNull()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName");

            ArgumentNullException ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.SendTelemetryAsync(null, "telemetryValue")).ConfigureAwait(false);
            Assert.Equal("telemetryName", ex.ParamName);
            Assert.StartsWith("The parameter named telemetryName can't be null, empty string or white space.", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task TestSendTelemetryAsyncWhenTelemetryValueIsNull()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName");

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.SendTelemetryAsync("telemetryName", null)).ConfigureAwait(false);
            Assert.Equal("telemetryValue", ex.ParamName);
            Assert.StartsWith("The parameter named telemetryValue can't be null, empty string or white space.", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task TestSendTelemetryAsyncWhenInterfaceNotRegistered()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName");

            var ex = await Assert.ThrowsAsync<DigitalTwinDeviceInterfaceNotRegisteredException>(() => client.SendTelemetryAsync("telemetryName", "telemetryValue")).ConfigureAwait(false);
            Assert.Equal("The interface instanceName is not registered.", ex.Message);
        }

        [Fact]
        public void TestSendTelemetryAsyncCallDigitalTwinClient()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName");

            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString("HostName=zzz.azure-devices.net;DeviceId=aaaa;SharedAccessKey=WWWWWWWWWWWWWWWW/WWWWWWWWWWWWWWWWWWWWWWWWWW=");
            var digitalTwinClientMock = Substitute.For<DigitalTwinClient>(deviceClient);

            digitalTwinClientMock.SendTelemetryAsync("urn:id", "instanceName", "telemetryName", "telemetryValue", CancellationToken.None).ReturnsForAnyArgs(Task.CompletedTask);
            client.Initialize(digitalTwinClientMock);

            client.SendTelemetryAsync("telemetryName", "telemetryValue").Wait();

            digitalTwinClientMock.Received().SendTelemetryAsync("urn:id", "instanceName", "telemetryName", "telemetryValue", CancellationToken.None).Wait();
        }

        [Fact]
        public async Task TestUpdateAsyncCommandStatusAsyncWhenUpdateNameIsNull()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName");
            var cmdUpdate = new DigitalTwinAsyncCommandUpdate(null, "TestReqId", 999, "TestPayload");

            ArgumentNullException ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateAsyncCommandStatusAsync(cmdUpdate)).ConfigureAwait(false);
            Assert.Equal("DigitalTwinAsyncCommandUpdate.Name", ex.ParamName);
            Assert.StartsWith("The parameter named DigitalTwinAsyncCommandUpdate.Name can't be null, empty string or white space.", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task TestUpdateAsyncCommandStatusAsyncWhenUpdateRequestIdIsNull()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName");
            var cmdUpdate = new DigitalTwinAsyncCommandUpdate("TestName", null, 999, "TestPayload");

            ArgumentNullException ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateAsyncCommandStatusAsync(cmdUpdate)).ConfigureAwait(false);
            Assert.Equal("DigitalTwinAsyncCommandUpdate.RequestId", ex.ParamName);
            Assert.StartsWith("The parameter named DigitalTwinAsyncCommandUpdate.RequestId can't be null, empty string or white space.", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task TestUpdateAsyncCommandStatusAsyncWhenInterfaceNotRegistered()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName");
            var cmdUpdate = new DigitalTwinAsyncCommandUpdate("TestName", "TestRequestId", 999, "TestPayload");

            var ex = await Assert.ThrowsAsync<DigitalTwinDeviceInterfaceNotRegisteredException>(() => client.UpdateAsyncCommandStatusAsync(cmdUpdate)).ConfigureAwait(false);
            Assert.Equal("The interface instanceName is not registered.", ex.Message);
        }

        [Fact]
        public void TestUpdateAsyncCommandStatusAsyncCallDigitalTwinClient()
        {
            var client = new DigitalTwinInterfaceTestClient("urn:id", "instanceName");
            var cmdUpdate = new DigitalTwinAsyncCommandUpdate("TestName", "TestReqId", 999, "TestPayload");

            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString("HostName=zzz.azure-devices.net;DeviceId=aaaa;SharedAccessKey=WWWWWWWWWWWWWWWW/WWWWWWWWWWWWWWWWWWWWWWWWWW=");
            var digitalTwinClientMock = Substitute.For<DigitalTwinClient>(deviceClient);

            digitalTwinClientMock.UpdateAsyncCommandStatusAsync("urn:id", "instanceName", cmdUpdate, CancellationToken.None).ReturnsForAnyArgs(Task.CompletedTask);
            client.Initialize(digitalTwinClientMock);

            client.UpdateAsyncCommandStatusAsync(cmdUpdate).Wait();

            digitalTwinClientMock.Received().UpdateAsyncCommandStatusAsync("urn:id", "instanceName", cmdUpdate, CancellationToken.None).Wait();
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
