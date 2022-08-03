// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Http2;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Scheduled jobs management.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-jobs"/>.
    public class ScheduledJobsClient
    {
        private readonly string _hostName;
        private readonly IotHubConnectionProperties _credentialProvider;
        private readonly HttpClient _httpClient;
        private readonly HttpRequestMessageFactory _httpRequestMessageFactory;

        private const string JobsUriFormat = "/jobs/v2/{0}";
        private const string JobsQueryFormat = "/jobs/v2/query";
        private const string CancelJobUriFormat = "/jobs/v2/{0}/cancel";

        private const string ContinuationTokenHeader = "x-ms-continuation";
        private const string PageSizeHeader = "x-ms-max-item-count";

        private static readonly TimeSpan s_defaultOperationTimeout = TimeSpan.FromSeconds(100);

        /// <summary>
        /// Creates client, provided for unit testing purposes only.
        /// </summary>
        protected ScheduledJobsClient()
        {
        }

        internal ScheduledJobsClient(string hostName, IotHubConnectionProperties credentialProvider, HttpClient httpClient, HttpRequestMessageFactory httpRequestMessageFactory)
        {
            _hostName = hostName;
            _credentialProvider = credentialProvider;
            _httpClient = httpClient;
            _httpRequestMessageFactory = httpRequestMessageFactory;
        }

        /// <summary>
        /// Explicitly open the client instance.
        /// </summary>
        public virtual Task OpenAsync()
        {
            return TaskHelpers.CompletedTask;
        }

        /// <summary>
        /// Closes the client instance and disposes its resources.
        /// </summary>
        public virtual Task CloseAsync()
        {
            return TaskHelpers.CompletedTask;
        }

        /// <summary>
        /// Gets the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the job to retrieve</param>
        /// <param name="cancellationToken">Task cancellation token</param>
        /// <returns>The matching JobResponse object</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="jobId"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="jobId"/> is empty or white space.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<JobResponse> GetAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, jobId, nameof(GetAsync));

            try
            {
                Argument.RequireNotNullOrEmpty(jobId, nameof(jobId));
                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetJobUri(jobId), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<JobResponse>(response, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(GetAsync)} threw an exception: {ex}", nameof(GetAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, jobId, nameof(GetAsync));
            }
        }

        /// <summary>
        /// Get IQuery through which job responses for specified jobType and jobStatus are retrieved page by page,
        /// and specify page size
        /// </summary>
        /// <param name="jobType">The job type to query. Could be null if not querying.</param>
        /// <param name="jobStatus">The job status to query. Could be null if not querying.</param>
        /// <param name="pageSize">Number of job responses in a page</param>
        /// <returns>A query object to get results and next pages.</returns>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        public virtual IQuery CreateQuery(JobType? jobType = null, JobStatus? jobStatus = null, int? pageSize = null)
        {
            return new Query(async (token) => await GetAsync(jobType, jobStatus, pageSize, token, CancellationToken.None).ConfigureAwait(false));
        }

        /// <summary>
        /// Cancels/Deletes the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the job to cancel</param>
        /// <param name="cancellationToken">Task cancellation token</param>
        /// <returns>A JobResponse object</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="jobId"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="jobId"/> is empty or white space.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<JobResponse> CancelAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, jobId, nameof(CancelAsync));

            try
            {
                Argument.RequireNotNullOrEmpty(jobId, nameof(jobId));
                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Post, new Uri(CancelJobUriFormat.FormatInvariant(jobId), UriKind.Relative), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<JobResponse>(response, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(CancelAsync)} threw an exception: {ex}", nameof(CancelAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, jobId, nameof(CancelAsync));
            }
        }

        /// <summary>
        /// Creates a new Job to run a device method on one or multiple devices
        /// </summary>
        /// <param name="jobId">Unique Job Id for this job</param>
        /// <param name="scheduledDeviceMethod"></param>
        /// <param name="cancellationToken">Task cancellation token</param>
        /// <returns>A JobResponse object</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="jobId"/> or <paramref name="scheduledDeviceMethod"/> or <paramref name="scheduledDeviceMethod.queryCondition"/> or <paramref name="scheduledDeviceMethod.cloudToDeviceMethod"/> or <paramref name="scheduledDeviceMethod.startTimeUtc"/> or <paramref name="scheduledDeviceMethod.maxExecutionTimeInSeconds"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="jobId"/> or <paramref name="scheduledDeviceMethod.queryCondition"/> is empty or white space.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<JobResponse> ScheduleDeviceMethodAsync(string jobId, ScheduledDeviceMethod scheduledDeviceMethod, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"jobId=[{jobId}], queryCondition=[{scheduledDeviceMethod.queryCondition}]", nameof(ScheduleDeviceMethodAsync));
            try
            {
                Argument.RequireNotNullOrEmpty(jobId, nameof(jobId));
                Argument.RequireNotNull(scheduledDeviceMethod, nameof(scheduledDeviceMethod));
                Argument.RequireNotNullOrEmpty(scheduledDeviceMethod.queryCondition, nameof(scheduledDeviceMethod.queryCondition));
                Argument.RequireNotNull(scheduledDeviceMethod.cloudToDeviceMethod, nameof(scheduledDeviceMethod.cloudToDeviceMethod));
                Argument.RequireNotNull(scheduledDeviceMethod.startTimeUtc, nameof(scheduledDeviceMethod.startTimeUtc));
                Argument.RequireNotNull(scheduledDeviceMethod.maxExecutionTimeInSeconds, nameof(scheduledDeviceMethod.maxExecutionTimeInSeconds));
                cancellationToken.ThrowIfCancellationRequested();

                var jobRequest = new JobRequest
                {
                    JobId = jobId,
                    JobType = JobType.ScheduleDeviceMethod,
                    CloudToDeviceMethod = scheduledDeviceMethod.cloudToDeviceMethod,
                    QueryCondition = scheduledDeviceMethod.queryCondition,
                    StartTime = scheduledDeviceMethod.startTimeUtc,
                    MaxExecutionTimeInSeconds = scheduledDeviceMethod.maxExecutionTimeInSeconds
                };

                return await CreateAsync(jobRequest, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(ScheduleDeviceMethodAsync)} threw an exception: {ex}", nameof(ScheduleDeviceMethodAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, $"jobId=[{jobId}], queryCondition=[{scheduledDeviceMethod.queryCondition}]", nameof(ScheduleDeviceMethodAsync));
            }
        }

        /// <summary>
        /// Creates a new Job to update twin tags and desired properties on one or multiple devices
        /// </summary>
        /// <param name="jobId">Unique Job Id for this job</param>
        /// <param name="scheduledTwinUpdate"></param>
        /// <param name="cancellationToken">Task cancellation token</param>
        /// <returns>A JobResponse object</returns>\
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="jobId"/> or <paramref name="scheduledTwinUpdate"/> or <paramref name="scheduledTwinUpdate.queryCondition"/> or <paramref name="scheduledTwinUpdate.twin"/> or <paramref name="scheduledTwinUpdate.startTimeUtc"/> or <paramref name="scheduledTwinUpdate.maxExecutionTimeInSeconds"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="jobId"/> or <paramref name="scheduledTwinUpdate.queryCondition"/> is empty or white space.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual Task<JobResponse> ScheduleTwinUpdateAsync(string jobId, ScheduledTwinUpdate scheduledTwinUpdate, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"jobId=[{jobId}], queryCondition=[{scheduledTwinUpdate.queryCondition}]", nameof(ScheduleDeviceMethodAsync));
            try
            {
                Argument.RequireNotNullOrEmpty(jobId, nameof(jobId));
                Argument.RequireNotNull(scheduledTwinUpdate, nameof(scheduledTwinUpdate));
                Argument.RequireNotNullOrEmpty(scheduledTwinUpdate.queryCondition, nameof(scheduledTwinUpdate.queryCondition));
                Argument.RequireNotNull(scheduledTwinUpdate.twin, nameof(scheduledTwinUpdate.twin));
                Argument.RequireNotNull(scheduledTwinUpdate.startTimeUtc, nameof(scheduledTwinUpdate.startTimeUtc));
                Argument.RequireNotNull(scheduledTwinUpdate.maxExecutionTimeInSeconds, nameof(scheduledTwinUpdate.maxExecutionTimeInSeconds));
                cancellationToken.ThrowIfCancellationRequested();

                var jobRequest = new JobRequest
                {
                    JobId = jobId,
                    JobType = JobType.ScheduleUpdateTwin,
                    UpdateTwin = scheduledTwinUpdate.twin,
                    QueryCondition = scheduledTwinUpdate.queryCondition,
                    StartTime = scheduledTwinUpdate.startTimeUtc,
                    MaxExecutionTimeInSeconds = scheduledTwinUpdate.maxExecutionTimeInSeconds
                };

                return CreateAsync(jobRequest, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(ScheduleTwinUpdateAsync)} threw an exception: {ex}", nameof(ScheduleTwinUpdateAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, $"jobId=[{jobId}], queryCondition=[{scheduledTwinUpdate.queryCondition}]", nameof(ScheduleDeviceMethodAsync));
            }

        }

        private async Task<JobResponse> CreateAsync(JobRequest jobRequest, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"jobId=[{jobRequest?.JobId}], jobType=[{jobRequest?.JobType}]", nameof(CreateAsync));

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Put, GetJobUri(jobRequest.JobId), _credentialProvider, jobRequest);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<JobResponse>(response, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(CreateAsync)} threw an exception: {ex}", nameof(CreateAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"jobId=[{jobRequest?.JobId}], jobType=[{jobRequest?.JobType}]", nameof(CreateAsync));
            }
        }

        private async Task<QueryResult> GetAsync(JobType? jobType, JobStatus? jobStatus, int? pageSize, string continuationToken, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"jobType=[{jobType}], jobStatus=[{jobStatus}], pageSize=[{pageSize}]", nameof(GetAsync));

            try
            {
                var customHeaders = new Dictionary<string, string>();
                if (!string.IsNullOrWhiteSpace(continuationToken))
                {
                    customHeaders.Add(ContinuationTokenHeader, continuationToken);
                }

                if (pageSize != null)
                {
                    customHeaders.Add(PageSizeHeader, pageSize.ToString());
                }

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, new Uri(JobsQueryFormat, UriKind.Relative), _credentialProvider, null, BuildQueryJobUri(jobType, jobStatus));
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await QueryResult.FromHttpResponseAsync(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(GetAsync)} threw an exception: {ex}", nameof(GetAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"jobType=[{jobType}], jobStatus=[{jobStatus}], pageSize=[{pageSize}]", nameof(GetAsync));
            }
        }

        private static string BuildQueryJobUri(JobType? jobType, JobStatus? jobStatus)
        {
            var stringBuilder = new StringBuilder();

            if (jobType != null)
            {
                stringBuilder.Append("&jobType={0}".FormatInvariant(WebUtility.UrlEncode(jobType.ToString())));
            }

            if (jobStatus != null)
            {
                stringBuilder.Append("&jobStatus={0}".FormatInvariant(WebUtility.UrlEncode(jobStatus.ToString())));
            }

            return stringBuilder.ToString();
        }

        private static Uri GetJobUri(string jobId)
        {
            return new Uri(JobsUriFormat.FormatInvariant(jobId ?? string.Empty), UriKind.Relative);
        }
    }
}
