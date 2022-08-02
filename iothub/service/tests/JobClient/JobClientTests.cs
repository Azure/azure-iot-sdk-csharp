// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

namespace Microsoft.Azure.Devices.Api.Test
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Http2;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    [TestCategory("Unit")]
    public class JobClientTests
    {
        private readonly string jobId = "testJobId";
        private readonly JobResponse expectedJobResponse = new JobResponse();
        private readonly TimeSpan timeout = TimeSpan.FromMinutes(1);
        private const string HostName = "acme.azure-devices.net";
        private static Uri HttpUri = new Uri("https://" + HostName);
        private const string validMockAuthenticationHeaderValue = $"SharedAccessSignature sr={HostName}&sig=thisIsFake&se=000000&skn=registryRead";

        private Mock<IHttpClientHelper> httpClientHelperMock;
        private ScheduledJobsClient jobClient;

        [TestInitialize]
        public void Setup()
        {
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            httpClientHelperMock = new Mock<IHttpClientHelper>();
            jobClient = new ScheduledJobsClient(HostName, mockCredentialProvider.Object, mockHttpClient.Object, mockHttpRequestFactory);
        }

        private void NoExtraJobParamTestSetup(JobType jobType, CancellationToken cancellationToken)
        {
            httpClientHelperMock.Setup(s => s.PutAsync<JobRequest, JobResponse>(
                It.IsAny<Uri>(),
                It.Is<JobRequest>(
                    r =>
                        r.JobId == jobId && r.JobType == jobType),
                It.IsAny<Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(),
                It.Is<CancellationToken>(c => c == cancellationToken)))
                .Returns(Task.FromResult(expectedJobResponse));
        }

        private void NoExtraJobParamMultiDeviceTestSetup(JobType jobType, CancellationToken cancellationToken)
        {
            httpClientHelperMock.Setup(s => s.PutAsync<JobRequest, JobResponse>(
                It.IsAny<Uri>(),
                It.Is<JobRequest>(
                    r =>
                        r.JobId == jobId && r.JobType == jobType),
                It.IsAny<Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(),
                It.Is<CancellationToken>(c => c == cancellationToken)))
                .Returns(Task.FromResult(expectedJobResponse));
        }

        private void TestVerify(JobResponse actualJobResponse)
        {
            Assert.AreEqual(expectedJobResponse, actualJobResponse);

            httpClientHelperMock.Verify(v => v.PutAsync<JobRequest, JobResponse>(
                It.IsAny<Uri>(),
                It.IsAny<JobRequest>(),
                It.IsAny<Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
