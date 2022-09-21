// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Provides current job report when fetched.
    /// </summary>
    public class ScheduledJob : IotHubScheduledJobResponse
    {
        /// <summary>
        /// System generated. Ignored at creation.
        /// </summary>
        [JsonProperty(PropertyName = "jobId", NullValueHandling = NullValueHandling.Ignore)]
        public string JobId { get; internal set; }

        /// <summary>
        /// [Required] The type of job to execute.
        /// </summary>
        [JsonProperty(PropertyName = "jobType", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public JobType JobType { get; internal set; }

        // Some service Jobs APIs use "type" as the key for this value and some others use "jobType".
        // This private field is a workaround that allows us to deserialize either "type" or "jobType"
        // as the created time value for this class and expose it either way as JobType.
        [JsonProperty(PropertyName = "type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        internal JobType AlternateJobType
        {
            get => JobType;
            set => JobType = value;
        }

        /// <summary>
        /// Device query condition.
        /// </summary>
        [JsonProperty(PropertyName = "queryCondition", NullValueHandling = NullValueHandling.Ignore)]
        public string QueryCondition { get; internal set; }

        /// <summary>
        /// Scheduled job start time in UTC.
        /// </summary>
        [JsonProperty(PropertyName = "createdDateTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? CreatedOnUtc { get; internal set; }

        // Some service Jobs APIs use "createdTime" as the key for this value and some others use "createdDateTimeUtc".
        // This private field is a workaround that allows us to deserialize either "createdTime" or "createdDateTimeUtc"
        // as the created time value for this class and expose it either way as CreatedTimeUtc.
        [JsonProperty(PropertyName = "createdTime", NullValueHandling = NullValueHandling.Ignore)]
        internal DateTime? AlternateCreatedOnUtc
        {
            get => CreatedOnUtc;
            set => CreatedOnUtc = value;
        }

        /// <summary>
        /// Max execution time.
        /// </summary>
        /// <remarks>The precision on this is in seconds.</remarks>
        [JsonIgnore]
        public TimeSpan MaxExecutionTime { get; set; }

        [JsonProperty(PropertyName = "maxExecutionTimeInSeconds", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal long MaxExecutionTimeInSeconds
        {
            get => (long)MaxExecutionTime.TotalSeconds;
            set => MaxExecutionTime = TimeSpan.FromSeconds(value);
        }

        /// <summary>
        /// Different number of devices in the job.
        /// </summary>
        [JsonProperty(PropertyName = "deviceJobStatistics", NullValueHandling = NullValueHandling.Ignore)]
        public DeviceJobStatistics DeviceJobStatistics { get; internal set; }

        /// <summary>
        /// The Id of the device for this response.
        /// </summary>
        /// <remarks>
        /// It can be null (e.g., in case of a parent orchestration).
        /// </remarks>
        [JsonProperty(PropertyName = "deviceId", NullValueHandling = NullValueHandling.Ignore)]
        public string DeviceId { get; internal set; }

        /// <summary>
        /// The job Id of the parent orchestration, if any.
        /// </summary>
        [JsonProperty(PropertyName = "parentJobId", NullValueHandling = NullValueHandling.Ignore)]
        public string ParentJobId { get; internal set; }
    }
}
