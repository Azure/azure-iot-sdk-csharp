// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    using Microsoft.Azure.Devices.Shared;
    using Newtonsoft.Json;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Job management
    /// </summary>
    public abstract class JobClient : IDisposable
    {
        /// <summary>
        /// Creates a JobClient from the Iot Hub connection string.
        /// </summary>
        /// <param name="connectionString"> The Iot Hub connection string.</param>
        /// <returns> A JobClient instance. </returns>
        public static JobClient CreateFromConnectionString(string connectionString)
        {
            return CreateFromConnectionString(connectionString, new HttpTransportSettings());
        }

        /// <summary>
        /// Creates a JobClient from the Iot Hub connection string and HTTP transport settings
        /// </summary>
        /// <param name="connectionString"> The Iot Hub connection string.</param>
        /// <param name="transportSettings"> The HTTP transport settings.</param>
        /// <returns> A JobClient instance. </returns>
        public static JobClient CreateFromConnectionString(string connectionString, HttpTransportSettings transportSettings)
        {
            if (transportSettings == null)
            {
                throw new ArgumentNullException(nameof(transportSettings), "HTTP Transport settings cannot be null.");
            }
            JsonConvert.DefaultSettings = JsonSerializerSettingsInitializer.GetDefaultJsonSerializerSettings();
            TlsVersions.Instance.SetLegacyAcceptableVersions();

            var iotHubConnectionString = IotHubConnectionString.Parse(connectionString);
            return new HttpJobClient(iotHubConnectionString, transportSettings);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        { }

        /// <summary>
        /// Explicitly open the JobClient instance.
        /// </summary>
        public abstract Task OpenAsync();

        /// <summary>
        /// Closes the JobClient instance and disposes its resources.
        /// </summary>
        public abstract Task CloseAsync();

        /// <summary>
        /// Gets the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the Job to retrieve</param>
        /// <returns>The matching JobResponse object</returns>
        public abstract Task<JobResponse> GetJobAsync(string jobId);

        /// <summary>
        /// Gets the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the job to retrieve</param>
        /// <param name="cancellationToken">Task cancellation token</param>
        /// <returns>The matching JobResponse object</returns>
        public abstract Task<JobResponse> GetJobAsync(string jobId, CancellationToken cancellationToken);

        /// <summary>
        /// Get IQuery through which job responses for all job types and statuses are retrieved page by page
        /// </summary>
        /// <returns>IQuery</returns>
        public abstract IQuery CreateQuery();

        /// <summary>
        /// Get IQuery through which job responses are retrieved page by page and specify page size
        /// </summary>
        /// <param name="pageSize">Number of job responses in a page</param>
        /// <returns></returns>
        public abstract IQuery CreateQuery(int? pageSize);

        /// <summary>
        /// Get IQuery through which job responses for specified jobType and jobStatus are retrieved page by page
        /// </summary>
        /// <param name="jobType">The job type to query. Could be null if not querying.</param>
        /// <param name="jobStatus">The job status to query. Could be null if not querying.</param>
        /// <returns></returns>
        public abstract IQuery CreateQuery(JobType? jobType, JobStatus? jobStatus);

        /// <summary>
        /// Get IQuery through which job responses for specified jobType and jobStatus are retrieved page by page,
        /// and specify page size
        /// </summary>
        /// <param name="jobType">The job type to query. Could be null if not querying.</param>
        /// <param name="jobStatus">The job status to query. Could be null if not querying.</param>
        /// <param name="pageSize">Number of job responses in a page</param>
        /// <returns></returns>
        public abstract IQuery CreateQuery(JobType? jobType, JobStatus? jobStatus, int? pageSize);

        /// <summary>
        /// Cancels/Deletes the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the Job to cancel</param>
        public abstract Task<JobResponse> CancelJobAsync(string jobId);

        /// <summary>
        /// Cancels/Deletes the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the job to cancel</param>
        /// <param name="cancellationToken">Task cancellation token</param>
        public abstract Task<JobResponse> CancelJobAsync(string jobId, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a new Job to run a device method on one or multiple devices
        /// </summary>
        /// <param name="jobId">Unique Job Id for this job</param>
        /// <param name="queryCondition">Query condition to evaluate which devices to run the job on</param>
        /// <param name="cloudToDeviceMethod">Method call parameters</param>
        /// <param name="startTimeUtc">Date time in Utc to start the job</param>
        /// <param name="maxExecutionTimeInSeconds">Max execution time in seconds, i.e., ttl duration the job can run</param>
        /// <returns>A JobResponse object</returns>
        public abstract Task<JobResponse> ScheduleDeviceMethodAsync(string jobId, string queryCondition, CloudToDeviceMethod cloudToDeviceMethod, DateTime startTimeUtc, long maxExecutionTimeInSeconds);

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
        public abstract Task<JobResponse> ScheduleDeviceMethodAsync(string jobId, string queryCondition, CloudToDeviceMethod cloudToDeviceMethod, DateTime startTimeUtc, long maxExecutionTimeInSeconds, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a new Job to update twin tags and desired properties on one or multiple devices
        /// </summary>
        /// <param name="jobId">Unique Job Id for this job</param>
        /// <param name="queryCondition">Query condition to evaluate which devices to run the job on</param>
        /// <param name="twin">Twin object to use for the update</param>
        /// <param name="startTimeUtc">Date time in Utc to start the job</param>
        /// <param name="maxExecutionTimeInSeconds">Max execution time in seconds, i.e., ttl duration the job can run</param>
        /// <returns>A JobResponse object</returns>
        public abstract Task<JobResponse> ScheduleTwinUpdateAsync(string jobId, string queryCondition, Twin twin, DateTime startTimeUtc, long maxExecutionTimeInSeconds);

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
        public abstract Task<JobResponse> ScheduleTwinUpdateAsync(string jobId, string queryCondition, Twin twin, DateTime startTimeUtc, long maxExecutionTimeInSeconds, CancellationToken cancellationToken);
    }
}
