// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using FluentAssertions;

namespace Microsoft.Azure.Devices.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class QueryClientTests
    {
        private const string HostName = "contoso.azure-devices.net";
        private static readonly string s_validMockAuthenticationHeaderValue = $"SharedAccessSignature sr={HostName}&sig=thisIsFake&se=000000&skn=registryRead";
        private static readonly string s_connectionString = $"HostName={HostName};SharedAccessKeyName=iothubowner;SharedAccessKey=dGVzdFN0cmluZzE=";

        private static readonly Uri s_httpUri = new($"https://{HostName}");
        private static readonly RetryHandler s_retryHandler = new(new IotHubServiceNoRetry());
        private static IotHubServiceClientOptions s_options = new()
        {
            Protocol = IotHubTransportProtocol.Tcp,
            RetryPolicy = new IotHubServiceNoRetry()
        };

        [TestMethod]
        public async Task QueryClient_CreateAsync()
        {
            // arrange
            string query = "select * from devices where deviceId = 'foo'";
            var twin = new ClientTwin("foo");

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");

            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(new List<ClientTwin> { twin }),
            };
            mockHttpResponse.Headers.Add("ETag", "\"AAAAAAAAAAE=\"");

            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var queryClient = new QueryClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            AsyncPageable<ClientTwin> response = queryClient.CreateAsync<ClientTwin>(query);
            await response.GetAsyncEnumerator().MoveNextAsync().ConfigureAwait(false);

            // assert
            response.GetAsyncEnumerator().Current.DeviceId.Should().Be("foo");
        }

        [TestMethod]
        public async Task QueryClient_CreateAsync_NullParamterThrows()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(s_connectionString);

            // act
            Func<Task> act = async () => await serviceClient.Query.CreateAsync<ClientTwin>(null).GetAsyncEnumerator().MoveNextAsync();

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task QueryClient_CreateAsync_IotHubNotFound_ThrowsIotHubServiceException()
        {
            // arrange
            var responseMessage = new ErrorPayload2
            {
                Message = "test",
                ExceptionMessage = "test"
            };

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");

            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = HttpMessageHelper.SerializePayload(responseMessage),
            };

            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var queryClient = new QueryClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            // query returns HttpStatusCode.NotFound
            Func<Task> act = async () => await queryClient.CreateAsync<ClientTwin>("SELECT * FROM devices").GetAsyncEnumerator().MoveNextAsync();

            // assert
            var error = await act.Should().ThrowAsync<IotHubServiceException>();
            error.And.IsTransient.Should().BeFalse();
            error.And.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task QueryClient_CreateJobsQueryAsync()
        {
            // arrange
            var job = new ScheduledJob();
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");

            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(new List<ScheduledJob> { job }),
            };
            mockHttpResponse.Headers.Add("ETag", "\"AAAAAAAAAAE=\"");

            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var queryClient = new QueryClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            Func<Task> act = async () => await queryClient.CreateJobsQueryAsync().GetAsyncEnumerator().MoveNextAsync();

            // assert
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task QueryClient_CreateJobsQuery_IotHubNotFound_ThrowsIotHubServiceException()
        {
            // arrange
            var responseMessage = new ErrorPayload2
            {
                Message = "test",
                ExceptionMessage = "test"
            };

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");

            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = HttpMessageHelper.SerializePayload(responseMessage),
            };

            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var queryClient = new QueryClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            Func<Task> act = async () => await queryClient.CreateJobsQueryAsync().GetAsyncEnumerator().MoveNextAsync();

            // assert
            var error = await act.Should().ThrowAsync<IotHubServiceException>();
            error.And.IsTransient.Should().BeFalse();
            error.And.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
