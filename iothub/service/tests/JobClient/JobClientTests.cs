﻿// Copyright (c) Microsoft. All rights reserved.
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
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    [TestCategory("Unit")]
    public class JobClientTests
    {
        private readonly string jobId = "testJobId";
        private readonly JobResponse expectedJobResponse = new JobResponse();
        private readonly TimeSpan timeout = TimeSpan.FromMinutes(1);

        private Mock<IHttpClientHelper> httpClientHelperMock;
        private JobClient jobClient;

        [TestInitialize]
        public void Setup()
        {
            httpClientHelperMock = new Mock<IHttpClientHelper>();
            jobClient = new JobClient(httpClientHelperMock.Object);
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

        [TestMethod]
        public void DisposeTest()
        {
            httpClientHelperMock.Setup(restOp => restOp.Dispose());
            jobClient.Dispose();
            httpClientHelperMock.Verify(restOp => restOp.Dispose(), Times.Once());
        }

        [TestMethod]
        public async Task CloseAsyncTest()
        {
            httpClientHelperMock.Setup(restOp => restOp.Dispose());
            await jobClient.CloseAsync().ConfigureAwait(false);
            httpClientHelperMock.Verify(restOp => restOp.Dispose(), Times.Never());
        }
    }
}
