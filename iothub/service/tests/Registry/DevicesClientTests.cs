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
    public class DevicesClientTests
    {
        private const string HostName = "contoso.azure-devices.net";
        private const string ValidMockConnectionString = $"HostName={HostName};SharedAccessKeyName=iothubowner;SharedAccessKey=dGVzdFN0cmluZzE=";
        private const string ValidMockAuthenticationHeaderValue = $"SharedAccessSignature sr={HostName}&sig=thisIsFake&se=000000&skn=registryRead";

        private static Uri s_httpUri = new($"https://{HostName}");
        private static RetryHandler s_retryHandler = new(new IotHubServiceNoRetry());

        [TestMethod]
        public async Task DevicesClient_GetAsync()
        {
            // arrange
            const string deviceId = "123";
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(ValidMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            var deviceToReturn = new Device(deviceId) { ConnectionState = ClientConnectionState.Connected };
            var mockHttpResponse = new HttpResponseMessage
            {
                Content = HttpMessageHelper.SerializePayload(deviceToReturn),
                StatusCode = HttpStatusCode.OK,
            };
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            Device device = await devicesClient.GetAsync(deviceId).ConfigureAwait(false);

            // assert
            device.Id.Should().Be(deviceId);
            device.ConnectionState.Should().Be(deviceToReturn.ConnectionState);
            mockHttpClient.VerifyAll();
        }

        [TestMethod]
        public async Task DevicesClient_GetAsync_NullDeviceIdThrows()
        {
            // arrange
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                s_retryHandler);

            // act
            Func<Task> act = async () => await devicesClient.GetAsync(null).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_CreateAsync_NullDeviceThrows()
        {
            // arrange
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                s_retryHandler);

            // act
            Func<Task> act = async () => await devicesClient.CreateAsync((Device)null).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_CreateAsync_HasEtagThrows()
        {
            // arrange
            var goodDevice = new Device("123") { ConnectionState = ClientConnectionState.Connected };
            var badDevice = new Device("234") { ConnectionState = ClientConnectionState.Connected, ETag = new("234") };

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                s_retryHandler);

            // act
            Func<Task> act = async () => await devicesClient.CreateAsync(new List<Device> { goodDevice, badDevice }).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_CreateAsync_NullDeviceInListThrows()
        {
            // arrange
            var goodDevice = new Device("123") { ConnectionState = ClientConnectionState.Connected };
            Device badDevice = null;

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                s_retryHandler);

            // act
            Func<Task> act = async () => await devicesClient.CreateAsync(new List<Device> { goodDevice, badDevice }).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_CreateAsync_EmptyListThrows()
        {
            // arrange
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                s_retryHandler);

            // act
            Func<Task> act = async () => await devicesClient.CreateAsync(new List<Device>()).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_SetAsync()
        {
            // arrange
            var deviceToReturn = new Device("123") { ConnectionState = ClientConnectionState.Connected, ETag = new ETag("123") };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(ValidMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(deviceToReturn),
            };
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            Device returnedDevice = await devicesClient.SetAsync(deviceToReturn).ConfigureAwait(false);

            // assert
            returnedDevice.Id.Should().Be(deviceToReturn.Id);
            mockHttpClient.VerifyAll();
        }

        [TestMethod]
        public async Task DevicesClient_SetAsync_NullDeviceThrows()
        {
            // arrange
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                s_retryHandler);

            // act
            Func<Task> act = async () => await devicesClient.SetAsync((Device)null).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_SetAsync_NullDeviceInListThrows()
        {
            // arrange
            var goodDevice = new Device("123") { ConnectionState = ClientConnectionState.Connected, ETag = new ETag("234") };
            Device badDevice = null;

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                s_retryHandler);

            // act
            Func<Task> act = async () => await devicesClient.SetAsync(new List<Device> { goodDevice, badDevice }).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_SetAsync_EmptyListThrows()
        {
            // arrange
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                s_retryHandler);

            // act
            Func<Task> act = async () => await devicesClient.SetAsync(new List<Device>()).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_SetAsync_WithOnlyIfUnchangedTrue()
        {
            // arrange
            var goodDevice1 = new Device("123") { ConnectionState = ClientConnectionState.Connected };
            var goodDevice2 = new Device("234") { ConnectionState = ClientConnectionState.Connected };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(ValidMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(new BulkRegistryOperationResult()),
            };
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            Func<Task> act = async () => await devicesClient.SetAsync(new List<Device> { goodDevice1, goodDevice2 }, false).ConfigureAwait(false);

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_SetAsync_OnlyIfUnchangedTrueWithEtag()
        {
            // arrange
            var goodDevice1 = new Device("123") { ConnectionState = ClientConnectionState.Connected, ETag = new ETag("234") };
            var goodDevice2 = new Device("234") { ConnectionState = ClientConnectionState.Connected, ETag = new ETag("123") };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(ValidMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(new BulkRegistryOperationResult()),
            };
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            Func<Task> act = async () => await devicesClient.SetAsync(new List<Device> { goodDevice1, goodDevice2 }, true).ConfigureAwait(false);

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_DeleteAsync_DeviceIdEmptyStringThrows()
        {
            // arrange
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                s_retryHandler);

            // act
            Func<Task> act = async () => await devicesClient.DeleteAsync(string.Empty).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_DeleteAsync_NullDeviceInListThrows()
        {
            // arrange
            var goodDevice = new Device("123") { ConnectionState = ClientConnectionState.Connected, ETag = new ETag("234") };
            Device badDevice = null;

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                s_retryHandler);

            // act
            Func<Task> act = async () => await devicesClient.DeleteAsync(new List<Device> { goodDevice, badDevice }).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_DeleteAsync_EmptyListThrows()
        {
            // arrange
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                s_retryHandler);

            // act
            Func<Task> act = async () => await devicesClient.DeleteAsync(new List<Device>()).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_DeleteAsync_Bulk()
        {
            // arrange
            var goodDevice1 = new Device("123") { ConnectionState = ClientConnectionState.Connected };
            var goodDevice2 = new Device("234") { ConnectionState = ClientConnectionState.Connected };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(ValidMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(new BulkRegistryOperationResult()),
            };
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            Func<Task> act = async () => await devicesClient.DeleteAsync(new List<Device> { goodDevice1, goodDevice2 }, false).ConfigureAwait(false);

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_DeleteAsync_OnlyIfUnchangedTrueNoEtagThrows()
        {
            // arrange
            var badDevice1 = new Device("123") { ConnectionState = ClientConnectionState.Connected };
            var badDevice2 = new Device("234") { ConnectionState = ClientConnectionState.Connected };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(ValidMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
            };
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                s_retryHandler);

            // act
            Func<Task> act = async () => await devicesClient.DeleteAsync(new List<Device> { badDevice1, badDevice2 }, true).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_DeleteAsync_OnlyIfUnchangedTrueHasEtags()
        {
            // arrange
            var goodDevice1 = new Device("123") { ConnectionState = ClientConnectionState.Connected, ETag = new ETag("234") };
            var goodDevice2 = new Device("234") { ConnectionState = ClientConnectionState.Connected, ETag = new ETag("123") };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(ValidMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(new BulkRegistryOperationResult()),
            };
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            Func<Task> act = async () => await devicesClient.DeleteAsync(new List<Device> { goodDevice1, goodDevice2 }, true).ConfigureAwait(false);

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
        }
    }
}
