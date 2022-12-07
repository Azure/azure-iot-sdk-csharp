// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Provides current job report when fetched.
    /// </summary>
    public class ScheduledJob : IotHubScheduledJobResponse
    {
        /// <summary>
        /// This constructor is for deserialization and unit test mocking purposes.
        /// </summary>
        /// <remarks>
        /// To unit test methods that use this type as a response, inherit from this class and give it a constructor
        /// that can set the properties you want.
        /// </remarks>
        protected internal ScheduledJob()
        { }

        /// <summary>
        /// The type of job to execute.
        /// </summary>
        [JsonProperty("jobType")]
        public JobType JobType { get; protected internal set; }

        // Some service Jobs APIs use "type" as the key for this value and some others use "jobType".
        // This private field is a workaround that allows us to deserialize either "type" or "jobType"
        // as the created time value for this class and expose it either way as JobType.
        [JsonProperty("type")]
        internal JobType AlternateJobType
        {
            get => JobType;
            set => JobType = value;
        }

        /// <summary>
        /// Device query condition.
        /// </summary>
        [JsonProperty("queryCondition")]
        public string QueryCondition { get; protected internal set; }

        /// <summary>
        /// Max execution time.
        /// </summary>
        /// <remarks>The precision on this is in seconds.</remarks>
        [JsonIgnore]
        public TimeSpan MaxExecutionTime { get; set; }

        [JsonProperty("maxExecutionTimeInSeconds")]
        internal long MaxExecutionTimeInSeconds
        {
            get => (long)MaxExecutionTime.TotalSeconds;
            set => MaxExecutionTime = TimeSpan.FromSeconds(value);
        }

        /// <summary>
        /// Different number of devices in the job.
        /// </summary>
        [JsonProperty("deviceJobStatistics")]
        public DeviceJobStatistics DeviceJobStatistics { get; protected internal set; }

        /// <summary>
        /// The Id of the device for this response.
        /// </summary>
        /// <remarks>
        /// It can be null (e.g., in case of a parent orchestration).
        /// </remarks>
        [JsonProperty("deviceId")]
        public string DeviceId { get; protected internal set; }

        /// <summary>
        /// The job Id of the parent orchestration, if any.
        /// </summary>
        [JsonProperty("parentJobId")]
        public string ParentJobId { get; protected internal set; }
    }
}
