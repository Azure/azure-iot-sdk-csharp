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
            Func<Task> act = async () => await queryClient.CreateAsync<ClientTwin>(query);

            // assert
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task QueryClient_CreateAsync_NullParamterThrows()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(s_connectionString);
            QueryClient queryClient = serviceClient.Query;

            // act
            Func<Task> act = async () => await queryClient.CreateAsync<ClientTwin>(null);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task QueryClient_CreateAsync_HttpException()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(s_connectionString);
            QueryClient queryClient = serviceClient.Query;

            // act
            // query from a Hub that does not exist
            Func<Task> act = async () => await queryClient.CreateAsync<ClientTwin>("SELECT * FROM devices");

            // assert
            await act.Should().ThrowAsync<IotHubServiceException>();
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
        public async Task QueryClient_CreateJobsQuery_HttpException()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(s_connectionString);
            QueryClient queryClient = serviceClient.Query;

            // act
            // query from Hub that doesn't exist
            Func<Task> act = async () => await queryClient.CreateJobsQueryAsync();

            // assert
            await act.Should().ThrowAsync<IotHubServiceException>();
        }
    }
}
