// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Exceptions;

namespace Microsoft.Azure.Devices
{
    internal class HttpJobClient : JobClient
    {
        private const string JobsUriFormat = "/jobs/v2/{0}?{1}";
        private const string JobsQueryFormat = "/jobs/v2/query?{0}";
        private const string CancelJobUriFormat = "/jobs/v2/{0}/cancel?{1}";

        private const string ContinuationTokenHeader = "x-ms-continuation";
        private const string PageSizeHeader = "x-ms-max-item-count";

        private static readonly TimeSpan s_defaultOperationTimeout = TimeSpan.FromSeconds(100);

        private IHttpClientHelper _httpClientHelper;

        internal HttpJobClient(IotHubConnectionString connectionString, HttpTransportSettings transportSettings)
        {
            _httpClientHelper = new HttpClientHelper(
                connectionString.HttpsEndpoint,
                connectionString,
                ExceptionHandlingHelper.GetDefaultErrorMapping(),
                s_defaultOperationTimeout,
                transportSettings.Proxy);
        }

        // internal test helper
        internal HttpJobClient(IHttpClientHelper httpClientHelper, string iotHubName)
        {
            if (httpClientHelper == null)
            {
                throw new ArgumentNullException(nameof(httpClientHelper));
            }

            _httpClientHelper = httpClientHelper;
        }

        public override Task OpenAsync()
        {
            return TaskHelpers.CompletedTask;
        }

