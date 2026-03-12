// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static System.Net.WebRequestMethods;

namespace Microsoft.Azure.Devices.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class DevicesClientTests
    {
        private const string HostName = "contoso.azure-devices.net";
        private static readonly string s_validMockAuthenticationHeaderValue = $"SharedAccessSignature sr={HostName}&sig=thisIsFake&se=000000&skn=registryRead";

        private static readonly Uri s_httpUri = new($"https://{HostName}");
        private static readonly RetryHandler s_retryHandler = new(new IotHubServiceNoRetry());

        [TestMethod]
        public async Task DevicesClient_GetAsync()
        {
            // arrange
            const string deviceId = "123";
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            var deviceToReturn = new Device(deviceId) { ConnectionState = ClientConnectionState.Connected };
            using var mockHttpResponse = new HttpResponseMessage
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
        public async Task DevicesClient_CreateAsync()
        {
            // arrange
            var goodDevice = new Device("123") { ConnectionState = ClientConnectionState.Connected };

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
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
            Func<Task> act = async () => await devicesClient.CreateAsync(new List<Device> { goodDevice }).ConfigureAwait(false);

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_CreateWithTwinAsync()
        {
            // arrange
            var testTagName = "DevicesClient_Tag";
            var testTagValue = 100;
            var goodDevice1 = new Device("123") { ConnectionState = ClientConnectionState.Connected };
            var clientTwin1 = new ClientTwin("123")
            {
                Tags = { { testTagName, testTagValue } },
            };

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
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
            Func<Task> act = async () => await devicesClient.CreateWithTwinAsync(goodDevice1, clientTwin1).ConfigureAwait(false);

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_CreateWithTwinAsync_NullDeviceThrows()
        {
            // arrange
            var testTagName = "DevicesClient_Tag";
            var testTagValue = 100;
            var clientTwin1 = new ClientTwin("123")
            {
                Tags = { { testTagName, testTagValue } },
            };

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
            Func<Task> act = async () => await devicesClient.CreateWithTwinAsync(null, clientTwin1).ConfigureAwait(false);
            // assert
            await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }


        [TestMethod]
        public async Task DevicesClient_CreateWithTwinAsync_NullClientTwinThrows()
        {
            // arrange
            var goodDevice1 = new Device("123") { ConnectionState = ClientConnectionState.Connected };

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
            Func<Task> act = async () => await devicesClient.CreateWithTwinAsync(goodDevice1, null).ConfigureAwait(false);
            // assert
            await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_SetAsync()
        {
            // arrange
            var deviceToReturn = new Device("123") { ConnectionState = ClientConnectionState.Connected, ETag = new ETag("123") };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
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
            var returnedDevice = await devicesClient.SetAsync(deviceToReturn).ConfigureAwait(false);

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
        public async Task DevicesClient_SetAsync_Bulk()
        {
            // arrange
            var goodDevice1 = new Device("123") { ConnectionState = ClientConnectionState.Connected };
            var goodDevice2 = new Device("234") { ConnectionState = ClientConnectionState.Connected };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
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
        public async Task DevicesClient_SetAsync_WithOnlyIfUnchangedTrueNoEtagThrows()
        {
            // arrange
            var deviceWithoutETag1 = new Device("123") { ConnectionState = ClientConnectionState.Connected };
            var deviceWithoutETag2 = new Device("234") { ConnectionState = ClientConnectionState.Connected };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
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
            Func<Task> act = async () => await devicesClient.SetAsync(new List<Device> { deviceWithoutETag1, deviceWithoutETag2 }, true).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentException>().ConfigureAwait(false);
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
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
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
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
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
            var deviceWithoutETag1 = new Device("123") { ConnectionState = ClientConnectionState.Connected };
            var deviceWithoutETag2 = new Device("234") { ConnectionState = ClientConnectionState.Connected };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            using var mockHttpResponse = new HttpResponseMessage
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
            Func<Task> act = async () => await devicesClient.DeleteAsync(new List<Device> { deviceWithoutETag1, deviceWithoutETag2 }, true).ConfigureAwait(false);

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
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
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

        [TestMethod]
        public async Task DevicesClient_GetJobAsync()
        {
            // arrange
            var jobId = "sampleJob";
            var jobStatus = JobStatus.Completed;

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            var jobToReturn = new IotHubJobResponse{ JobId = jobId, Status = jobStatus };
            using var mockHttpResponse = new HttpResponseMessage
            {
                Content = HttpMessageHelper.SerializePayload(jobToReturn),
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
            var jobsResult = await devicesClient.GetJobAsync(jobId).ConfigureAwait(false);

            // assert
            jobsResult.JobId.Should().Be(jobId);
            jobsResult.Status.Should().Be(jobStatus);
            mockHttpClient.VerifyAll();
        }

        [TestMethod]
        public async Task DevicesClient_GetJobAsync_NullJobIdThrows()
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
            Func<Task> act = async () => await devicesClient.GetJobAsync(null).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_GetJobAsync_EmptyJobIdThrows()
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
            Func<Task> act = async () => await devicesClient.GetJobAsync("").ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_GetJobAsync_InvalidJobIdThrows()
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
            Func<Task> act = async () => await devicesClient.GetJobAsync("sample job").ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_GetJobsAsync()
        {
            // arrange
            var jobId = "sampleJob";
            var jobStatus = JobStatus.Completed;

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            var jobsToReturn = new List<IotHubJobResponse> { new IotHubJobResponse { JobId = jobId, Status = jobStatus } };
            using var mockHttpResponse = new HttpResponseMessage
            {
                Content = HttpMessageHelper.SerializePayload(jobsToReturn),
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
            var jobsResult = await devicesClient.GetJobsAsync().ConfigureAwait(false);

            // assert
            jobsResult.ElementAt(0).JobId.Should().Be(jobId);
            jobsResult.ElementAt(0).Status.Should().Be(jobStatus);
            mockHttpClient.VerifyAll();
        }

        [TestMethod]
        public async Task DevicesClient_CancelJobAsync_NullJobIdThrows()
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
            Func<Task> act = async () => await devicesClient.CancelJobAsync(null).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_CancelJobAsync_EmptyJobIdThrows()
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
            Func<Task> act = async () => await devicesClient.CancelJobAsync("").ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_CancelJobAsync_InvalidJobIdThrows()
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
            Func<Task> act = async () => await devicesClient.CancelJobAsync("sample job").ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_GetModulesAsync()
        {
            // arrange
            var module1 = new Module("1234", "module1");
            var module2 = new Module("1234", "module2");

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            var modulesToReturn = new List<Module>() { module1, module2 };
            using var mockHttpResponse = new HttpResponseMessage
            {
                Content = HttpMessageHelper.SerializePayload(modulesToReturn),
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
            var modulesResult = await devicesClient.GetModulesAsync("1234").ConfigureAwait(false);

            // assert
            modulesResult.Count().Should().Be(2);
            mockHttpClient.VerifyAll();
        }


        [TestMethod]
        public async Task DevicesClient_GetModulesAsync_NullDeviceId_Throws()
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
            Func<Task> act = async () => await devicesClient.CancelJobAsync(null).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_GetModulesAsync_EmptyDeviceId_Throws()
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
            Func<Task> act = async () => await devicesClient.CancelJobAsync("").ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_GetRegistryStatisticsAsync()
        {
            // arrange
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            var registryStatisticsToReturn = new RegistryStatistics
            {
                TotalDeviceCount = 100,
                EnabledDeviceCount = 80,
                DisabledDeviceCount = 20
            };

            using var mockHttpResponse = new HttpResponseMessage
            {
                Content = HttpMessageHelper.SerializePayload(registryStatisticsToReturn),
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

            RegistryStatistics statistics = await devicesClient.GetRegistryStatisticsAsync();
            statistics.Should().NotBeNull();
            statistics.EnabledDeviceCount.Should().Be(80);
            statistics.TotalDeviceCount.Should().Be(100);
            statistics.DisabledDeviceCount.Should().Be(20);
        }

        [TestMethod]
        public async Task DevicesClient_GetServiceStatisticsAsync()
        {
            // arrange
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            var serviceStatisticsToReturn = new ServiceStatistics
            {
                ConnectedDeviceCount = 100
            };

            using var mockHttpResponse = new HttpResponseMessage
            {
                Content = HttpMessageHelper.SerializePayload(serviceStatisticsToReturn),
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

            ServiceStatistics statistics = await devicesClient.GetServiceStatisticsAsync();
            statistics.Should().NotBeNull();
            statistics.ConnectedDeviceCount.Should().Be(100);
        }

        [TestMethod]
        public async Task DevicesClient_ImportAsync_nullJobParametersThrows()
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
            Func<Task> act = async () => await devicesClient.ImportAsync(null).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_ImportAsync_MissingContainerNameInJobParametersThrows()
        {
            // arrange
            ImportJobProperties badImportJobProperties = new ImportJobProperties
            {
                OutputBlobContainerUri = new Uri("https://myaccount.blob.core.windows.net/")
            };

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
            Func<Task> act = async () => await devicesClient.ImportAsync(badImportJobProperties).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_ImportAsync_EmptyContainerNameInJobParametersThrows()
        {
            // arrange
            ImportJobProperties badImportJobProperties = new ImportJobProperties
            {
                OutputBlobContainerUri = new Uri("https://myaccount.blob.core.windows.net/ ")
            };
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
            Func<Task> act = async () => await devicesClient.ImportAsync(badImportJobProperties).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_ExportAsync_nullJobParametersThrows()
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
            Func<Task> act = async () => await devicesClient.ExportAsync(null).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_ExportAsync_MissingContainerNameInJobParametersThrows()
        {
            // arrange
            ImportJobProperties badImportJobProperties = new ImportJobProperties
            {
                OutputBlobContainerUri = new Uri("https://myaccount.blob.core.windows.net/")
            };

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
            Func<Task> act = async () => await devicesClient.ImportAsync(badImportJobProperties).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DevicesClient_ExportAsync_EmptyContainerNameInJobParametersThrows()
        {

            // arrange
            ImportJobProperties badImportJobProperties = new ImportJobProperties
            {
                OutputBlobContainerUri = new Uri("https://myaccount.blob.core.windows.net/ ")
            };
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
            Func<Task> act = async () => await devicesClient.ImportAsync(badImportJobProperties).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentException>().ConfigureAwait(false);
        }
    }
}
