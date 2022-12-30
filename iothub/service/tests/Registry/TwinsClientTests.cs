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
    public class TwinsClientTests
    {
        private const string HostName = "contoso.azure-devices.net";
        private static readonly string s_validMockAuthenticationHeaderValue = $"SharedAccessSignature sr={HostName}&sig=thisIsFake&se=000000&skn=registryRead";

        private static Uri s_httpUri = new($"https://{HostName}");
        private static RetryHandler s_retryHandler = new(new IotHubServiceNoRetry());

        [TestMethod]
        public async Task TwinsClient_GetTwin_Device()
        {
            // arrange
            string deviceId = "123";
            var goodTwin = new ClientTwin(deviceId);
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
            };
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var twinsClient = new TwinsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            Func<Task> act = async () => await twinsClient.GetAsync(deviceId);

            // assert
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task TwinsClient_GetTwin_Module()
        {
            // arrange
            string deviceId = "123";
            string moduleId = "234";
            var goodTwin = new ClientTwin(deviceId)
            {
                ModelId = moduleId
            };

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
            };
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var twinsClient = new TwinsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            Func<Task> act = async () => await twinsClient.GetAsync(deviceId, moduleId);

            // assert
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task TwinsClient_UpdateAsync()
        {
            // arrange
            string deviceId = "123";
            string moduleId = "234";
            var goodTwin = new ClientTwin(deviceId);

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
            };
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var twinsClient = new TwinsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            Func<Task> getTwin = async () => await twinsClient.GetAsync(deviceId);
            goodTwin.ModelId = moduleId;
            Func<Task> updateTwin = async () => await twinsClient.UpdateAsync(deviceId, goodTwin);

            // assert
            await updateTwin.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task TwinsClient_UpdateAsync_ModuleId()
        {
            // arrange
            string deviceId = "123";
            string moduleId = "234";
            var goodTwin = new ClientTwin(deviceId)
            {
                ModelId = moduleId
            };

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
            };
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var twinsClient = new TwinsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            Func<Task> act = async () => await twinsClient.UpdateAsync(deviceId, moduleId, goodTwin);

            // assert
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task TwinsClient_UpdateAsync_BadTwinIdThrows()
        {
            // arrange
            var goodTwin = new ClientTwin("123");
            var badTwin = new ClientTwin("/badTwin");
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
            };
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var twinsClient = new TwinsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            Func<Task> act = async () => await twinsClient.UpdateAsync(new List<ClientTwin> { goodTwin, badTwin }, true).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task TwinsClient_UpdateAsync_OnlyIfUnchangedTrueNoEtagThrows()
        {
            // arrange
            var goodTwin = new ClientTwin("123") { ETag = new ETag("234") };
            var badTwin = new ClientTwin("234");
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
            };
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var twinsClient = new TwinsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            Func<Task> act = async () => await twinsClient.UpdateAsync(new List<ClientTwin> { goodTwin, badTwin }, true).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task TwinsClient_UpdateAsync_NullTwinInListThrows()
        {
            // arrange
            var goodTwin = new ClientTwin("123") { ETag = new ETag("234") };
            ClientTwin badTwin = null;
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
            };
            var mockHttpClient = new Mock<HttpClient>();

            var twinsClient = new TwinsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            Func<Task> act = async () => await twinsClient.UpdateAsync(new List<ClientTwin> { goodTwin, badTwin }, true).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task TwinsClient_UpdateAsync_EmptyListThrows()
        {
            // arrange
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
            };
            var mockHttpClient = new Mock<HttpClient>();

            var twinsClient = new TwinsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            Func<Task> act = async () => await twinsClient.UpdateAsync(new List<ClientTwin>()).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task TwinsClient_UpdateAsync_OnlyIfUnchangedFalseNoEtag()
        {
            // arrange
            var goodTwin1 = new ClientTwin("123");
            var goodTwin2 = new ClientTwin("234");
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(new BulkRegistryOperationResult())
            };
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var twinsClient = new TwinsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            Func<Task> act = async () => await twinsClient.UpdateAsync(new List<ClientTwin> { goodTwin1, goodTwin2 }, false).ConfigureAwait(false);

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task TwinsClient_ReplaceAsync()
        {
            // arrange
            var goodTwin1 = new ClientTwin("123");
            var goodTwin2 = new ClientTwin("234");
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(new BulkRegistryOperationResult())
            };
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var twinsClient = new TwinsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);


            // act
            Func<Task> act = async () => await twinsClient.ReplaceAsync("123", goodTwin2);

            // assert
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task TwinsClient_ReplaceAsync_ModuleId()
        {
            // arrange
            var goodTwin1 = new ClientTwin("123")
            {
                ModuleId = "321"
            };

            var goodTwin2 = new ClientTwin("234");
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(new BulkRegistryOperationResult())
            };
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var twinsClient = new TwinsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);


            // act
            Func<Task> act = async () => await twinsClient.ReplaceAsync("123", "321", goodTwin2);

            // assert
            await act.Should().NotThrowAsync();
        }
    }
}