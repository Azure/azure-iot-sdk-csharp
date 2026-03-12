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
        private static readonly string s_toSelect = "Devices";
        private static readonly string s_jobId = "foo";
        private static readonly string s_deviceId = "bar";
        private static readonly TimeSpan s_jobTimeSpan = new(10);

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
            var scheduledJob = new ScheduledJob
            {
                JobId = s_jobId,
                DeviceId = s_deviceId,
                MaxExecutionTime = s_jobTimeSpan,
            };

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

            var queryClient = new Mock<QueryClient>();

            var scheduledJobsClient = new ScheduledJobsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                queryClient.Object,
                s_retryHandler);

            // act
            ScheduledJob jobResponse = await scheduledJobsClient.GetAsync(s_jobId);

            // assert
            jobResponse.JobId.Should().Be(s_jobId);
            jobResponse.DeviceId.Should().Be(s_deviceId);
            jobResponse.MaxExecutionTimeInSeconds.Should().Be(s_jobTimeSpan.Seconds);
        }

        [TestMethod]
        public async Task ScheduledJobsClient_GetAsync_NullArgumentThrows()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(s_connectionString);

            // act
            Func<Task> act = async () => await serviceClient.ScheduledJobs.GetAsync(null);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task ScheduledJobsClient_GetAsync_JobNotFound_ThrowsIotHubServiceException()
        {
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

            var queryClient = new Mock<QueryClient>();

            var scheduledJobsClient = new ScheduledJobsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                queryClient.Object,
                s_retryHandler);

            // act
            Func<Task> act = async() => await scheduledJobsClient.GetAsync("foo");

            // assert
            var error = await act.Should().ThrowAsync<IotHubServiceException>();
            error.And.IsTransient.Should().BeFalse();
            error.And.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task ScheduledJobsClient_CancelAsync()
        {
            // arrange
            var scheduledJob = new ScheduledJob
            {
                JobId = s_jobId,
                DeviceId = s_deviceId,
                MaxExecutionTime = s_jobTimeSpan,
            };

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

            var queryClient = new Mock<QueryClient>();

            var scheduledJobsClient = new ScheduledJobsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                queryClient.Object,
                s_retryHandler);

            // act
            ScheduledJob jobResponse = await scheduledJobsClient.CancelAsync("foo");

            // assert
            jobResponse.JobId.Should().Be(s_jobId);
            jobResponse.DeviceId.Should().Be(s_deviceId);
            jobResponse.MaxExecutionTimeInSeconds.Should().Be(s_jobTimeSpan.Seconds);
        }

        [TestMethod]
        public async Task ScheduledJobsClient_CancelAsync_NullParameterThrows()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(s_connectionString);

            // act
            Func<Task> act = async () => await serviceClient.ScheduledJobs.CancelAsync(null);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task ScheduledJobsClient_CancelAsync_JobNotFound_ThrowsIotHubServiceException()
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

            var queryClient = new Mock<QueryClient>();

            var scheduledJobsClient = new ScheduledJobsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                queryClient.Object,
                s_retryHandler);

            // act
            Func<Task> act = async() => await scheduledJobsClient.CancelAsync("foo");

            // assert
            var error = await act.Should().ThrowAsync<IotHubServiceException>();
            error.And.IsTransient.Should().BeFalse();
            error.And.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task ScheduledJobsClient_ScheduleDirectMethodAsync()
        {
            // arrange
            var scheduledJob = new ScheduledJob
            {
                JobId = s_jobId,
                DeviceId = s_deviceId,
                MaxExecutionTime = s_jobTimeSpan,
            };

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

            var queryClient = new Mock<QueryClient>();

            var scheduledJobsClient = new ScheduledJobsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                queryClient.Object,
                s_retryHandler);

            // act
            ScheduledJob returnedJob = await scheduledJobsClient.ScheduleDirectMethodAsync(
                $"SELECT * FROM {s_toSelect}",
                directMethodRequest,
                startTime,
                scheduledJobsOptions);

            // assert
            returnedJob.JobId.Should().Be(s_jobId);
            returnedJob.DeviceId.Should().Be(s_deviceId);
            returnedJob.MaxExecutionTimeInSeconds.Should().Be(s_jobTimeSpan.Seconds);
        }

        [TestMethod]
        public async Task ScheduledJobsClient_ScheduleDirectMethodAsync_NullParamterThrows()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(s_connectionString);

            // act
            Func<Task> act = async () => await serviceClient.ScheduledJobs.ScheduleDirectMethodAsync(
               null,
               null,
               new DateTimeOffset(DateTime.UtcNow),
               new ScheduledJobsOptions());

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task ScheduledJobsClient_ScheduleDirectMethodAsync_JobNotFound_ThrowsIotHubServiceException()
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

            var queryClient = new Mock<QueryClient>();

            var scheduledJobsClient = new ScheduledJobsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                queryClient.Object,
                s_retryHandler);

            // act
            Func<Task> act = async () => await scheduledJobsClient.ScheduleDirectMethodAsync(
                $"SELECT * FROM {s_toSelect}",
                new DirectMethodServiceRequest("bar"),
                new DateTimeOffset(DateTime.UtcNow),
                new ScheduledJobsOptions());

            // assert
            var error = await act.Should().ThrowAsync<IotHubServiceException>();
            error.And.IsTransient.Should().BeFalse();
            error.And.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task ScheduledJobsClient_ScheduleTwinUpdateAsync()
        {
            // arrange
            var scheduledJob = new ScheduledJob
            {
                JobId = s_jobId,
                DeviceId = s_deviceId,
                MaxExecutionTime = s_jobTimeSpan,
            };

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

            var queryClient = new Mock<QueryClient>();

            var scheduledJobsClient = new ScheduledJobsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                queryClient.Object,
                s_retryHandler);

            // act
            ScheduledJob jobResponse = await scheduledJobsClient.ScheduleTwinUpdateAsync(
                $"SELECT * FROM {s_toSelect}",
                new ClientTwin(),
                new DateTimeOffset(DateTime.UtcNow));

            // assert
            jobResponse.JobId.Should().Be(s_jobId);
            jobResponse.DeviceId.Should().Be(s_deviceId);
            jobResponse.MaxExecutionTimeInSeconds.Should().Be(s_jobTimeSpan.Seconds);
        }

        [TestMethod]
        public async Task ScheduledJobsClient_ScheduleTwinUpdateAysnc_NullParamterThrows()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(s_connectionString);

            // act
            Func<Task> act = async () => await serviceClient.ScheduledJobs.ScheduleTwinUpdateAsync(
                $"SELECT * FROM {s_toSelect}",
                null,
                new DateTimeOffset(DateTime.UtcNow));

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task ScheduledJobsClient_ScheduleTwinUpdateAysnc_JobNotFound_ThrowsIotHubServiceException()
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

            var queryClient = new Mock<QueryClient>();

            var scheduledJobsClient = new ScheduledJobsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                queryClient.Object,
                s_retryHandler);

            // act
            Func<Task> act = async () => await scheduledJobsClient.ScheduleTwinUpdateAsync(
                $"SELECT * FROM {s_toSelect}",
                new ClientTwin(),
                new DateTimeOffset(DateTime.UtcNow));

            // assert
            var error = await act.Should().ThrowAsync<IotHubServiceException>();
            error.And.IsTransient.Should().BeFalse();
            error.And.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
