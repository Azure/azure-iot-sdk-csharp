// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Api.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    [TestCategory("Unit")]
    public class RegistryManagerTests
    {
        private const string IotHubName = "acme";

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructorWithInvalidConnectionStringTest()
        {
            RegistryManager.CreateFromConnectionString(string.Empty);
        }

        [TestMethod]
        public void ConstructorWithValidConnectionStringTest()
        {
            var authMethod = new ServiceAuthenticationWithSharedAccessPolicyKey("test", "dGVzdFN0cmluZzE=");
            var builder = IotHubConnectionStringBuilder.Create("acme.azure-devices.net", authMethod);
            var registryManager = RegistryManager.CreateFromConnectionString(builder.ToString());
            Assert.IsNotNull(registryManager);
        }

        [TestMethod]
        public async Task GetDeviceAsyncTest()
        {
            const string DeviceId = "123";
            var deviceToReturn = new Device(DeviceId) { ConnectionState = DeviceConnectionState.Connected };
            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(restOp => restOp.GetAsync<Device>(It.IsAny<Uri>(), It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(), null, false, It.IsAny<CancellationToken>())).ReturnsAsync(deviceToReturn);

            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            var device = await registryManager.GetDeviceAsync(DeviceId).ConfigureAwait(false);
            Assert.AreSame(deviceToReturn, device);
            restOpMock.VerifyAll();
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public async Task GetDeviceAsyncWithNullDeviceIdTest()
        {
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.GetDeviceAsync(null).ConfigureAwait(false);
            Assert.Fail("Calling GetDeviceAsync with null device id did not throw an exception.");
        }

        [TestMethod]
        public async Task RegisterDeviceAsyncTest()
        {
            var deviceToReturn = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(restOp => restOp.PutAsync(It.IsAny<Uri>(), It.IsAny<Device>(), It.IsAny<PutOperationType>(), It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(deviceToReturn);

            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            var returnedDevice = await registryManager.AddDeviceAsync(deviceToReturn).ConfigureAwait(false);
            Assert.AreSame(deviceToReturn, returnedDevice);
            restOpMock.VerifyAll();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task RegisterDeviceAsyncWithInvalidDeviceIdTest()
        {
            var deviceToReturn = new Device("/baddevice") { ConnectionState = DeviceConnectionState.Connected, ETag = "123" };
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDeviceAsync(deviceToReturn).ConfigureAwait(false);
            Assert.Fail("RegisterDevice API did not throw exception when bad deviceid was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task RegisterDeviceAsyncWithETagSetTest()
        {
            var deviceToReturn = new Device("123") { ConnectionState = DeviceConnectionState.Connected, ETag = "123" };
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDeviceAsync(deviceToReturn).ConfigureAwait(false);
            Assert.Fail("RegisterDevice API did not throw exception when ETag was set.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task RegisterDeviceAsyncWithNullDeviceTest()
        {
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDeviceAsync(null).ConfigureAwait(false);
            Assert.Fail("RegisterDevice API did not throw exception when the device parameter was null.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task RegisterDeviceAsyncWithDeviceIdNullTest()
        {
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDeviceAsync(new Device()).ConfigureAwait(false);
            Assert.Fail("RegisterDevice API did not throw exception when the device's id was not set.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task RegisterDevicesAsyncWithInvalidDeviceIdTest()
        {
            var goodDevice = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            // '/' is not a valid character in DeviceId
            var badDevice = new Device("/baddevice") { ConnectionState = DeviceConnectionState.Connected };
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDevicesAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("RegisterDevices API did not throw exception when bad deviceid was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task RegisterDevicesAsyncWithETagsSetTest()
        {
            var goodDevice = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            var badDevice = new Device("234") { ConnectionState = DeviceConnectionState.Connected, ETag = "234" };
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDevicesAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("RegisterDevices API did not throw exception when ETag was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task RegisterDevicesAsyncWithNullDeviceTest()
        {
            var goodDevice = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            Device badDevice = null;
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDevicesAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("RegisterDevices API did not throw exception when Null device was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task RegisterDevicesAsyncWithNullDeviceListTest()
        {
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDevicesAsync(new List<Device>()).ConfigureAwait(false);
            Assert.Fail("RegisterDevices API did not throw exception when Null device list was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task RegisterDevicesAsyncWithDeviceIdNullTest()
        {
            var goodDevice = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            var badDevice = new Device();
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.AddDevicesAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("RegisterDevices API did not throw exception when deviceId was null.");
        }

        [TestMethod]
        public async Task UpdateDeviceAsyncTest()
        {
            var deviceToReturn = new Device("123") { ConnectionState = DeviceConnectionState.Connected, ETag = "123" };
            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(restOp => restOp.PutAsync(It.IsAny<Uri>(), It.IsAny<Device>(), It.IsAny<PutOperationType>(), It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(deviceToReturn);

            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            var returnedDevice = await registryManager.UpdateDeviceAsync(deviceToReturn).ConfigureAwait(false);
            Assert.AreSame(deviceToReturn, returnedDevice);
            restOpMock.VerifyAll();
        }

        private Device PrepareTestDevice(int batteryLevel, string firmwareVersion)
        {
            Device deviceToReturn = new Device("Device123");
            return deviceToReturn;
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public async Task UpdateDeviceWithNullDeviceTest()
        {
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.UpdateDeviceAsync(null).ConfigureAwait(false);
            Assert.Fail("UpdateDevice api did not throw exception when the device parameter was null.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateDeviceWithDeviceIdNullTest()
        {
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.UpdateDeviceAsync(new Device() { ETag = "*" }).ConfigureAwait(false);
            Assert.Fail("UpdateDevice api did not throw exception when the device's id was null.");
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public async Task UpdateDeviceWithInvalidDeviceIdTest()
        {
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            // '/' is not a valid char in DeviceId
            await registryManager.UpdateDeviceAsync(new Device("/baddevice") { ETag = "*" }).ConfigureAwait(false);
            Assert.Fail("UpdateDevice api did not throw exception when the deviceid was invalid.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateDevicesAsyncWithInvalidDeviceIdTest()
        {
            var goodDevice = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            var badDevice = new Device("/baddevice") { ConnectionState = DeviceConnectionState.Connected };
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.UpdateDevicesAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("UpdateDevices API did not throw exception when bad deviceid was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateDevicesAsyncWithETagMissingTest()
        {
            var goodDevice = new Device("123") { ConnectionState = DeviceConnectionState.Connected, ETag = "234" };
            var badDevice = new Device("234") { ConnectionState = DeviceConnectionState.Connected };
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.UpdateDevicesAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("UpdateDevices API did not throw exception when ETag was null.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task UpdateDevicesAsyncWithNullDeviceTest()
        {
            var goodDevice = new Device("123") { ConnectionState = DeviceConnectionState.Connected, ETag = "234" };
            Device badDevice = null;
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.UpdateDevicesAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("UpdateDevices API did not throw exception when Null device was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateDevicesAsyncWithNullDeviceListTest()
        {
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.UpdateDevicesAsync(new List<Device>()).ConfigureAwait(false);
            Assert.Fail("UpdateDevices API did not throw exception when Null device list was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateDevicesAsyncWithDeviceIdNullTest()
        {
            var goodDevice = new Device("123") { ConnectionState = DeviceConnectionState.Connected, ETag = "234" };
            var badDevice = new Device();
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.UpdateDevicesAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("UpdateDevices API did not throw exception when deviceId was null.");
        }

        [TestMethod]
        public async Task UpdateDevicesAsyncForceUpdateTest()
        {
            var goodDevice1 = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            var goodDevice2 = new Device("234") { ConnectionState = DeviceConnectionState.Connected };
            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(restOp => restOp.PostAsync<IEnumerable<ExportImportDevice>, Task<BulkRegistryOperationResult>>(It.IsAny<Uri>(), It.IsAny<IEnumerable<ExportImportDevice>>(), It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Task<BulkRegistryOperationResult>)null);

            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.UpdateDevicesAsync(new List<Device>() { goodDevice1, goodDevice2 }, true, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateDevicesAsyncForceUpdateMissingETagTest()
        {
            var badDevice1 = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            var badDevice2 = new Device("234") { ConnectionState = DeviceConnectionState.Connected };
            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(restOp => restOp.PostAsync<IEnumerable<ExportImportDevice>, Task<BulkRegistryOperationResult>>(It.IsAny<Uri>(), It.IsAny<IEnumerable<ExportImportDevice>>(), It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Task<BulkRegistryOperationResult>)null);

            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.UpdateDevicesAsync(new List<Device>() { badDevice1, badDevice2 }, false, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task UpdateDevicesAsyncForceUpdateFalseTest()
        {
            var goodDevice1 = new Device("123") { ConnectionState = DeviceConnectionState.Connected, ETag = "234" };
            var goodDevice2 = new Device("234") { ConnectionState = DeviceConnectionState.Connected, ETag = "123" };
            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(restOp => restOp.PostAsync<IEnumerable<ExportImportDevice>, Task<BulkRegistryOperationResult>>(It.IsAny<Uri>(), It.IsAny<IEnumerable<ExportImportDevice>>(), It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Task<BulkRegistryOperationResult>)null);

            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.UpdateDevicesAsync(new List<Device>() { goodDevice1, goodDevice2 }, false, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeleteDeviceAsyncTest()
        {
            var restOpMock = new Mock<IHttpClientHelper>();
            var mockETag = new ETagHolder() { ETag = "*" };
            restOpMock.Setup(restOp => restOp.DeleteAsync(It.IsAny<Uri>(), mockETag, It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(), null, It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.RemoveDeviceAsync(new Device()).ConfigureAwait(false);
            restOpMock.VerifyAll();
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public async Task DeleteDeviceAsyncWithNullIdTest()
        {
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.RemoveDeviceAsync(string.Empty).ConfigureAwait(false);
            Assert.Fail("Delete API did not throw exception when the device id was null.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeleteDevicesAsyncWithInvalidDeviceIdTest()
        {
            var goodDevice = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            var badDevice = new Device("/baddevice") { ConnectionState = DeviceConnectionState.Connected };
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.RemoveDevicesAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("DeleteDevices API did not throw exception when bad deviceid was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeleteDevicesAsyncWithETagMissingTest()
        {
            var goodDevice = new Device("123") { ConnectionState = DeviceConnectionState.Connected, ETag = "234" };
            var badDevice = new Device("234") { ConnectionState = DeviceConnectionState.Connected };
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.RemoveDevicesAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("DeleteDevices API did not throw exception when ETag was null.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task DeleteDevicesAsyncWithNullDeviceTest()
        {
            var goodDevice = new Device("123") { ConnectionState = DeviceConnectionState.Connected, ETag = "234" };
            Device badDevice = null;
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.RemoveDevicesAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("DeleteDevices API did not throw exception when Null device was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeleteDevicesAsyncWithNullDeviceListTest()
        {
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.RemoveDevicesAsync(new List<Device>()).ConfigureAwait(false);
            Assert.Fail("DeleteDevices API did not throw exception when Null device list was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeleteDevicesAsyncWithDeviceIdNullTest()
        {
            var goodDevice = new Device("123") { ConnectionState = DeviceConnectionState.Connected, ETag = "234" };
            var badDevice = new Device();
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.RemoveDevicesAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("DeleteDevices API did not throw exception when deviceId was null.");
        }

        [TestMethod]
        public async Task DeleteDevicesAsyncForceDeleteTest()
        {
            var goodDevice1 = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            var goodDevice2 = new Device("234") { ConnectionState = DeviceConnectionState.Connected };
            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(restOp => restOp.PostAsync<IEnumerable<ExportImportDevice>, Task<BulkRegistryOperationResult>>(It.IsAny<Uri>(), It.IsAny<IEnumerable<ExportImportDevice>>(), It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Task<BulkRegistryOperationResult>)null);

            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.RemoveDevicesAsync(new List<Device>() { goodDevice1, goodDevice2 }, true, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeleteDevicesAsyncForceDeleteFalseMissingETagTest()
        {
            var badDevice1 = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            var badDevice2 = new Device("234") { ConnectionState = DeviceConnectionState.Connected };
            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(restOp => restOp.PostAsync<IEnumerable<ExportImportDevice>, Task<BulkRegistryOperationResult>>(It.IsAny<Uri>(), It.IsAny<IEnumerable<ExportImportDevice>>(), It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Task<BulkRegistryOperationResult>)null);

            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.RemoveDevicesAsync(new List<Device>() { badDevice1, badDevice2 }, false, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeleteDevicesAsyncForceDeleteFalseTest()
        {
            var goodDevice1 = new Device("123") { ConnectionState = DeviceConnectionState.Connected, ETag = "234" };
            var goodDevice2 = new Device("234") { ConnectionState = DeviceConnectionState.Connected, ETag = "123" };
            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(restOp => restOp.PostAsync<IEnumerable<ExportImportDevice>, Task<BulkRegistryOperationResult>>(It.IsAny<Uri>(), It.IsAny<IEnumerable<ExportImportDevice>>(), It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Task<BulkRegistryOperationResult>)null);

            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.RemoveDevicesAsync(new List<Device>() { goodDevice1, goodDevice2 }, false, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateTwinsAsyncWithInvalidDeviceIdTest()
        {
            var goodTwin = new Twin("123");
            var badTwin = new Twin("/badTwin");
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
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
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
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
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.UpdateTwinsAsync(new List<Twin>() { goodTwin, badTwin }).ConfigureAwait(false);
            Assert.Fail("UpdateTwins API did not throw exception when Null twin was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateTwinsAsyncWithNullTwinListTest()
        {
            var restOpMock = new Mock<IHttpClientHelper>();
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
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
            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
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

            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
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

            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
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

            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            await registryManager.UpdateTwinsAsync(new List<Twin>() { goodTwin1, goodTwin2 }, false, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public void DisposeTest()
        {
            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(restOp => restOp.Dispose());

            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
            registryManager.Dispose();
            restOpMock.Verify(restOp => restOp.Dispose(), Times.Once());
        }

        [TestMethod]
        public async Task CloseAsyncTest()
        {
            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(restOp => restOp.Dispose());

            var registryManager = new RegistryManager(IotHubName, restOpMock.Object);
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
