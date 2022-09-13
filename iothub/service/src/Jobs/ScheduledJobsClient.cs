﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common.Exceptions;

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
            HttpRequestMessageFactory httpRequestMessageFactory,
            QueryClient queryClient)
        {
            _hostName = hostName;
            _credentialProvider = credentialProvider;
            _httpClient = httpClient;
            _httpRequestMessageFactory = httpRequestMessageFactory;
            _queryClient = queryClient;
        }

        /// <summary>
        /// Gets the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the job to get.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>The matching sheduled job object.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="jobId"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="jobId"/> is empty or white space.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<ScheduledJob> GetAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting job {jobId}", nameof(GetAsync));

            Argument.AssertNotNullOrWhiteSpace(jobId, nameof(jobId));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetJobUri(jobId), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<ScheduledJob>(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Getting job {jobId} threw an exception: {ex}", nameof(GetAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Getting job {jobId}", nameof(GetAsync));
            }
        }

        /// <summary>
        /// Queries an iterable set of jobs for specified type and status.
        /// </summary>
        /// <param name="options">The optional parameters to run with the query.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>An iterable set of jobs for specified type and status.</returns>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual Task<QueryResponse<ScheduledJob>> CreateQueryAsync(JobQueryOptions options = null, CancellationToken cancellationToken = default)
        {
            return _queryClient.CreateJobsQueryAsync(options, cancellationToken);
        }

        /// <summary>
        /// Cancels/deletes the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the job to cancel.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>A job object</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="jobId"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="jobId"/> is empty or white space.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<ScheduledJob> CancelAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Canceling job {jobId}", nameof(CancelAsync));

            try
            {
                Argument.AssertNotNullOrWhiteSpace(jobId, nameof(jobId));
                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(
                    HttpMethod.Post,
                    new Uri(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            CancelJobUriFormat,
                            jobId),
                        UriKind.Relative),
                    _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<ScheduledJob>(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Canceling job {jobId} threw an exception: {ex}", nameof(CancelAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Canceling job {jobId}", nameof(CancelAsync));
            }
        }

        /// <summary>
        /// Creates a new job to run a device method on one or multiple devices.
        /// </summary>
        /// <param name="scheduledDirectMethod">Required parameters for scheduled device method, i.e:
        /// <paramref name="scheduledDirectMethod.CloudToDeviceMethod"/>, <paramref name="scheduledDirectMethod.QueryCondition"/>,
        /// <paramref name="scheduledDirectMethod.StartTimeUtc"/>.</param>
        /// <param name="scheduledJobsOptions">Optional parameters for scheduled device method, i.e: <paramref name="scheduledJobsOptions.JobId"/>
        /// and <paramref name="scheduledJobsOptions.MaxExecutionTimeInSeconds"/>.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>A job object.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="scheduledJobsOptions.JobId"/> or <paramref name="scheduledDirectMethod"/> or <paramref name="scheduledDirectMethod.queryCondition"/> or <paramref name="scheduledDirectMethod.cloudToDeviceMethod"/> or <paramref name="scheduledDirectMethod.startTimeUtc"/> or <paramref name="scheduledDirectMethod.maxExecutionTimeInSeconds"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="scheduledJobsOptions.JobId"/> or <paramref name="scheduledDirectMethod.queryCondition"/> is empty or white space.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<ScheduledJob> ScheduleDirectMethodAsync(
            ScheduledDirectMethod scheduledDirectMethod,
            ScheduledJobsOptions scheduledJobsOptions,
            CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Scheduling direct method job {scheduledJobsOptions.JobId}", nameof(ScheduleDirectMethodAsync));

            Argument.AssertNotNull(scheduledDirectMethod, nameof(scheduledDirectMethod));

            Argument.AssertNotNullOrWhiteSpace(scheduledDirectMethod.QueryCondition, $"{nameof(scheduledDirectMethod)}.{nameof(scheduledDirectMethod.QueryCondition)}");
            Argument.AssertNotNull(scheduledDirectMethod.DirectMethodRequest, $"{nameof(scheduledDirectMethod)}.{nameof(scheduledDirectMethod.DirectMethodRequest)}");
            Argument.AssertNotNull(scheduledDirectMethod.StartOnUtc, $"{nameof(scheduledDirectMethod)}.{nameof(scheduledDirectMethod.StartOnUtc)}");

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var jobRequest = new JobRequest
                {
                    JobId = string.IsNullOrWhiteSpace(scheduledJobsOptions.JobId) ? Guid.NewGuid().ToString() : scheduledJobsOptions.JobId,
                    JobType = JobType.ScheduleDeviceMethod,
                    DirectMethodRequest = scheduledDirectMethod.DirectMethodRequest,
                    QueryCondition = scheduledDirectMethod.QueryCondition,
                    StartOn = scheduledDirectMethod.StartOnUtc,
                    MaxExecutionTime = scheduledJobsOptions.MaxExecutionTime,
                };

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Put, GetJobUri(jobRequest.JobId), _credentialProvider, jobRequest);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<ScheduledJob>(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Scheduling direct method job {scheduledJobsOptions.JobId} threw an exception: {ex}", nameof(ScheduleDirectMethodAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, $"Schedulign direct method job {scheduledJobsOptions.JobId}", nameof(ScheduleDirectMethodAsync));
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
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<ScheduledJob> ScheduleTwinUpdateAsync(
            ScheduledTwinUpdate scheduledTwinUpdate,
            ScheduledJobsOptions scheduledJobsOptions = default,
            CancellationToken cancellationToken = default)
        {
            // If the user didn't choose a job Id, make one.
            scheduledJobsOptions.JobId = string.IsNullOrWhiteSpace(scheduledJobsOptions?.JobId)
                ? Guid.NewGuid().ToString()
                : scheduledJobsOptions.JobId;

            if (Logging.IsEnabled)
                Logging.Enter(this, $"queryCondition=[{scheduledJobsOptions.JobId}]", nameof(ScheduleDirectMethodAsync));

            Argument.AssertNotNull(scheduledTwinUpdate, nameof(scheduledTwinUpdate));

            Argument.AssertNotNullOrWhiteSpace(scheduledTwinUpdate.QueryCondition, $"{nameof(scheduledTwinUpdate)}.{nameof(scheduledTwinUpdate.QueryCondition)}");
            Argument.AssertNotNull(scheduledTwinUpdate.Twin, $"{nameof(scheduledTwinUpdate)}.{nameof(scheduledTwinUpdate.Twin)}");
            Argument.AssertNotNull(scheduledTwinUpdate.StartOnUtc, $"{nameof(scheduledTwinUpdate)}.{nameof(scheduledTwinUpdate.StartOnUtc)}");

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var jobRequest = new JobRequest
                {
                    JobId = scheduledJobsOptions.JobId,
                    JobType = JobType.ScheduleUpdateTwin,
                    UpdateTwin = scheduledTwinUpdate.Twin,
                    QueryCondition = scheduledTwinUpdate.QueryCondition,
                    StartOn = scheduledTwinUpdate.StartOnUtc,
                    MaxExecutionTime = scheduledJobsOptions?.MaxExecutionTime,
                };
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Put, GetJobUri(jobRequest.JobId), _credentialProvider, jobRequest);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<ScheduledJob>(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Scheduling twin update {scheduledJobsOptions.JobId} threw an exception: {ex}", nameof(ScheduleTwinUpdateAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, $"Scheduling twin update {scheduledJobsOptions.JobId}", nameof(ScheduleDirectMethodAsync));
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
