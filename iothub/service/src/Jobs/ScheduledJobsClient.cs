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
    /// Subclient of <see cref="IotHubServiceClient"/> for Scheduled jobs management.
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

        internal ScheduledJobsClient(
            string hostName,
            IotHubConnectionProperties credentialProvider,
            HttpClient httpClient,
            HttpRequestMessageFactory httpRequestMessageFactory)
        {
            _hostName = hostName;
            _credentialProvider = credentialProvider;
            _httpClient = httpClient;
            _httpRequestMessageFactory = httpRequestMessageFactory;
        }

        /// <summary>
        /// Gets the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the job to get.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>The matching sheduled job object.</returns>
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
        public virtual async Task<ScheduledJob> GetAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, jobId, nameof(GetAsync));

            try
            {
                Argument.RequireNotNullOrEmpty(jobId, nameof(jobId));
                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetJobUri(jobId), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper2.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper2.DeserializeResponseAsync<ScheduledJob>(response, cancellationToken).ConfigureAwait(false);
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
        /// Gets <see cref="IQuery"/> through which job responses for specified jobType and jobStatus are retrieved page by page,
        /// and specify page size.
        /// </summary>
        /// <param name="jobType">The job type to query. Could be null if not querying.</param>
        /// <param name="jobStatus">The job status to query. Could be null if not querying.</param>
        /// <param name="pageSize">Number of job responses in a page.</param>
        /// <returns>A <see cref="IQuery"/> object to get results and next pages.</returns>
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
            return new Query(async (token) => await GetAsync(
                jobType,
                jobStatus,
                pageSize,
                token,
                CancellationToken.None)
            .ConfigureAwait(false));
        }

        /// <summary>
        /// Cancels/deletes the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the job to cancel.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>A job object</returns>
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
        public virtual async Task<ScheduledJob> CancelAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, jobId, nameof(CancelAsync));

            try
            {
                Argument.RequireNotNullOrEmpty(jobId, nameof(jobId));
                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Post, new Uri(CancelJobUriFormat.FormatInvariant(jobId), UriKind.Relative), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper2.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper2.DeserializeResponseAsync<ScheduledJob>(response, cancellationToken).ConfigureAwait(false);
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
        /// Creates a new job to run a device method on one or multiple devices.
        /// </summary>
        /// <param name="scheduledDirectMethod">Required parameters for scheduled device method, i.e: <paramref name="scheduledDirectMethod.CloudToDeviceMethod"/>, <paramref name="scheduledDirectMethod.QueryCondition"/>, <paramref name="scheduledDirectMethod.StartTimeUtc"/>.</param>
        /// <param name="scheduledJobsOptions">Optional parameters for scheduled device method, i.e: <paramref name="scheduledJobsOptions.JobId"/> and <paramref name="scheduledJobsOptions.MaxExecutionTimeInSeconds"/>.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>A job object.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="scheduledJobsOptions.JobId"/> or <paramref name="scheduledDirectMethod"/> or <paramref name="scheduledDirectMethod.queryCondition"/> or <paramref name="scheduledDirectMethod.cloudToDeviceMethod"/> or <paramref name="scheduledDirectMethod.startTimeUtc"/> or <paramref name="scheduledDirectMethod.maxExecutionTimeInSeconds"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="scheduledJobsOptions.JobId"/> or <paramref name="scheduledDirectMethod.queryCondition"/> is empty or white space.</exception>
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
        public virtual async Task<ScheduledJob> ScheduleDirectMethodAsync(ScheduledDirectMethod scheduledDirectMethod,
            ScheduledJobsOptions scheduledJobsOptions, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"jobId=[{scheduledJobsOptions.JobId}], queryCondition=[{scheduledDirectMethod.QueryCondition}]", nameof(ScheduleDirectMethodAsync));
            try
            {
                Argument.RequireNotNull(scheduledDirectMethod, nameof(scheduledDirectMethod));
                Argument.RequireNotNullOrEmpty(scheduledDirectMethod.QueryCondition, nameof(scheduledDirectMethod.QueryCondition));
                Argument.RequireNotNull(scheduledDirectMethod.CloudToDeviceMethod, nameof(scheduledDirectMethod.CloudToDeviceMethod));
                Argument.RequireNotNull(scheduledDirectMethod.StartTimeUtc, nameof(scheduledDirectMethod.StartTimeUtc));
                cancellationToken.ThrowIfCancellationRequested();

                var jobRequest = new JobRequest
                {
                    JobId = string.IsNullOrWhiteSpace(scheduledJobsOptions.JobId) ? Guid.NewGuid().ToString() : scheduledJobsOptions.JobId,
                    JobType = JobType.ScheduleDeviceMethod,
                    CloudToDeviceMethod = scheduledDirectMethod.CloudToDeviceMethod,
                    QueryCondition = scheduledDirectMethod.QueryCondition,
                    StartTimeUtc = scheduledDirectMethod.StartTimeUtc,
                    MaxExecutionTime = scheduledJobsOptions.MaxExecutionTime
                };
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Put, GetJobUri(jobRequest.JobId), _credentialProvider, jobRequest);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper2.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper2.DeserializeResponseAsync<ScheduledJob>(response, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(ScheduleDirectMethodAsync)} threw an exception: {ex}", nameof(ScheduleDirectMethodAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, $"jobId=[{scheduledJobsOptions.JobId}], queryCondition=[{scheduledDirectMethod.QueryCondition}]", nameof(ScheduleDirectMethodAsync));
            }
        }

        /// <summary>
        /// Creates a new job to update twin tags and desired properties on one or multiple devices.
        /// </summary>
        /// <param name="scheduledTwinUpdate">Required parameters for scheduled twin update, i.e: <paramref name="scheduledTwinUpdate.Twin"/>, <paramref name="scheduledTwinUpdate.QueryCondition"/>, <paramref name="scheduledTwinUpdate.StartTimeUtc"/>.</param>
        /// <param name="scheduledJobsOptions">Optional parameters for scheduled twin update, i.e: <paramref name="scheduledJobsOptions.JobId"/> and <paramref name="scheduledJobsOptions.MaxExecutionTimeInSeconds"/>.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>A job object.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="scheduledJobsOptions.JobId"/> or <paramref name="scheduledTwinUpdate"/> or <paramref name="scheduledTwinUpdate.QueryCondition"/> or <paramref name="scheduledTwinUpdate.Twin"/> or <paramref name="scheduledTwinUpdate.StartTimeUtc"/> or <paramref name="scheduledJobsOptions.MaxExecutionTimeInSeconds"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="scheduledJobsOptions.JobId"/> or <paramref name="scheduledTwinUpdate.QueryCondition"/> is empty or white space.</exception>
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
        public virtual async Task<ScheduledJob> ScheduleTwinUpdateAsync(ScheduledTwinUpdate scheduledTwinUpdate,
            ScheduledJobsOptions scheduledJobsOptions = default, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"queryCondition=[{scheduledTwinUpdate.QueryCondition}]", nameof(ScheduleDirectMethodAsync));
            try
            {
                Argument.RequireNotNull(scheduledTwinUpdate, nameof(scheduledTwinUpdate));
                Argument.RequireNotNullOrEmpty(scheduledTwinUpdate.QueryCondition, nameof(scheduledTwinUpdate.QueryCondition));
                Argument.RequireNotNull(scheduledTwinUpdate.Twin, nameof(scheduledTwinUpdate.Twin));
                Argument.RequireNotNull(scheduledTwinUpdate.StartTimeUtc, nameof(scheduledTwinUpdate.StartTimeUtc));
                cancellationToken.ThrowIfCancellationRequested();

                var jobRequest = new JobRequest
                {
                    JobId = string.IsNullOrWhiteSpace(scheduledJobsOptions.JobId) ? Guid.NewGuid().ToString() : scheduledJobsOptions.JobId,
                    JobType = JobType.ScheduleUpdateTwin,
                    UpdateTwin = scheduledTwinUpdate.Twin,
                    QueryCondition = scheduledTwinUpdate.QueryCondition,
                    StartTimeUtc = scheduledTwinUpdate.StartTimeUtc,
                    MaxExecutionTime = scheduledJobsOptions.MaxExecutionTime
                };
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Put, GetJobUri(jobRequest.JobId), _credentialProvider, jobRequest);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper2.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper2.DeserializeResponseAsync<ScheduledJob>(response, cancellationToken).ConfigureAwait(false);
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
                    Logging.Enter(this, $"queryCondition=[{scheduledTwinUpdate.QueryCondition}]", nameof(ScheduleDirectMethodAsync));
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
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper2.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
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
