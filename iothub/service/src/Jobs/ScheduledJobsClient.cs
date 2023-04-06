// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Subclient of <see cref="IotHubServiceClient"/> for Scheduled jobs management.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-jobs"/>.
    public class ScheduledJobsClient
    {
        private const string JobsUriFormat = "/jobs/v2/{0}";
        private const string CancelJobUriFormat = "/jobs/v2/{0}/cancel";
        private const string ContinuationTokenHeader = "x-ms-continuation";
        private const string PageSizeHeader = "x-ms-max-item-count";

        private readonly string _hostName;
        private readonly IotHubConnectionProperties _credentialProvider;
        private readonly HttpClient _httpClient;
        private readonly HttpRequestMessageFactory _httpRequestMessageFactory;
        private readonly QueryClient _queryClient;
        private readonly RetryHandler _internalRetryHandler;

        /// <summary>
        /// Creates client, provided for unit testing purposes only.
        /// </summary>
        protected ScheduledJobsClient()
        { }

        internal ScheduledJobsClient(
            string hostName,
            IotHubConnectionProperties credentialProvider,
            HttpClient httpClient,
            HttpRequestMessageFactory httpRequestMessageFactory,
            QueryClient queryClient,
            RetryHandler retryHandler)
        {
            _hostName = hostName;
            _credentialProvider = credentialProvider;
            _httpClient = httpClient;
            _httpRequestMessageFactory = httpRequestMessageFactory;
            _queryClient = queryClient;
            _internalRetryHandler = retryHandler;
        }

        /// <summary>
        /// Gets the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the job to get.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>The matching sheduled job object.</returns>
        /// <exception cref="ArgumentNullException">When the provided <paramref name="jobId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the <paramref name="jobId"/> is empty or white space.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubServiceErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubServiceErrorCode"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<ScheduledJob> GetAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting job: {jobId}", nameof(GetAsync));

            Argument.AssertNotNullOrWhiteSpace(jobId, nameof(jobId));
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetJobUri(jobId), _credentialProvider);
                HttpResponseMessage response = null;

                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<ScheduledJob>(response).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                if (Fx.ContainsAuthenticationException(ex))
                {
                    throw new IotHubServiceException(ex.Message, HttpStatusCode.Unauthorized, IotHubServiceErrorCode.IotHubUnauthorizedAccess, null, ex);
                }
                throw new IotHubServiceException(ex.Message, HttpStatusCode.RequestTimeout, IotHubServiceErrorCode.GenericRequestTimeout, null, ex);
            }
            catch (Exception ex) when (Logging.IsEnabled)
            {
                Logging.Error(this, $"Getting job {jobId} threw an exception: {ex}", nameof(GetAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Getting job: {jobId}", nameof(GetAsync));
            }
        }

        /// <summary>
        /// Query all jobs or query jobs by type and/or status.
        /// </summary>
        /// <param name="options">The optional parameters to run the query with.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>An iterable set of the queried jobs.</returns>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubServiceErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubServiceErrorCode"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        /// <example>
        /// Iterate over jobs:
        /// <code language="csharp">
        /// AsyncPageable&lt;ScheduledJob&gt; jobsQuery = iotHubServiceClient.Query.CreateJobsQuery();
        /// await foreach (ScheduledJob scheduledJob in jobsQuery)
        /// {
        ///     Console.WriteLine(scheduledJob.JobId);
        /// }
        /// </code>
        /// Iterate over pages of twins:
        /// <code language="csharp">
        /// IAsyncEnumerable&lt;Page&lt;ScheduledJob&gt;&gt; jobsQuery = iotHubServiceClient.Query.CreateJobsQuery().AsPages();
        /// await foreach (Page&lt;ScheduledJob&gt; scheduledJobsPage in jobsQuery)
        /// {
        ///     foreach (ScheduledJob scheduledJob in scheduledJobsPage.Values)
        ///     {
        ///         Console.WriteLine(scheduledJob.JobId);
        ///     }
        ///     
        ///     // Note that this is disposed for you while iterating item-by-item, but not when
        ///     // iterating page-by-page. That is why this sample has to manually call dispose
        ///     // on the response object here.
        ///     scheduledJobsPage.GetRawResponse().Dispose();
        /// }
        /// </code>
        /// </example>
        public virtual AsyncPageable<ScheduledJob> CreateQuery(JobQueryOptions options = null, CancellationToken cancellationToken = default)
        {
            return _queryClient.CreateJobsQuery(options, cancellationToken);
        }

        /// <summary>
        /// Cancels/deletes the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the job to cancel.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>A job object</returns>
        /// <exception cref="ArgumentNullException">When the provided <paramref name="jobId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the <paramref name="jobId"/> is empty or white space.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubServiceErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubServiceErrorCode"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<ScheduledJob> CancelAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Canceling job: {jobId}", nameof(CancelAsync));

            Argument.AssertNotNullOrWhiteSpace(jobId, nameof(jobId));
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(
                    HttpMethod.Post,
                    new Uri(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            CancelJobUriFormat,
                            jobId),
                        UriKind.Relative),
                    _credentialProvider);
                HttpResponseMessage response = null;

                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<ScheduledJob>(response).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                if (Fx.ContainsAuthenticationException(ex))
                {
                    throw new IotHubServiceException(ex.Message, HttpStatusCode.Unauthorized, IotHubServiceErrorCode.IotHubUnauthorizedAccess, null, ex);
                }
                throw new IotHubServiceException(ex.Message, HttpStatusCode.RequestTimeout, IotHubServiceErrorCode.GenericRequestTimeout, null, ex);
            }
            catch (Exception ex) when (Logging.IsEnabled)
            {
                Logging.Error(this, $"Canceling job {jobId} threw an exception: {ex}", nameof(CancelAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Canceling job: {jobId}", nameof(CancelAsync));
            }
        }

        /// <summary>
        /// Creates a new job to run a device method on one or multiple devices.
        /// </summary>
        /// <param name="queryCondition">Query condition to evaluate which devices the job applies to.</param>
        /// <param name="directMethodRequest">Method call parameters.</param>
        /// <param name="startOnUtc">When to start the job in UTC.</param>
        /// <param name="scheduledJobsOptions">Optional parameters for scheduled device method, i.e: <paramref name="scheduledJobsOptions.JobId"/>
        /// and <paramref name="scheduledJobsOptions.MaxExecutionTimeInSeconds"/>.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>A job object.</returns>
        /// <exception cref="ArgumentNullException">When the provided <paramref name="scheduledJobsOptions.JobId"/> or <paramref name="queryCondition"/> or <paramref name="startOnUtc"/> is null.</exception>
        /// <exception cref="ArgumentException">If the <paramref name="scheduledJobsOptions.JobId"/> or <paramref name="queryCondition"/> is empty or white space.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubServiceErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubServiceErrorCode"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<ScheduledJob> ScheduleDirectMethodAsync(
            string queryCondition,
            DirectMethodServiceRequest directMethodRequest,
            DateTimeOffset startOnUtc,
            ScheduledJobsOptions scheduledJobsOptions,
            CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Scheduling direct method job: {scheduledJobsOptions.JobId}", nameof(ScheduleDirectMethodAsync));

            Argument.AssertNotNullOrWhiteSpace(queryCondition, $"{nameof(queryCondition)}");
            Argument.AssertNotNull(directMethodRequest, $"{nameof(directMethodRequest)}");
            Argument.AssertNotNull(startOnUtc, $"{nameof(startOnUtc)}");

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var jobRequest = new JobRequest
                {
                    JobId = string.IsNullOrWhiteSpace(scheduledJobsOptions.JobId) ? Guid.NewGuid().ToString() : scheduledJobsOptions.JobId,
                    JobType = JobType.ScheduleDeviceMethod,
                    DirectMethodRequest = directMethodRequest,
                    QueryCondition = queryCondition,
                    StartOn = startOnUtc,
                    MaxExecutionTime = scheduledJobsOptions.MaxExecutionTime,
                };

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Put, GetJobUri(jobRequest.JobId), _credentialProvider, jobRequest);
                HttpResponseMessage response = null;

                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<ScheduledJob>(response).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                if (Fx.ContainsAuthenticationException(ex))
                {
                    throw new IotHubServiceException(ex.Message, HttpStatusCode.Unauthorized, IotHubServiceErrorCode.IotHubUnauthorizedAccess, null, ex);
                }
                throw new IotHubServiceException(ex.Message, HttpStatusCode.RequestTimeout, IotHubServiceErrorCode.GenericRequestTimeout, null, ex);
            }
            catch (Exception ex) when (Logging.IsEnabled)
            {
                Logging.Error(this, $"Scheduling direct method job {scheduledJobsOptions.JobId} threw an exception: {ex}", nameof(ScheduleDirectMethodAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, $"Scheduling direct method job: {scheduledJobsOptions.JobId}", nameof(ScheduleDirectMethodAsync));
            }
        }

        /// <summary>
        /// Creates a new job to update twin tags and desired properties on one or multiple devices.
        /// </summary>
        /// <param name="queryCondition">Query condition to evaluate which devices to run the job on.</param>
        /// <param name="twin">Twin object to use for the update.</param>
        /// <param name="startOnUtc">When to start the job, in UTC.</param>
        /// <param name="scheduledJobsOptions">Optional parameters for scheduled twin update, i.e: <paramref name="scheduledJobsOptions.JobId"/> and
        /// <paramref name="scheduledJobsOptions.MaxExecutionTimeInSeconds"/>.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>A job object.</returns>
        /// <exception cref="ArgumentNullException">When the provided <paramref name="scheduledJobsOptions.JobId"/> or <paramref name="queryCondition"/>
        /// or <paramref name="twin"/> or <paramref name="startOnUtc"/> or <paramref name="scheduledJobsOptions.MaxExecutionTimeInSeconds"/> is null.</exception>
        /// <exception cref="ArgumentException">If the <paramref name="scheduledJobsOptions.JobId"/> or <paramref name="queryCondition"/> is empty or white space.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubServiceErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubServiceErrorCode"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<TwinScheduledJob> ScheduleTwinUpdateAsync(
            string queryCondition,
            ClientTwin twin,
            DateTimeOffset startOnUtc,
            ScheduledJobsOptions scheduledJobsOptions = default,
            CancellationToken cancellationToken = default)
        {
            // If the user didn't choose a job Id, make one.
            if (scheduledJobsOptions == default)
            {
                scheduledJobsOptions = new();
            }

            scheduledJobsOptions.JobId = string.IsNullOrWhiteSpace(scheduledJobsOptions.JobId)
                ? Guid.NewGuid().ToString()
                : scheduledJobsOptions.JobId;

            if (Logging.IsEnabled)
                Logging.Enter(this, $"Scheduling twin update: {scheduledJobsOptions.JobId}", nameof(ScheduleDirectMethodAsync));

            Argument.AssertNotNullOrWhiteSpace(queryCondition, $"{nameof(queryCondition)}");
            Argument.AssertNotNull(twin, $"{nameof(twin)}");
            Argument.AssertNotNull(startOnUtc, $"{nameof(startOnUtc)}");

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var jobRequest = new JobRequest
                {
                    JobId = scheduledJobsOptions.JobId,
                    JobType = JobType.ScheduleUpdateTwin,
                    UpdateTwin = twin,
                    QueryCondition = queryCondition,
                    StartOn = startOnUtc,
                    MaxExecutionTime = scheduledJobsOptions?.MaxExecutionTime,
                };
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Put, GetJobUri(jobRequest.JobId), _credentialProvider, jobRequest);
                HttpResponseMessage response = null;

                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<TwinScheduledJob>(response).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                if (Fx.ContainsAuthenticationException(ex))
                {
                    throw new IotHubServiceException(ex.Message, HttpStatusCode.Unauthorized, IotHubServiceErrorCode.IotHubUnauthorizedAccess, null, ex);
                }
                throw new IotHubServiceException(ex.Message, HttpStatusCode.RequestTimeout, IotHubServiceErrorCode.GenericRequestTimeout, null, ex);
            }
            catch (Exception ex) when (Logging.IsEnabled)
            {
                Logging.Error(this, $"Scheduling twin update {scheduledJobsOptions.JobId} threw an exception: {ex}", nameof(ScheduleTwinUpdateAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Scheduling twin update: {scheduledJobsOptions.JobId}", nameof(ScheduleDirectMethodAsync));
            }
        }

        private static Uri GetJobUri(string jobId)
        {
            return new Uri(
                string.Format(
                    CultureInfo.InvariantCulture,
                    JobsUriFormat,
                    jobId ?? string.Empty),
                UriKind.Relative);
        }
    }
}
