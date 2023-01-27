// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using FluentAssertions;
using System.Runtime.CompilerServices;

namespace Microsoft.Azure.Devices.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ScheduledJobsClientTests
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
        public async Task ScheduledJobsClient_GetAsync()
        {
            // arrange
            var scheduledJob = new ScheduledJob();
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");

            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(scheduledJob),
            };
            mockHttpResponse.Headers.Add("ETag", "\"AAAAAAAAAAE=\"");

            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var qureyClient = new QueryClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            var scheduledJobsClient = new ScheduledJobsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                qureyClient,
                s_retryHandler);

            // act
            Func<Task> act = async () => await scheduledJobsClient.GetAsync("foo");

            // assert
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task ScheduledJobsClient_GetAsync_NullArgumentThrows()
        {
            // arrange
            var scheduledJob = new ScheduledJob();
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");

            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(scheduledJob),
            };
            mockHttpResponse.Headers.Add("ETag", "\"AAAAAAAAAAE=\"");

            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var qureyClient = new QueryClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            var scheduledJobsClient = new ScheduledJobsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                qureyClient,
                s_retryHandler);

            // act
            Func<Task> act = async () => await scheduledJobsClient.GetAsync(null);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task ScheduledJobsClient_GetAsync_HttpException()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(s_connectionString);
            ScheduledJobsClient scheduledJobsClient = serviceClient.ScheduledJobs;

            // act
            // query from a Hub that does not exist
            Func<Task> act = async() => await scheduledJobsClient.GetAsync("foo");

            // assert
            await act.Should().ThrowAsync<IotHubServiceException>();
        }

        [TestMethod]
        public async Task ScheduledJobsClient_CancelAsync()
        {
            // arrange
            var scheduledJob = new ScheduledJob();
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");

            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(scheduledJob),
            };
            mockHttpResponse.Headers.Add("ETag", "\"AAAAAAAAAAE=\"");

            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var qureyClient = new QueryClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            var scheduledJobsClient = new ScheduledJobsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                qureyClient,
                s_retryHandler);

            // act
            Func<Task> act = async () => await scheduledJobsClient.CancelAsync("foo");

            // assert
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task ScheduledJobsClient_CancelAsync_NullParameterThrows()
        {
            // arrange
            var scheduledJob = new ScheduledJob();
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");

            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(scheduledJob),
            };
            mockHttpResponse.Headers.Add("ETag", "\"AAAAAAAAAAE=\"");

            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var qureyClient = new QueryClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            var scheduledJobsClient = new ScheduledJobsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                qureyClient,
                s_retryHandler);

            // act
            Func<Task> act = async () => await scheduledJobsClient.CancelAsync(null);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task ScheduledJobsClient_CancelAsync_HttpException()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(s_connectionString);
            ScheduledJobsClient scheduledJobsClient = serviceClient.ScheduledJobs;

            // act
            // query from Hub that does not exist
            Func<Task> act = async() => await scheduledJobsClient.CancelAsync("foo");

            // assert
            await act.Should().ThrowAsync<IotHubServiceException>();
        }

        [TestMethod]
        public async Task ScheduledJobsClient_ScheduleDirectMethodAsync()
        {
            // arrange
            var scheduledJob = new ScheduledJob();
            var directMethodRequest = new DirectMethodServiceRequest("foo");
            var startTime = new DateTimeOffset();
            var scheduledJobsOptions = new ScheduledJobsOptions();

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");

            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(scheduledJob),
            };
            mockHttpResponse.Headers.Add("ETag", "\"AAAAAAAAAAE=\"");

            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var qureyClient = new QueryClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            var scheduledJobsClient = new ScheduledJobsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                qureyClient,
                s_retryHandler);

            // act
            Func<Task> act = async () => await scheduledJobsClient.ScheduleDirectMethodAsync(
                "foo",
                directMethodRequest, 
               startTime,
               scheduledJobsOptions);

            // assert
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task ScheduledJobsClient_ScheduleDirectMethodAsync_NullParamterThrows()
        {
            // arrange
            var scheduledJob = new ScheduledJob();
            var scheduledJobsOptions = new ScheduledJobsOptions();

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");

            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(scheduledJob),
            };
            mockHttpResponse.Headers.Add("ETag", "\"AAAAAAAAAAE=\"");

            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var qureyClient = new QueryClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            var scheduledJobsClient = new ScheduledJobsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                qureyClient,
                s_retryHandler);

            // act
            Func<Task> act = async () => await scheduledJobsClient.ScheduleDirectMethodAsync(
               null,
               null,
               new DateTimeOffset(DateTime.UtcNow),
               scheduledJobsOptions);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task ScheduledJobsClient_ScheduleDirectMethodAsync_HttpException()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(s_connectionString);
            ScheduledJobsClient scheduledJobsClient = serviceClient.ScheduledJobs;

            // act
            // schedule method to Hub that does not exist
            Func<Task> act = async () => await scheduledJobsClient.ScheduleDirectMethodAsync(
                "foo",
                new DirectMethodServiceRequest("bar"),
                new DateTimeOffset(DateTime.UtcNow),
                new ScheduledJobsOptions());

            // assert
            await act.Should().ThrowAsync<IotHubServiceException>();
        }

        [TestMethod]
        public async Task ScheduledJobsClient_ScheduleTwinUpdateAsync()
        {
            // arrange
            var scheduledJob = new ScheduledJob();

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");

            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(scheduledJob),
            };
            mockHttpResponse.Headers.Add("ETag", "\"AAAAAAAAAAE=\"");

            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var qureyClient = new QueryClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            var scheduledJobsClient = new ScheduledJobsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                qureyClient,
                s_retryHandler);

            // act
            Func<Task> act = async () => await scheduledJobsClient.ScheduleTwinUpdateAsync(
                "foo",
                new ClientTwin(),
                new DateTimeOffset(DateTime.UtcNow));

            // assert
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task ScheduledJobsClient_ScheduleTwinUpdateAysnc_NullParamterThrows()
        {
            // arrange
            var scheduledJob = new ScheduledJob();

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");

            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(scheduledJob),
            };
            mockHttpResponse.Headers.Add("ETag", "\"AAAAAAAAAAE=\"");

            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var qureyClient = new QueryClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            var scheduledJobsClient = new ScheduledJobsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                qureyClient,
                s_retryHandler);

            // act
            Func<Task> act = async () => await scheduledJobsClient.ScheduleTwinUpdateAsync(
                "foo",
                null,
                new DateTimeOffset(DateTime.UtcNow));

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task ScheduledJobsClient_ScheduleTwinUpdateAysnc_HttpException()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(s_connectionString);
            ScheduledJobsClient scheduledJobsClient = serviceClient.ScheduledJobs;

            // act
            // schedule twin update from Hub that doesn't exist
            Func<Task> act = async () => await scheduledJobsClient.ScheduleTwinUpdateAsync(
                "foo",
                new ClientTwin(),
                new DateTimeOffset(DateTime.UtcNow));

            // assert
            await act.Should().ThrowAsync<IotHubServiceException>();
        }
    }
}
