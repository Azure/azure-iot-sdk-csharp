// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using FluentAssertions;
using System.Collections;
using Newtonsoft.Json.Linq;

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
<<<<<<< HEAD
            QueryResponse<ClientTwin> response = await queryClient.CreateAsync<ClientTwin>(query);

            // assert
            response.CurrentPage.First().DeviceId.Should().Be("foo");
=======
            Func<Task> act = async () => await queryClient.CreateAsync<ClientTwin>(query);

            // assert
            await act.Should().NotThrowAsync();
>>>>>>> 6b0e5e81faa3ff3add3ded10b61c239eef5e4ae9
        }

        [TestMethod]
        public async Task QueryClient_CreateAsync_NullParamterThrows()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(s_connectionString);

            // act
            Func<Task> act = async () => await serviceClient.Query.CreateAsync<ClientTwin>(null);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
<<<<<<< HEAD
        public async Task QueryClient_CreateAsync_IotHubNotFound_ThrowsIotHubServiceException()
        {
            // arrange
=======
        public async Task QueryClient_CreateAsync_HttpException()
        {
            // arrange
            string digitalTwinId = "foo";
            var digitalTwin = new BasicDigitalTwin
            {
                Id = digitalTwinId,
            };
>>>>>>> 6b0e5e81faa3ff3add3ded10b61c239eef5e4ae9
            var responseMessage = new ResponseMessage2
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
<<<<<<< HEAD

            // act
            // query returns HttpStatusCode.NotFound
            Func<Task> act = async () => await queryClient.CreateAsync<ClientTwin>("SELECT * FROM devices");

            // assert
            var error = await act.Should().ThrowAsync<IotHubServiceException>();
            error.And.IsTransient.Should().BeFalse();
            error.And.StatusCode.Should().Be(HttpStatusCode.NotFound);
=======
            // act
            // query from a Hub that does not exist
            Func<Task> act = async () => await queryClient.CreateAsync<ClientTwin>("SELECT * FROM devices");

            // assert
            await act.Should().ThrowAsync<IotHubServiceException>();
>>>>>>> 6b0e5e81faa3ff3add3ded10b61c239eef5e4ae9
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
            Func<Task> act = async () => await queryClient.CreateJobsQueryAsync();

            // assert
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
<<<<<<< HEAD
        public async Task QueryClient_CreateJobsQuery_IotHubNotFound_ThrowsIotHubServiceException()
        {
            // arrange
=======
        public async Task QueryClient_CreateJobsQuery_HttpException()
        {
            // arrange
            string digitalTwinId = "foo";
            var digitalTwin = new BasicDigitalTwin
            {
                Id = digitalTwinId,
            };
>>>>>>> 6b0e5e81faa3ff3add3ded10b61c239eef5e4ae9
            var responseMessage = new ResponseMessage2
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
<<<<<<< HEAD

=======
>>>>>>> 6b0e5e81faa3ff3add3ded10b61c239eef5e4ae9
            // act
            Func<Task> act = async () => await queryClient.CreateJobsQueryAsync();

            // assert
<<<<<<< HEAD
            var error = await act.Should().ThrowAsync<IotHubServiceException>();
            error.And.IsTransient.Should().BeFalse();
            error.And.StatusCode.Should().Be(HttpStatusCode.NotFound);
=======
            await act.Should().ThrowAsync<IotHubServiceException>();
>>>>>>> 6b0e5e81faa3ff3add3ded10b61c239eef5e4ae9
        }
    }
}