        public override Task CloseAsync()
        {
            return TaskHelpers.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_httpClientHelper != null)
                {
                    _httpClientHelper.Dispose();
                    _httpClientHelper = null;
                }
            }
        }

        public override Task<JobResponse> GetJobAsync(string jobId)
        {
            return GetJobAsync(jobId, CancellationToken.None);
        }

        public override IQuery CreateQuery()
        {
            return CreateQuery(null, null, null);
        }

        public override IQuery CreateQuery(int? pageSize)
        {
            return CreateQuery(null, null, pageSize);
        }

        public override IQuery CreateQuery(JobType? jobType, JobStatus? jobStatus)
        {
            return CreateQuery(jobType, jobStatus, null);
        }

        public override IQuery CreateQuery(JobType? jobType, JobStatus? jobStatus, int? pageSize)
        {
            return new Query((token) => GetJobsAsync(jobType, jobStatus, pageSize, token, CancellationToken.None));
        }

        public override Task<JobResponse> CancelJobAsync(string jobId)
        {
            return CancelJobAsync(jobId, CancellationToken.None);
        }

        public override Task<JobResponse> GetJobAsync(string jobId, CancellationToken cancellationToken)
        {
            EnsureInstanceNotClosed();

            var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
            {
                {
                    HttpStatusCode.NotFound,
                    responseMessage => Task.FromResult((Exception) new JobNotFoundException(jobId))
                }
            };

            return _httpClientHelper.GetAsync<JobResponse>(
                GetJobUri(jobId),
                errorMappingOverrides,
                null,
                cancellationToken);
        }

        public override Task<JobResponse> CancelJobAsync(string jobId, CancellationToken cancellationToken)
        {
            EnsureInstanceNotClosed();

            return _httpClientHelper.PostAsync<string, JobResponse>(
                new Uri(CancelJobUriFormat.FormatInvariant(jobId, ClientApiVersionHelper.ApiVersionQueryString), UriKind.Relative),
                null,
                null,
                null,
                cancellationToken);
        }

        /// <inheritdoc/>
        public override Task<JobResponse> ScheduleDeviceMethodAsync(
            string jobId,
            string queryCondition,
            CloudToDeviceMethod methodCall,
            DateTime startTimeUtc,
            long maxExecutionTimeInSeconds)
        {
            return ScheduleDeviceMethodAsync(jobId, queryCondition, methodCall, startTimeUtc, maxExecutionTimeInSeconds, CancellationToken.None);
        }

        /// <inheritdoc/>
        public override Task<JobResponse> ScheduleDeviceMethodAsync(
            string jobId,
            string queryCondition,
            CloudToDeviceMethod cloudToDeviceMethod,
            DateTime startTimeUtc,
            long maxExecutionTimeInSeconds,
            CancellationToken cancellationToken)
        {
            EnsureInstanceNotClosed();

            var jobRequest = new JobRequest
            {
                JobId = jobId,
                JobType = JobType.ScheduleDeviceMethod,
                CloudToDeviceMethod = cloudToDeviceMethod,
                QueryCondition = queryCondition,
                StartTime = startTimeUtc,
                MaxExecutionTimeInSeconds = maxExecutionTimeInSeconds
            };

            return CreateJobAsync(jobRequest, cancellationToken);
        }

        public override Task<JobResponse> ScheduleTwinUpdateAsync(
            string jobId,
            string queryCondition,
            Twin twin,
            DateTime startTimeUtc,
            long maxExecutionTimeInSeconds)
        {
            return ScheduleTwinUpdateAsync(jobId, queryCondition, twin, startTimeUtc, maxExecutionTimeInSeconds, CancellationToken.None);
        }

        public override Task<JobResponse> ScheduleTwinUpdateAsync(
            string jobId,
            string queryCondition,
            Twin twin,
            DateTime startTimeUtc,
            long maxExecutionTimeInSeconds,
            CancellationToken cancellationToken)
        {
            EnsureInstanceNotClosed();

            var jobRequest = new JobRequest
            {
                JobId = jobId,
                JobType = JobType.ScheduleUpdateTwin,
                UpdateTwin = twin,
                QueryCondition = queryCondition,
                StartTime = startTimeUtc,
                MaxExecutionTimeInSeconds = maxExecutionTimeInSeconds
            };

            return CreateJobAsync(jobRequest, cancellationToken);
        }

        private Task<JobResponse> CreateJobAsync(JobRequest jobRequest, CancellationToken cancellationToken)
        {
            var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
            {
                {
                    HttpStatusCode.PreconditionFailed,
                    async (responseMessage) =>
                        new PreconditionFailedException(
                            await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false))
                },
                {
                    HttpStatusCode.NotFound, async responseMessage =>
                    {
                        string responseContent = await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false);
                        return (Exception) new DeviceNotFoundException(responseContent, (Exception) null);
                    }
                }
            };

            return _httpClientHelper.PutAsync<JobRequest, JobResponse>(
                GetJobUri(jobRequest.JobId),
                jobRequest,
                errorMappingOverrides,
                cancellationToken);
        }

        private void EnsureInstanceNotClosed()
        {
            if (_httpClientHelper == null)
            {
                throw new ObjectDisposedException("JobClient", ApiResources.JobClientInstanceAlreadyClosed);
            }
        }

        private async Task<QueryResult> GetJobsAsync(JobType? jobType, JobStatus? jobStatus, int? pageSize, string continuationToken, CancellationToken cancellationToken)
        {
            EnsureInstanceNotClosed();

            var customHeaders = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(continuationToken))
            {
                customHeaders.Add(ContinuationTokenHeader, continuationToken);
            }

            if (pageSize != null)
            {
                customHeaders.Add(PageSizeHeader, pageSize.ToString());
            }

            HttpResponseMessage response = await _httpClientHelper.GetAsync<HttpResponseMessage>(
                BuildQueryJobUri(jobType, jobStatus),
                null,
                customHeaders,
                cancellationToken).ConfigureAwait(false);

            return await QueryResult.FromHttpResponseAsync(response).ConfigureAwait(false);
        }

        private static Uri BuildQueryJobUri(JobType? jobType, JobStatus? jobStatus)
        {
            var stringBuilder = new StringBuilder(JobsQueryFormat.FormatInvariant(ClientApiVersionHelper.ApiVersionQueryString));

            if (jobType != null)
            {
                stringBuilder.Append("&jobType={0}".FormatInvariant(WebUtility.UrlEncode(jobType.ToString())));
            }

            if (jobStatus != null)
            {
                stringBuilder.Append("&jobStatus={0}".FormatInvariant(WebUtility.UrlEncode(jobStatus.ToString())));
            }

            return new Uri(stringBuilder.ToString(), UriKind.Relative);
        }

        private static Uri GetJobUri(string jobId)
        {
            return new Uri(JobsUriFormat.FormatInvariant(jobId ?? string.Empty, ClientApiVersionHelper.ApiVersionQueryString), UriKind.Relative);
        }
    }
}
