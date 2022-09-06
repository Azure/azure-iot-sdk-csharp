// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class DevicesTests
    {
        private const string HostName = "acme.azure-devices.net";
        private static Uri HttpUri = new Uri("https://" + HostName);
        private const string validMockConnectionString = $"HostName={HostName};SharedAccessKeyName=iothubowner;SharedAccessKey=dGVzdFN0cmluZzE=";
        private const string validMockAuthenticationHeaderValue = $"SharedAccessSignature sr={HostName}&sig=thisIsFake&se=000000&skn=registryRead";

        [TestMethod]
        public async Task GetAsyncTest()
        {
            const string DeviceId = "123";
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var deviceToReturn = new Device(DeviceId) { ConnectionState = DeviceConnectionState.Connected };
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = HttpMessageHelper.SerializePayload(deviceToReturn);
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var DevicesClient = new DevicesClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory);
            var device = await DevicesClient.GetAsync(DeviceId).ConfigureAwait(false);
            Assert.AreEqual(deviceToReturn.Id, device.Id);
            mockHttpClient.VerifyAll();
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public async Task GetDeviceAsyncWithNullDeviceIdTest()
        {
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var DevicesClient = new DevicesClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory.Object);
            await DevicesClient.GetAsync(null).ConfigureAwait(false);
            Assert.Fail("Calling GetDeviceAsync with null device id did not throw an exception.");
        }

        [TestMethod]
        public async Task RegisterDeviceAsyncTest()
        {
            var deviceToReturn = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = HttpMessageHelper.SerializePayload(deviceToReturn);
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var DevicesClient = new DevicesClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory);
            var returnedDevice = await DevicesClient.CreateAsync(deviceToReturn).ConfigureAwait(false);
            Assert.AreEqual(deviceToReturn.Id, returnedDevice.Id);
            mockHttpClient.VerifyAll();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task RegisterDeviceAsyncWithNullDeviceTest()
        {
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var DevicesClient = new DevicesClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory.Object);
            await DevicesClient.CreateAsync((Device)null).ConfigureAwait(false);
            Assert.Fail("RegisterDevice API did not throw exception when the device parameter was null.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task RegisterDevicesAsyncWithETagsSetTest()
        {
            var goodDevice = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            var badDevice = new Device("234") { ConnectionState = DeviceConnectionState.Connected, ETag = new("234") };

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var DevicesClient = new DevicesClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory.Object);

            await DevicesClient.CreateAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("RegisterDevices API did not throw exception when ETag was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task RegisterDevicesAsyncWithNullDeviceTest()
        {
            var goodDevice = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            Device badDevice = null;

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var DevicesClient = new DevicesClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory.Object);

            await DevicesClient.CreateAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("RegisterDevices API did not throw exception when Null device was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task RegisterDevicesAsyncWithNullDeviceListTest()
        {
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var DevicesClient = new DevicesClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory.Object);

            await DevicesClient.CreateAsync(new List<Device>()).ConfigureAwait(false);
            Assert.Fail("RegisterDevices API did not throw exception when Null device list was used.");
        }

        [TestMethod]
        public async Task UpdateDeviceAsyncTest()
        {
            var deviceToReturn = new Device("123") { ConnectionState = DeviceConnectionState.Connected, ETag = new ETag("123") };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = HttpMessageHelper.SerializePayload(deviceToReturn);
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var DevicesClient = new DevicesClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory);

            var returnedDevice = await DevicesClient.SetAsync(deviceToReturn).ConfigureAwait(false);
            Assert.AreEqual(deviceToReturn.Id, returnedDevice.Id);
            mockHttpClient.VerifyAll();
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public async Task UpdateDeviceWithNullDeviceTest()
        {
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var DevicesClient = new DevicesClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory.Object);

            await DevicesClient.SetAsync((Device)null).ConfigureAwait(false);
            Assert.Fail("UpdateDevice api did not throw exception when the device parameter was null.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task UpdateDevicesAsyncWithNullDeviceTest()
        {
            var goodDevice = new Device("123") { ConnectionState = DeviceConnectionState.Connected, ETag = new ETag("234") };
            Device badDevice = null;

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var DevicesClient = new DevicesClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory.Object);

            await DevicesClient.SetAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("UpdateDevices API did not throw exception when Null device was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateDevicesAsyncWithNullDeviceListTest()
        {
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var DevicesClient = new DevicesClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory.Object);

            await DevicesClient.SetAsync(new List<Device>()).ConfigureAwait(false);
            Assert.Fail("UpdateDevices API did not throw exception when Null device list was used.");
        }

        [TestMethod]
        public async Task UpdateDevicesAsyncForceUpdateTest()
        {
            var goodDevice1 = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            var goodDevice2 = new Device("234") { ConnectionState = DeviceConnectionState.Connected };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            mockHttpResponse.Content = HttpMessageHelper.SerializePayload(new BulkRegistryOperationResult());
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var DevicesClient = new DevicesClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory);

            await DevicesClient.SetAsync(new List<Device>() { goodDevice1, goodDevice2 }, false, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task UpdateDevicesAsyncForceUpdateFalseTest()
        {
            var goodDevice1 = new Device("123") { ConnectionState = DeviceConnectionState.Connected, ETag = new ETag("234") };
            var goodDevice2 = new Device("234") { ConnectionState = DeviceConnectionState.Connected, ETag = new ETag("123") };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            mockHttpResponse.Content = HttpMessageHelper.SerializePayload(new BulkRegistryOperationResult());
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var DevicesClient = new DevicesClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory);

            await DevicesClient.SetAsync(new List<Device>() { goodDevice1, goodDevice2 }, true, CancellationToken.None).ConfigureAwait(false);
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public async Task DeleteDeviceAsyncWithNullIdTest()
        {
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var DevicesClient = new DevicesClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory.Object);

            await DevicesClient.DeleteAsync(string.Empty).ConfigureAwait(false);
            Assert.Fail("Delete API did not throw exception when the device id was null.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task DeleteDevicesAsyncWithNullDeviceTest()
        {
            var goodDevice = new Device("123") { ConnectionState = DeviceConnectionState.Connected, ETag = new ETag("234") };
            Device badDevice = null;

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var DevicesClient = new DevicesClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory.Object);

            await DevicesClient.DeleteAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("DeleteDevices API did not throw exception when Null device was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeleteDevicesAsyncWithNullDeviceListTest()
        {
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var DevicesClient = new DevicesClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory.Object);

            await DevicesClient.DeleteAsync(new List<Device>()).ConfigureAwait(false);
            Assert.Fail("DeleteDevices API did not throw exception when Null device list was used.");
        }

        [TestMethod]
        public async Task DeleteDevicesAsyncForceDeleteTest()
        {
            var goodDevice1 = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            var goodDevice2 = new Device("234") { ConnectionState = DeviceConnectionState.Connected };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = null;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            mockHttpResponse.Content = HttpMessageHelper.SerializePayload(new BulkRegistryOperationResult());
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var DevicesClient = new DevicesClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory);

            await DevicesClient.DeleteAsync(new List<Device>() { goodDevice1, goodDevice2 }, false, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeleteDevicesAsyncForceDeleteFalseMissingETagTest()
        {
            var badDevice1 = new Device("123") { ConnectionState = DeviceConnectionState.Connected };
            var badDevice2 = new Device("234") { ConnectionState = DeviceConnectionState.Connected };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var DevicesClient = new DevicesClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory.Object);

            await DevicesClient.DeleteAsync(new List<Device>() { badDevice1, badDevice2 }, true, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeleteDevicesAsyncForceDeleteFalseTest()
        {
            var goodDevice1 = new Device("123") { ConnectionState = DeviceConnectionState.Connected, ETag = new ETag("234") };
            var goodDevice2 = new Device("234") { ConnectionState = DeviceConnectionState.Connected, ETag = new ETag("123") };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            mockHttpResponse.Content = HttpMessageHelper.SerializePayload(new BulkRegistryOperationResult());
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var DevicesClient = new DevicesClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory);

            await DevicesClient.DeleteAsync(new List<Device>() { goodDevice1, goodDevice2 }, true, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateTwinsAsyncWithInvalidDeviceIdTest()
        {
            var goodTwin = new Twin("123");
            var badTwin = new Twin("/badTwin");
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var Twin = new TwinsClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory);
            await Twin.UpdateAsync(new List<Twin>() { goodTwin, badTwin }).ConfigureAwait(false);
            Assert.Fail("UpdateTwins API did not throw exception when bad deviceid was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateTwinsAsyncWithETagMissingTest()
        {
            var goodTwin = new Twin("123") { ETag = "234" };
            var badTwin = new Twin("234");
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var Twin = new TwinsClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory);
            await Twin.UpdateAsync(new List<Twin>() { goodTwin, badTwin }).ConfigureAwait(false);
            Assert.Fail("UpdateTwins API did not throw exception when ETag was null.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task UpdateTwinsAsyncWithNullTwinTest()
        {
            var goodTwin = new Twin("123") { ETag = "234" };
            Twin badTwin = null;
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();

            var Twin = new TwinsClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory);
            await Twin.UpdateAsync(new List<Twin>() { goodTwin, badTwin }).ConfigureAwait(false);
            Assert.Fail("UpdateTwins API did not throw exception when Null twin was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateTwinsAsyncWithNullTwinListTest()
        {
            var serviceClient = new IotHubServiceClient(validMockConnectionString);
            await serviceClient.Twins.UpdateAsync(new List<Twin>()).ConfigureAwait(false);
            Assert.Fail("UpdateTwins API did not throw exception when Null twin list was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateTwinsAsyncWithDeviceIdNullTest()
        {
            var goodTwin = new Twin("123") { ETag = "234" };
            var badTwin = new Twin();
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var Twin = new TwinsClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory);
            await Twin.UpdateAsync(new List<Twin>() { goodTwin, badTwin }).ConfigureAwait(false);
            Assert.Fail("UpdateTwins API did not throw exception when deviceId was null.");
        }

        [TestMethod]
        public async Task UpdateTwinsAsyncForceUpdateTest()
        {
            var goodTwin1 = new Twin("123");
            var goodTwin2 = new Twin("234");
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            mockHttpResponse.Content = HttpMessageHelper.SerializePayload(new BulkRegistryOperationResult());
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var Twin = new TwinsClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory);
            await Twin.UpdateAsync(new List<Twin>() { goodTwin1, goodTwin2 }, true, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateTwinsAsyncForceUpdateMissingETagTest()
        {
            var badTwin1 = new Twin("123");
            var badTwin2 = new Twin("234");
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var Twin = new TwinsClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory);
            await Twin.UpdateAsync(new List<Twin>() { badTwin1, badTwin2 }, false, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task UpdateTwinsAsyncForceUpdateFalseTest()
        {
            var goodTwin1 = new Twin("123") { ETag = "234" };
            var goodTwin2 = new Twin("234") { ETag = "123" };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            mockHttpResponse.Content = HttpMessageHelper.SerializePayload(new BulkRegistryOperationResult());
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var Twin = new TwinsClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory);
            await Twin.UpdateAsync(new List<Twin>() { goodTwin1, goodTwin2 }, false, CancellationToken.None).ConfigureAwait(false);
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
