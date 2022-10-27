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
            var deviceToReturn = new Device(DeviceId) { ConnectionState = ClientConnectionState.Connected };
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = HttpMessageHelper.SerializePayload(deviceToReturn);
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var DevicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

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

            var DevicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

            await DevicesClient.GetAsync(null).ConfigureAwait(false);
            Assert.Fail("Calling GetDeviceAsync with null device id did not throw an exception.");
        }

        [TestMethod]
        public async Task RegisterDeviceAsyncTest()
        {
            var deviceToReturn = new Device("123") { ConnectionState = ClientConnectionState.Connected };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = HttpMessageHelper.SerializePayload(deviceToReturn);
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var DevicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

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

            var DevicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

            await DevicesClient.CreateAsync((Device)null).ConfigureAwait(false);
            Assert.Fail("RegisterDevice API did not throw exception when the device parameter was null.");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task RegisterDevicesAsyncWithETagsSetTest()
        {
            var goodDevice = new Device("123") { ConnectionState = ClientConnectionState.Connected };
            var badDevice = new Device("234") { ConnectionState = ClientConnectionState.Connected, ETag = new("234") };

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var DevicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

            await DevicesClient.CreateAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("RegisterDevices API did not throw exception when ETag was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task RegisterDevicesAsyncWithNullDeviceTest()
        {
            var goodDevice = new Device("123") { ConnectionState = ClientConnectionState.Connected };
            Device badDevice = null;

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var DevicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

            await DevicesClient.CreateAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("RegisterDevices API did not throw exception when Null device was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task RegisterDevicesAsyncWithEmptyDeviceListTest()
        {
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var DevicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

            await DevicesClient.CreateAsync(new List<Device>()).ConfigureAwait(false);
            Assert.Fail("RegisterDevices API did not throw exception when empty device list was used.");
        }

        [TestMethod]
        public async Task UpdateDeviceAsyncTest()
        {
            var deviceToReturn = new Device("123") { ConnectionState = ClientConnectionState.Connected, ETag = new ETag("123") };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = HttpMessageHelper.SerializePayload(deviceToReturn);
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var DevicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

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

            var DevicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

            await DevicesClient.SetAsync((Device)null).ConfigureAwait(false);
            Assert.Fail("UpdateDevice api did not throw exception when the device parameter was null.");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task UpdateDevicesAsyncWithNullDeviceTest()
        {
            var goodDevice = new Device("123") { ConnectionState = ClientConnectionState.Connected, ETag = new ETag("234") };
            Device badDevice = null;

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var DevicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

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

            var DevicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

            await DevicesClient.SetAsync(new List<Device>()).ConfigureAwait(false);
            Assert.Fail("UpdateDevices API did not throw exception when Null device list was used.");
        }

        [TestMethod]
        public async Task UpdateDevicesAsyncForceUpdateTest()
        {
            var goodDevice1 = new Device("123") { ConnectionState = ClientConnectionState.Connected };
            var goodDevice2 = new Device("234") { ConnectionState = ClientConnectionState.Connected };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            mockHttpResponse.Content = HttpMessageHelper.SerializePayload(new BulkRegistryOperationResult());
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var DevicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

            await DevicesClient.SetAsync(new List<Device>() { goodDevice1, goodDevice2 }, false, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task UpdateDevicesAsyncForceUpdateFalseTest()
        {
            var goodDevice1 = new Device("123") { ConnectionState = ClientConnectionState.Connected, ETag = new ETag("234") };
            var goodDevice2 = new Device("234") { ConnectionState = ClientConnectionState.Connected, ETag = new ETag("123") };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            mockHttpResponse.Content = HttpMessageHelper.SerializePayload(new BulkRegistryOperationResult());
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var DevicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

            await DevicesClient.SetAsync(new List<Device>() { goodDevice1, goodDevice2 }, true, CancellationToken.None).ConfigureAwait(false);
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public async Task DeleteDeviceAsyncWithNullIdTest()
        {
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var DevicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

            await DevicesClient.DeleteAsync(string.Empty).ConfigureAwait(false);
            Assert.Fail("Delete API did not throw exception when the device id was null.");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task DeleteDevicesAsyncWithNullDeviceTest()
        {
            var goodDevice = new Device("123") { ConnectionState = ClientConnectionState.Connected, ETag = new ETag("234") };
            Device badDevice = null;

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var DevicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

            await DevicesClient.DeleteAsync(new List<Device>() { goodDevice, badDevice }).ConfigureAwait(false);
            Assert.Fail("DeleteDevices API did not throw exception when Null device was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeleteDevicesAsyncWithEmptyDeviceListTest()
        {
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var DevicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

            await DevicesClient.DeleteAsync(new List<Device>()).ConfigureAwait(false);
            Assert.Fail("DeleteDevices API did not throw exception when empty device list was used.");
        }

        [TestMethod]
        public async Task DeleteDevicesAsyncForceDeleteTest()
        {
            var goodDevice1 = new Device("123") { ConnectionState = ClientConnectionState.Connected };
            var goodDevice2 = new Device("234") { ConnectionState = ClientConnectionState.Connected };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = null;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            mockHttpResponse.Content = HttpMessageHelper.SerializePayload(new BulkRegistryOperationResult());
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var DevicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

            await DevicesClient.DeleteAsync(new List<Device>() { goodDevice1, goodDevice2 }, false, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeleteDevicesAsyncForceDeleteFalseMissingETagTest()
        {
            var badDevice1 = new Device("123") { ConnectionState = ClientConnectionState.Connected };
            var badDevice2 = new Device("234") { ConnectionState = ClientConnectionState.Connected };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var DevicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

            await DevicesClient.DeleteAsync(new List<Device>() { badDevice1, badDevice2 }, true, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeleteDevicesAsyncForceDeleteFalseTest()
        {
            var goodDevice1 = new Device("123") { ConnectionState = ClientConnectionState.Connected, ETag = new ETag("234") };
            var goodDevice2 = new Device("234") { ConnectionState = ClientConnectionState.Connected, ETag = new ETag("123") };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            mockHttpResponse.Content = HttpMessageHelper.SerializePayload(new BulkRegistryOperationResult());
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var DevicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

            await DevicesClient.DeleteAsync(new List<Device>() { goodDevice1, goodDevice2 }, true, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task UpdateTwinsAsyncWithInvalidDeviceIdTest()
        {
            var goodTwin = new ClientTwin("123");
            var badTwin = new ClientTwin("/badTwin");
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var Twin = new TwinsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory, 
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

            await Twin.UpdateAsync(new List<ClientTwin>() { goodTwin, badTwin }, true).ConfigureAwait(false);
            Assert.Fail("UpdateTwins API did not throw exception when bad deviceid was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task UpdateTwinsAsyncWithETagMissingTest()
        {
            var goodTwin = new ClientTwin("123") { ETag = new ETag("234") };
            var badTwin = new ClientTwin("234");
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var Twin = new TwinsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

            await Twin.UpdateAsync(new List<ClientTwin>() { goodTwin, badTwin }, true).ConfigureAwait(false);
            Assert.Fail("UpdateTwins API did not throw exception when ETag was null.");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task UpdateTwinsAsyncWithNullTwinTest()
        {
            var goodTwin = new ClientTwin("123") { ETag = new ETag("234") };
            ClientTwin badTwin = null;
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();

            var Twin = new TwinsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

            await Twin.UpdateAsync(new List<ClientTwin>() { goodTwin, badTwin }, true).ConfigureAwait(false);
            Assert.Fail("UpdateTwins API did not throw exception when Null twin was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateTwinsAsyncWithNullTwinListTest()
        {
            var serviceClient = new IotHubServiceClient(validMockConnectionString);
            await serviceClient.Twins.UpdateAsync(new List<ClientTwin>()).ConfigureAwait(false);
            Assert.Fail("UpdateTwins API did not throw exception when Null twin list was used.");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task UpdateTwinsAsyncWithDeviceIdNullTest()
        {
            var goodTwin = new ClientTwin("123") { ETag = new ETag("234") };
            var badTwin = new ClientTwin();
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var Twin = new TwinsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

            await Twin.UpdateAsync(new List<ClientTwin>() { goodTwin, badTwin }, true).ConfigureAwait(false);
            Assert.Fail("UpdateTwins API did not throw exception when deviceId was null.");
        }

        [TestMethod]
        public async Task UpdateTwinsAsyncForceUpdateTest()
        {
            var goodTwin1 = new ClientTwin("123");
            var goodTwin2 = new ClientTwin("234");
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            mockHttpResponse.Content = HttpMessageHelper.SerializePayload(new BulkRegistryOperationResult());
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var Twin = new TwinsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

            await Twin.UpdateAsync(new List<ClientTwin>() { goodTwin1, goodTwin2 }, false, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task UpdateTwinsAsyncForceUpdateMissingETagTest()
        {
            var badTwin1 = new ClientTwin("123");
            var badTwin2 = new ClientTwin("234");
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var Twin = new TwinsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

            await Twin.UpdateAsync(new List<ClientTwin>() { badTwin1, badTwin2 }, true, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task UpdateTwinsAsyncForceUpdateFalseTest()
        {
            var goodTwin1 = new ClientTwin("123") { ETag = new ETag("234") };
            var goodTwin2 = new ClientTwin("234") { ETag = new ETag("234") };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            mockHttpResponse.Content = HttpMessageHelper.SerializePayload(new BulkRegistryOperationResult());
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var Twin = new TwinsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                new IotHubServiceRetryHandler(new IotHubServiceNoRetry()));

            await Twin.UpdateAsync(new List<ClientTwin>() { goodTwin1, goodTwin2 }, true, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public void Twin_ParentScopes_NotNull()
        {
            var twin = new ClientTwin();
            twin.ParentScopes.Should().NotBeNull("To prevent NREs because a property was unexecptedly null, it should have a default list instance assigned.");
            twin.ParentScopes.Should().BeEmpty("The default list instance should be empty.");
        }
    }
}
