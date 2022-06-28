// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Http2;
using Microsoft.Azure.Devices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class RegistryClientTests
    {
        private const string IotHubName = "acme";
        private const string validMockConnectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=dGVzdFN0cmluZzE=";

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ConstructorWithInvalidConnectionStringTest()
        {
            new RegistryClient(string.Empty);
        }

        [TestMethod]
        public void ConstructorWithValidConnectionStringTest()
        {
            var authMethod = new ServiceAuthenticationWithSharedAccessPolicyKey("test", "dGVzdFN0cmluZzE=");
            var builder = IotHubConnectionStringBuilder.Create("acme.azure-devices.net", authMethod);
            var RegistryClient = new RegistryClient(builder.ToString());
            Assert.IsNotNull(RegistryClient);
        }

        [TestMethod]
        public async Task GetDeviceAsyncTest()
        {
            const string DeviceId = "123";
            var deviceToReturn = new Device(DeviceId) { ConnectionState = DeviceConnectionState.Connected };
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = HttpMessageHelper2.SerializePayload(deviceToReturn);
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);
            settings.HttpClient = mockHttpClient.Object;

            var RegistryClient = new RegistryClient(validMockConnectionString, settings);
            var device = await RegistryClient.GetDeviceAsync(DeviceId).ConfigureAwait(false);
            Assert.AreEqual(deviceToReturn.Id, device.Id);
            mockHttpClient.VerifyAll();
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public async Task GetDeviceAsyncWithNullDeviceIdTest()
        {
            var restOpMock = new Mock<IHttpClientHelper>();
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = new Mock<HttpClient>().Object;
            var RegistryClient = new RegistryClient(validMockConnectionString, settings);
            await RegistryClient.GetDeviceAsync(null).ConfigureAwait(false);
            Assert.Fail("Calling GetDeviceAsync with null device id did not throw an exception.");
        }

        [TestMethod]
        public async Task RegisterDeviceAsyncTest()
        {
            var deviceToReturn = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = HttpMessageHelper2.SerializePayload(deviceToReturn);
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);
            settings.HttpClient = mockHttpClient.Object;

            var RegistryClient = new RegistryClient(validMockConnectionString, settings);
            var returnedDevice = await RegistryClient.AddDeviceAsync(deviceToReturn).ConfigureAwait(false);
            Assert.AreEqual(deviceToReturn.Id, returnedDevice.Id);
            mockHttpClient.VerifyAll();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task RegisterDeviceAsyncWithNullDeviceTest()
        {
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = new Mock<HttpClient>().Object;
            var RegistryClient = new RegistryClient(validMockConnectionString, settings);
            await RegistryClient.AddDeviceAsync(null).ConfigureAwait(false);
            Assert.Fail("RegisterDevice API did not throw exception when the device parameter was null.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task RegisterDevicesAsyncWithETagsSetTest()
        {
            var goodDevice = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            var badDevice = new Device("234") { ConnectionState = DeviceConnectionState.Connected, ETag = "234" };
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = new Mock<HttpClient>().Object;
            var RegistryClient = new RegistryClient(validMockConnectionString, settings);
            await RegistryClient.AddDevicesAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("RegisterDevices API did not throw exception when ETag was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task RegisterDevicesAsyncWithNullDeviceTest()
        {
            var goodDevice = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            Device badDevice = null;
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = new Mock<HttpClient>().Object;
            var RegistryClient = new RegistryClient(validMockConnectionString, settings);
            await RegistryClient.AddDevicesAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("RegisterDevices API did not throw exception when Null device was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task RegisterDevicesAsyncWithNullDeviceListTest()
        {
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = new Mock<HttpClient>().Object;
            var RegistryClient = new RegistryClient(validMockConnectionString, settings);
            await RegistryClient.AddDevicesAsync(new List<Device>()).ConfigureAwait(false);
            Assert.Fail("RegisterDevices API did not throw exception when Null device list was used.");
        }

        [TestMethod]
        public async Task UpdateDeviceAsyncTest()
        {
            var deviceToReturn = new Device("123") { ConnectionState = DeviceConnectionState.Connected, ETag = "123" };
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = HttpMessageHelper2.SerializePayload(deviceToReturn);
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);
            settings.HttpClient = mockHttpClient.Object;

            var RegistryClient = new RegistryClient(validMockConnectionString, settings);
            var returnedDevice = await RegistryClient.UpdateDeviceAsync(deviceToReturn).ConfigureAwait(false);
            Assert.AreEqual(deviceToReturn.Id, returnedDevice.Id);
            mockHttpClient.VerifyAll();
        }

        private Device PrepareTestDevice(int batteryLevel, string firmwareVersion)
        {
            var deviceToReturn = new Device("Device123");
            return deviceToReturn;
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public async Task UpdateDeviceWithNullDeviceTest()
        {
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = new Mock<HttpClient>().Object;
            var RegistryClient = new RegistryClient(validMockConnectionString, settings);
            await RegistryClient.UpdateDeviceAsync(null).ConfigureAwait(false);
            Assert.Fail("UpdateDevice api did not throw exception when the device parameter was null.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateDevicesAsyncWithInvalidDeviceIdTest()
        {
            var goodDevice = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            var badDevice = new Device("/baddevice") { ConnectionState = DeviceConnectionState.Connected };
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = new Mock<HttpClient>().Object;
            var RegistryClient = new RegistryClient(validMockConnectionString, settings);
            await RegistryClient.UpdateDevicesAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("UpdateDevices API did not throw exception when bad deviceid was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateDevicesAsyncWithETagMissingTest()
        {
            var goodDevice = new Device("123") { ConnectionState = DeviceConnectionState.Connected, ETag = "234" };
            var badDevice = new Device("234") { ConnectionState = DeviceConnectionState.Connected };
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = new Mock<HttpClient>().Object;
            var RegistryClient = new RegistryClient(validMockConnectionString, settings);
            await RegistryClient.UpdateDevicesAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("UpdateDevices API did not throw exception when ETag was null.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task UpdateDevicesAsyncWithNullDeviceTest()
        {
            var goodDevice = new Device("123") { ConnectionState = DeviceConnectionState.Connected, ETag = "234" };
            Device badDevice = null;
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = new Mock<HttpClient>().Object;
            var RegistryClient = new RegistryClient(validMockConnectionString, settings);
            await RegistryClient.UpdateDevicesAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("UpdateDevices API did not throw exception when Null device was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateDevicesAsyncWithNullDeviceListTest()
        {
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = new Mock<HttpClient>().Object;
            var RegistryClient = new RegistryClient(validMockConnectionString, settings);
            await RegistryClient.UpdateDevicesAsync(new List<Device>()).ConfigureAwait(false);
            Assert.Fail("UpdateDevices API did not throw exception when Null device list was used.");
        }

        [TestMethod]
        public async Task UpdateDevicesAsyncForceUpdateTest()
        {
            var goodDevice1 = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            var goodDevice2 = new Device("234") { ConnectionState = DeviceConnectionState.Connected };
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = null;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);
            settings.HttpClient = mockHttpClient.Object;

            var RegistryClient = new RegistryClient(validMockConnectionString, settings);
            await RegistryClient.UpdateDevicesAsync(new List<Device>() { goodDevice1, goodDevice2 }, true, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateDevicesAsyncForceUpdateMissingETagTest()
        {
            var badDevice1 = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            var badDevice2 = new Device("234") { ConnectionState = DeviceConnectionState.Connected };
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = null;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);
            settings.HttpClient = mockHttpClient.Object;

            var RegistryClient = new RegistryClient(validMockConnectionString, settings);
            await RegistryClient.UpdateDevicesAsync(new List<Device>() { badDevice1, badDevice2 }, false, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task UpdateDevicesAsyncForceUpdateFalseTest()
        {
            var goodDevice1 = new Device("123") { ConnectionState = DeviceConnectionState.Connected, ETag = "234" };
            var goodDevice2 = new Device("234") { ConnectionState = DeviceConnectionState.Connected, ETag = "123" };
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = null;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);
            settings.HttpClient = mockHttpClient.Object;

            var RegistryClient = new RegistryClient(validMockConnectionString, settings);
            await RegistryClient.UpdateDevicesAsync(new List<Device>() { goodDevice1, goodDevice2 }, false, CancellationToken.None).ConfigureAwait(false);
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public async Task DeleteDeviceAsyncWithNullIdTest()
        {
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = new Mock<HttpClient>().Object;
            var RegistryClient = new RegistryClient(validMockConnectionString, settings);
            await RegistryClient.RemoveDeviceAsync(string.Empty).ConfigureAwait(false);
            Assert.Fail("Delete API did not throw exception when the device id was null.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeleteDevicesAsyncWithInvalidDeviceIdTest()
        {
            var goodDevice = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            var badDevice = new Device("/baddevice") { ConnectionState = DeviceConnectionState.Connected };
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = new Mock<HttpClient>().Object;
            var RegistryClient = new RegistryClient(validMockConnectionString, settings);
            await RegistryClient.RemoveDevicesAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("DeleteDevices API did not throw exception when bad deviceid was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeleteDevicesAsyncWithETagMissingTest()
        {
            var goodDevice = new Device("123") { ConnectionState = DeviceConnectionState.Connected, ETag = "234" };
            var badDevice = new Device("234") { ConnectionState = DeviceConnectionState.Connected };
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = new Mock<HttpClient>().Object;
            var RegistryClient = new RegistryClient(validMockConnectionString, settings);
            await RegistryClient.RemoveDevicesAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("DeleteDevices API did not throw exception when ETag was null.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task DeleteDevicesAsyncWithNullDeviceTest()
        {
            var goodDevice = new Device("123") { ConnectionState = DeviceConnectionState.Connected, ETag = "234" };
            Device badDevice = null;
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = new Mock<HttpClient>().Object;
            var RegistryClient = new RegistryClient(validMockConnectionString, settings);
            await RegistryClient.RemoveDevicesAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("DeleteDevices API did not throw exception when Null device was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeleteDevicesAsyncWithNullDeviceListTest()
        {
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = new Mock<HttpClient>().Object;
            var RegistryClient = new RegistryClient(validMockConnectionString, settings);
            await RegistryClient.RemoveDevicesAsync(new List<Device>()).ConfigureAwait(false);
            Assert.Fail("DeleteDevices API did not throw exception when Null device list was used.");
        }

        [TestMethod]
        public async Task DeleteDevicesAsyncForceDeleteTest()
        {
            var goodDevice1 = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            var goodDevice2 = new Device("234") { ConnectionState = DeviceConnectionState.Connected };
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = null;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);
            settings.HttpClient = mockHttpClient.Object;

            var RegistryClient = new RegistryClient(validMockConnectionString, settings);
            await RegistryClient.RemoveDevicesAsync(new List<Device>() { goodDevice1, goodDevice2 }, true, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeleteDevicesAsyncForceDeleteFalseMissingETagTest()
        {
            var badDevice1 = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            var badDevice2 = new Device("234") { ConnectionState = DeviceConnectionState.Connected };
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = null;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);
            settings.HttpClient = mockHttpClient.Object;

            var RegistryClient = new RegistryClient(validMockConnectionString, settings);
            await RegistryClient.RemoveDevicesAsync(new List<Device>() { badDevice1, badDevice2 }, false, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeleteDevicesAsyncForceDeleteFalseTest()
        {
            var goodDevice1 = new Device("123") { ConnectionState = DeviceConnectionState.Connected, ETag = "234" };
            var goodDevice2 = new Device("234") { ConnectionState = DeviceConnectionState.Connected, ETag = "123" };
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = null;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);
            settings.HttpClient = mockHttpClient.Object;

            var RegistryClient = new RegistryClient(validMockConnectionString, settings);
            await RegistryClient.RemoveDevicesAsync(new List<Device>() { goodDevice1, goodDevice2 }, false, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateTwinsAsyncWithInvalidDeviceIdTest()
        {
            var goodTwin = new Twin("123");
            var badTwin = new Twin("/badTwin");
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(validMockConnectionString, restOpMock.Object);
            await registryManager.UpdateTwinsAsync(new List<Twin>() { goodTwin, badTwin }).ConfigureAwait(false);
            Assert.Fail("UpdateTwins API did not throw exception when bad deviceid was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateTwinsAsyncWithETagMissingTest()
        {
            var goodTwin = new Twin("123") { ETag = "234" };
            var badTwin = new Twin("234");
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(validMockConnectionString, restOpMock.Object);
            await registryManager.UpdateTwinsAsync(new List<Twin>() { goodTwin, badTwin }).ConfigureAwait(false);
            Assert.Fail("UpdateTwins API did not throw exception when ETag was null.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task UpdateTwinsAsyncWithNullTwinTest()
        {
            var goodTwin = new Twin("123") { ETag = "234" };
            Twin badTwin = null;
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(validMockConnectionString, restOpMock.Object);
            await registryManager.UpdateTwinsAsync(new List<Twin>() { goodTwin, badTwin }).ConfigureAwait(false);
            Assert.Fail("UpdateTwins API did not throw exception when Null twin was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateTwinsAsyncWithNullTwinListTest()
        {
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(validMockConnectionString, restOpMock.Object);
            await registryManager.UpdateTwinsAsync(new List<Twin>()).ConfigureAwait(false);
            Assert.Fail("UpdateTwins API did not throw exception when Null twin list was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateTwinsAsyncWithDeviceIdNullTest()
        {
            var goodTwin = new Twin("123") { ETag = "234" };
            var badTwin = new Twin();
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(validMockConnectionString, restOpMock.Object);
            await registryManager.UpdateTwinsAsync(new List<Twin>() { goodTwin, badTwin }).ConfigureAwait(false);
            Assert.Fail("UpdateTwins API did not throw exception when deviceId was null.");
        }

        [TestMethod]
        public async Task UpdateTwinsAsyncForceUpdateTest()
        {
            var goodTwin1 = new Twin("123");
            var goodTwin2 = new Twin("234");
            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(restOp => restOp
                .PostAsync<IEnumerable<ExportImportDevice>, Task<BulkRegistryOperationResult>>(
                    It.IsAny<Uri>(),
                    It.IsAny<IEnumerable<ExportImportDevice>>(),
                    It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Task<BulkRegistryOperationResult>)null);

            var registryManager = new RegistryManager(validMockConnectionString, restOpMock.Object);
            await registryManager.UpdateTwinsAsync(new List<Twin>() { goodTwin1, goodTwin2 }, true, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateTwinsAsyncForceUpdateMissingETagTest()
        {
            var badTwin1 = new Twin("123");
            var badTwin2 = new Twin("234");
            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(restOp => restOp
                .PostAsync<IEnumerable<ExportImportDevice>, Task<BulkRegistryOperationResult>>(
                It.IsAny<Uri>(),
                It.IsAny<IEnumerable<ExportImportDevice>>(),
                It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((Task<BulkRegistryOperationResult>)null);

            var registryManager = new RegistryManager(validMockConnectionString, restOpMock.Object);
            await registryManager.UpdateTwinsAsync(new List<Twin>() { badTwin1, badTwin2 }, false, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task UpdateTwinsAsyncForceUpdateFalseTest()
        {
            var goodTwin1 = new Twin("123") { ETag = "234" };
            var goodTwin2 = new Twin("234") { ETag = "123" };
            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(restOp => restOp
                .PostAsync<IEnumerable<ExportImportDevice>, Task<BulkRegistryOperationResult>>(
                It.IsAny<Uri>(),
                It.IsAny<IEnumerable<ExportImportDevice>>(),
                It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((Task<BulkRegistryOperationResult>)null);

            var registryManager = new RegistryManager(validMockConnectionString, restOpMock.Object);
            await registryManager.UpdateTwinsAsync(new List<Twin>() { goodTwin1, goodTwin2 }, false, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public void DisposeTest()
        {
            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(restOp => restOp.Dispose());

            var registryManager = new RegistryManager(validMockConnectionString, restOpMock.Object);
            registryManager.Dispose();
            restOpMock.Verify(restOp => restOp.Dispose(), Times.Once());
        }

        [TestMethod]
        public async Task CloseAsyncTest()
        {
            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(restOp => restOp.Dispose());

            var registryManager = new RegistryManager(validMockConnectionString, restOpMock.Object);
            await registryManager.CloseAsync().ConfigureAwait(false);
            restOpMock.Verify(restOp => restOp.Dispose(), Times.Never());
        }

        [TestMethod]
        public void Twin_ParentScopes_NotNull()
        {
            var twin = new Twin();
            twin.ParentScopes.Should().NotBeNull("To prevent NREs because a property was unexecptedly null, it should have a default list instance assigned.");
            twin.ParentScopes.Should().BeEmpty("The default list instance should be empty.");
        }
    }
}
