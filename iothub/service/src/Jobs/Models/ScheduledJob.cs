// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

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
        public ScheduledJob()
        { }

        /// <summary>
        /// The type of job to execute.
        /// </summary>
        [JsonPropertyName("jobType")]
        public JobType JobType { get; set; }


        /// <summary>
        /// This field is the same as <see cref="JobType"/>. It only exists for serialization/deserialization differences in some
        /// some service APIs.
        /// </summary>
        /// <remarks>
        /// Some service Jobs APIs use "type" as the key for this value and some others use "jobType".
        /// This private field is a workaround that allows us to deserialize either "type" or "jobType"
        /// as the created time value for this class and expose it either way as JobType.
        /// </remarks>
        [JsonPropertyName("type")]
        public JobType AlternateJobType
        {
            get => JobType;
            set => JobType = value;
        }

        /// <summary>
        /// Device query condition.
        /// </summary>
        [JsonPropertyName("queryCondition")]
        public string QueryCondition { get; set; }

        /// <summary>
        /// Max execution time for this job.
        /// </summary>
        [JsonPropertyName("maxExecutionTimeInSeconds")]
        public int MaxExecutionTimeInSeconds { get; set; }

        /// <summary>
        /// Different number of devices in the job.
        /// </summary>
        [JsonPropertyName("deviceJobStatistics")]
        public DeviceJobStatistics DeviceJobStatistics { get; set; }

        /// <summary>
        /// The Id of the device for this response.
        /// </summary>
        /// <remarks>
        /// It can be null (e.g., in case of a parent orchestration).
        /// </remarks>
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; }

        /// <summary>
        /// The job Id of the parent orchestration, if any.
        /// </summary>
        [JsonPropertyName("parentJobId")]
        public string ParentJobId { get; set; }
    }
}
