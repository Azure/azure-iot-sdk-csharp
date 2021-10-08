﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Microsoft.Azure.Devices.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common;
using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Devices.Common.Exceptions;
using System.Net.Http;
using System.Text;

#if !NET451

using Azure;
using Azure.Core;

#endif

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Job management
    /// </summary>
    public class JobClient : IDisposable
    {
        private const string _jobsUriFormat = "/jobs/v2/{0}?{1}";
        private const string _jobsQueryFormat = "/jobs/v2/query?{0}";
        private const string _CancelJobUriFormat = "/jobs/v2/{0}/cancel?{1}";

        private const string _continuationTokenHeader = "x-ms-continuation";
        private const string _pageSizeHeader = "x-ms-max-item-count";

        private static readonly TimeSpan s_defaultOperationTimeout = TimeSpan.FromSeconds(100);

        private IHttpClientHelper _httpClientHelper;

        /// <summary>
        /// Creates an instance of <see cref="JobClient"/>, provided for unit testing purposes only.
        /// Use the CreateFromConnectionString method to create an instance to use the client.
        /// </summary>
        public JobClient()
        {
        }

        // internal test helper
        internal JobClient(IHttpClientHelper httpClientHelper)
        {
            _httpClientHelper = httpClientHelper;
        }

        internal JobClient(IotHubConnectionProperties connectionProperties, HttpTransportSettings transportSettings)
        {
            _httpClientHelper = new HttpClientHelper(
                connectionProperties.HttpsEndpoint,
                connectionProperties,
                ExceptionHandlingHelper.GetDefaultErrorMapping(),
                s_defaultOperationTimeout,
                transportSettings.Proxy,
                transportSettings.ConnectionLeaseTimeoutMilliseconds);
        }

        /// <summary>
        /// Creates a JobClient from the IoT Hub connection string.
        /// For more information, see <see href="https://github.com/Azure/azure-iot-sdk-csharp#iot-hub-service-sdk"/>
        /// </summary>
        /// <param name="connectionString"> The IoT Hub connection string.</param>
        /// <returns> A JobClient instance. </returns>
        public static JobClient CreateFromConnectionString(string connectionString)
        {
            return CreateFromConnectionString(connectionString, new HttpTransportSettings());
        }

        /// <summary>
        /// Creates a JobClient from the IoT Hub connection string and HTTP transport settings
        /// </summary>
        /// <param name="connectionString"> The IoT Hub connection string.</param>
        /// <param name="transportSettings"> The HTTP transport settings.</param>
        /// <returns> A JobClient instance. </returns>
        public static JobClient CreateFromConnectionString(string connectionString, HttpTransportSettings transportSettings)
        {
            if (transportSettings == null)
            {
                throw new ArgumentNullException(nameof(transportSettings), "HTTP Transport settings cannot be null.");
            }
            TlsVersions.Instance.SetLegacyAcceptableVersions();

            var iotHubConnectionString = IotHubConnectionString.Parse(connectionString);
            return new JobClient(iotHubConnectionString, transportSettings);
        }

#if !NET451

        /// <summary>
        /// Creates an instance of <see cref="JobClient"/>.
        /// </summary>
        /// <param name="hostName">IoT hub host name.</param>
        /// <param name="credential">Azure Active Directory credentials to authenticate with IoT hub. See <see cref="TokenCredential"/></param>
        /// <param name="transportSettings">The HTTP transport settings.</param>
        /// <returns>An instance of <see cref="JobClient"/>.</returns>
        /// <remarks>
        /// For more information on configuring IoT hub with Azure Active Directory, see <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-dev-guide-azure-ad-rbac"/>
        /// </remarks>
        public static JobClient Create(
            string hostName,
            TokenCredential credential,
            HttpTransportSettings transportSettings = default)
        {
            if (string.IsNullOrEmpty(hostName))
            {
                throw new ArgumentNullException(nameof(hostName));
            }

            if (credential == null)
            {
                throw new ArgumentNullException(nameof(credential));
            }

            var tokenCredentialProperties = new IotHubTokenCrendentialProperties(hostName, credential);
            return new JobClient(tokenCredentialProperties, transportSettings ?? new HttpTransportSettings());
        }

        /// <summary>
        /// Creates an instance of <see cref="JobClient"/>.
        /// </summary>
        /// <param name="hostName">IoT hub host name.</param>
        /// <param name="credential">Credential that generates a SAS token to authenticate with IoT hub. See <see cref="AzureSasCredential"/>.</param>
        /// <param name="transportSettings">The HTTP transport settings.</param>
        /// <returns>An instance of <see cref="JobClient"/>.</returns>
        public static JobClient Create(
            string hostName,
            AzureSasCredential credential,
            HttpTransportSettings transportSettings = default)
        {
            if (string.IsNullOrEmpty(hostName))
            {
                throw new ArgumentNullException(nameof(hostName));
            }

            if (credential == null)
            {
                throw new ArgumentNullException(nameof(credential));
            }

            var sasCredentialProperties = new IotHubSasCredentialProperties(hostName, credential);
            return new JobClient(sasCredentialProperties, transportSettings ?? new HttpTransportSettings());
        }

#endif

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
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

        /// <summary>
        /// Explicitly open the JobClient instance.
        /// </summary>
        public virtual Task OpenAsync()
        {
            return TaskHelpers.CompletedTask;
        }

        /// <summary>
        /// Closes the JobClient instance and disposes its resources.
        /// </summary>
        public virtual Task CloseAsync()
        {
            return TaskHelpers.CompletedTask;
        }

        /// <summary>
        /// Gets the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the Job to retrieve</param>
        /// <returns>The matching JobResponse object</returns>
        public virtual Task<JobResponse> GetJobAsync(string jobId)
        {
            return GetJobAsync(jobId, CancellationToken.None);
        }

        /// <summary>
        /// Gets the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the job to retrieve</param>
        /// <param name="cancellationToken">Task cancellation token</param>
        /// <returns>The matching JobResponse object</returns>
        public virtual Task<JobResponse> GetJobAsync(string jobId, CancellationToken cancellationToken)
        {
            Logging.Enter(this, jobId, nameof(GetJobAsync));

            try
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
            finally
            {
                Logging.Exit(this, jobId, nameof(GetJobAsync));
            }
        }

        /// <summary>
        /// Get IQuery through which job responses for all job types and statuses are retrieved page by page
        /// </summary>
        /// <returns>IQuery</returns>
        public virtual IQuery CreateQuery()
        {
            return CreateQuery(null, null, null);
        }

        /// <summary>
        /// Get IQuery through which job responses are retrieved page by page and specify page size
        /// </summary>
        /// <param name="pageSize">Number of job responses in a page</param>
        /// <returns></returns>
        public virtual IQuery CreateQuery(int? pageSize)
        {
            return CreateQuery(null, null, pageSize);
        }

        /// <summary>
        /// Get IQuery through which job responses for specified jobType and jobStatus are retrieved page by page
        /// </summary>
        /// <param name="jobType">The job type to query. Could be null if not querying.</param>
        /// <param name="jobStatus">The job status to query. Could be null if not querying.</param>
        /// <returns></returns>
        public virtual IQuery CreateQuery(JobType? jobType, JobStatus? jobStatus)
        {
            return CreateQuery(jobType, jobStatus, null);
        }

        /// <summary>
        /// Get IQuery through which job responses for specified jobType and jobStatus are retrieved page by page,
        /// and specify page size
        /// </summary>
        /// <param name="jobType">The job type to query. Could be null if not querying.</param>
        /// <param name="jobStatus">The job status to query. Could be null if not querying.</param>
        /// <param name="pageSize">Number of job responses in a page</param>
        /// <returns></returns>
        public virtual IQuery CreateQuery(JobType? jobType, JobStatus? jobStatus, int? pageSize)
        {
            return new Query((token) => GetJobsAsync(jobType, jobStatus, pageSize, token, CancellationToken.None));
        }

        /// <summary>
        /// Cancels/Deletes the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the Job to cancel</param>
        public virtual Task<JobResponse> CancelJobAsync(string jobId)
        {
            return CancelJobAsync(jobId, CancellationToken.None);
        }

        /// <summary>
        /// Cancels/Deletes the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the job to cancel</param>
        /// <param name="cancellationToken">Task cancellation token</param>
        public virtual Task<JobResponse> CancelJobAsync(string jobId, CancellationToken cancellationToken)
        {
            Logging.Enter(this, jobId, nameof(CancelJobAsync));

            try
            {
                EnsureInstanceNotClosed();

                return _httpClientHelper.PostAsync<string, JobResponse>(
                    new Uri(_CancelJobUriFormat.FormatInvariant(jobId, ClientApiVersionHelper.ApiVersionQueryString), UriKind.Relative),
                    null,
                    null,
                    null,
                    cancellationToken);
            }
            finally
            {
                Logging.Exit(this, jobId, nameof(CancelJobAsync));
            }
        }

        /// <summary>
        /// Creates a new Job to run a device method on one or multiple devices
        /// </summary>
        /// <param name="jobId">Unique Job Id for this job</param>
        /// <param name="queryCondition">Query condition to evaluate which devices to run the job on</param>
        /// <param name="cloudToDeviceMethod">Method call parameters</param>
        /// <param name="startTimeUtc">Date time in Utc to start the job</param>
        /// <param name="maxExecutionTimeInSeconds">Max execution time in seconds, i.e., ttl duration the job can run</param>
        /// <returns>A JobResponse object</returns>
        public virtual Task<JobResponse> ScheduleDeviceMethodAsync(string jobId, string queryCondition, CloudToDeviceMethod cloudToDeviceMethod, DateTime startTimeUtc, long maxExecutionTimeInSeconds)
        {
            return ScheduleDeviceMethodAsync(jobId, queryCondition, cloudToDeviceMethod, startTimeUtc, maxExecutionTimeInSeconds, CancellationToken.None);
        }

        /// <summary>
        /// Creates a new Job to run a device method on one or multiple devices
        /// </summary>
        /// <param name="jobId">Unique Job Id for this job</param>
        /// <param name="queryCondition">Query condition to evaluate which devices to run the job on</param>
        /// <param name="cloudToDeviceMethod">Method call parameters</param>
        /// <param name="startTimeUtc">Date time in Utc to start the job</param>
        /// <param name="maxExecutionTimeInSeconds">Max execution time in seconds, i.e., ttl duration the job can run</param>
        /// <param name="cancellationToken">Task cancellation token</param>
        /// <returns>A JobResponse object</returns>
        public virtual Task<JobResponse> ScheduleDeviceMethodAsync(string jobId, string queryCondition, CloudToDeviceMethod cloudToDeviceMethod, DateTime startTimeUtc, long maxExecutionTimeInSeconds, CancellationToken cancellationToken)
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

        /// <summary>
        /// Creates a new Job to update twin tags and desired properties on one or multiple devices
        /// </summary>
        /// <param name="jobId">Unique Job Id for this job</param>
        /// <param name="queryCondition">Query condition to evaluate which devices to run the job on</param>
        /// <param name="twin">Twin object to use for the update</param>
        /// <param name="startTimeUtc">Date time in Utc to start the job</param>
        /// <param name="maxExecutionTimeInSeconds">Max execution time in seconds, i.e., ttl duration the job can run</param>
        /// <returns>A JobResponse object</returns>
        public virtual Task<JobResponse> ScheduleTwinUpdateAsync(string jobId, string queryCondition, Twin twin, DateTime startTimeUtc, long maxExecutionTimeInSeconds)
        {
            return ScheduleTwinUpdateAsync(jobId, queryCondition, twin, startTimeUtc, maxExecutionTimeInSeconds, CancellationToken.None);
        }

        /// <summary>
        /// Creates a new Job to update twin tags and desired properties on one or multiple devices
        /// </summary>
        /// <param name="jobId">Unique Job Id for this job</param>
        /// <param name="queryCondition">Query condition to evaluate which devices to run the job on</param>
        /// <param name="twin">Twin object to use for the update</param>
        /// <param name="startTimeUtc">Date time in Utc to start the job</param>
        /// <param name="maxExecutionTimeInSeconds">Max execution time in seconds, i.e., ttl duration the job can run</param>
        /// <param name="cancellationToken">Task cancellation token</param>
        /// <returns>A JobResponse object</returns>
        public virtual Task<JobResponse> ScheduleTwinUpdateAsync(string jobId, string queryCondition, Twin twin, DateTime startTimeUtc, long maxExecutionTimeInSeconds, CancellationToken cancellationToken)
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

        private void EnsureInstanceNotClosed()
        {
            if (_httpClientHelper == null)
            {
                throw new ObjectDisposedException("JobClient", ApiResources.JobClientInstanceAlreadyClosed);
            }
        }

        private static Uri GetJobUri(string jobId)
        {
            return new Uri(_jobsUriFormat.FormatInvariant(jobId ?? string.Empty, ClientApiVersionHelper.ApiVersionQueryString), UriKind.Relative);
        }

        private Task<JobResponse> CreateJobAsync(JobRequest jobRequest, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"jobId=[{jobRequest?.JobId}], jobType=[{jobRequest?.JobType}]", nameof(CreateJobAsync));

            try
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
            finally
            {
                Logging.Exit(this, $"jobId=[{jobRequest?.JobId}], jobType=[{jobRequest?.JobType}]", nameof(CreateJobAsync));
            }
        }

        private static Uri BuildQueryJobUri(JobType? jobType, JobStatus? jobStatus)
        {
            var stringBuilder = new StringBuilder(_jobsQueryFormat.FormatInvariant(ClientApiVersionHelper.ApiVersionQueryString));

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

        private async Task<QueryResult> GetJobsAsync(JobType? jobType, JobStatus? jobStatus, int? pageSize, string continuationToken, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"jobType=[{jobType}], jobStatus=[{jobStatus}], pageSize=[{pageSize}]", nameof(GetJobsAsync));

            try
            {
                EnsureInstanceNotClosed();

                var customHeaders = new Dictionary<string, string>();
                if (!string.IsNullOrWhiteSpace(continuationToken))
                {
                    customHeaders.Add(_continuationTokenHeader, continuationToken);
                }

                if (pageSize != null)
                {
                    customHeaders.Add(_pageSizeHeader, pageSize.ToString());
                }

                HttpResponseMessage response = await _httpClientHelper.GetAsync<HttpResponseMessage>(
                    BuildQueryJobUri(jobType, jobStatus),
                    null,
                    customHeaders,
                    cancellationToken).ConfigureAwait(false);

                return await QueryResult.FromHttpResponseAsync(response).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, $"jobType=[{jobType}], jobStatus=[{jobStatus}], pageSize=[{pageSize}]", nameof(GetJobsAsync));
            }
        }
    }
}
