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
    public class ScheduledJob
    {
        /// <summary>
        /// System generated.  Ignored at creation.
        /// </summary>
        [JsonProperty(PropertyName = "jobId", NullValueHandling = NullValueHandling.Ignore)]
        public string JobId { get; internal set; }

        /// <summary>
        /// Device query condition.
        /// </summary>
        [JsonProperty(PropertyName = "queryCondition", NullValueHandling = NullValueHandling.Ignore)]
        public string QueryCondition { get; internal set; }

        /// <summary>
        /// Scheduled job start time in UTC.
        /// </summary>
        [JsonProperty(PropertyName = "createdDateTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? CreatedTimeUtc { get; internal set; }

        // Some service Jobs APIs use "createdTime" as the key for this value and some others use "createdDateTimeUtc".
        // This private field is a workaround that allows us to deserialize either "createdTime" or "createdDateTimeUtc"
        // as the created time value for this class and expose it either way as CreatedTimeUtc.
        [JsonProperty(PropertyName = "createdTime", NullValueHandling = NullValueHandling.Ignore)]
        internal DateTime? _alternateCreatedTimeUtc
        {
            get => CreatedTimeUtc;
            set => CreatedTimeUtc = value;
        }

        /// <summary>
        /// System generated. Ignored at creation.
        /// </summary>
        [JsonProperty(PropertyName = "startTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? StartTimeUtc { get; internal set; }

        /// <summary>
        /// System generated. Ignored at creation.
        /// Represents the time the job stopped processing.
        /// </summary>
        [JsonProperty(PropertyName = "endTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? EndTimeUtc { get; internal set; }

        // Some service Jobs APIs use "endTime" as the key for this value and some others use "endTimeUtc".
        // This private field is a workaround that allows us to deserialize either "endTime" or "endTimeUtc"
        // as the created time value for this class and expose it either way as EndTimeUtc.
        [JsonProperty(PropertyName = "endTime")]
        internal DateTime? _alternateEndTimeUtc
        {
            get => EndTimeUtc;
            set => EndTimeUtc = value;
        }

        /// <summary>
        /// Max execution time in secounds.
        /// </summary>
        [JsonProperty(PropertyName = "maxExecutionTimeInSeconds", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public long MaxExecutionTimeInSeconds { get; internal set; }

        /// <summary>
        /// Required.
        /// The type of job to execute.
        /// </summary>
        [JsonProperty(PropertyName = "jobType", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public JobType JobType { get; internal set; }

        // Some service Jobs APIs use "type" as the key for this value and some others use "jobType".
        // This private field is a workaround that allows us to deserialize either "type" or "jobType"
        // as the created time value for this class and expose it either way as JobType.
        [JsonProperty(PropertyName = "type", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        internal JobType _alternateJobType
        {
            get => JobType;
            set => JobType = value;
        }

        /// <summary>
        /// System generated. Ignored at creation.
        /// </summary>
        [JsonProperty(PropertyName = "status", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public JobStatus Status { get; internal set; }

        /// <summary>
        /// Required if jobType is cloudToDeviceMethod.
        /// The method type and parameters.
        /// </summary>
        [JsonProperty(PropertyName = "cloudToDeviceMethod", NullValueHandling = NullValueHandling.Ignore)]
        public DirectMethodRequest DirectMethodRequest { get; set; }

        /// <summary>
        /// Required if jobType is updateTwin.
        /// The Update Twin tags and desired properties.
        /// </summary>
        [JsonProperty(PropertyName = "updateTwin", NullValueHandling = NullValueHandling.Ignore)]
        public Twin UpdateTwin { get; internal set; }

        /// <summary>
        /// System generated. Ignored at creation.
        /// </summary>
        /// <remarks>
        /// If status == failure, this represents a string containing the reason.
        /// </remarks>
        [JsonProperty(PropertyName = "failureReason", NullValueHandling = NullValueHandling.Ignore)]
        public string FailureReason { get; internal set; }

        /// <summary>
        /// System generated. Ignored at creation.
        /// Represents a string containing a message with status about the job execution.
        /// </summary>
        [JsonProperty(PropertyName = "statusMessage", NullValueHandling = NullValueHandling.Ignore)]
        public string StatusMessage { get; internal set; }

        /// <summary>
        /// Different number of devices in the job.
        /// </summary>
        [JsonProperty(PropertyName = "deviceJobStatistics", NullValueHandling = NullValueHandling.Ignore)]
        public DeviceJobStatistics DeviceJobStatistics { get; internal set; }

        /// <summary>
        /// The deviceId related to this response.
        /// </summary>
        /// <remarks>
        /// It can be null (e.g. in case of a parent orchestration).
        /// </remarks>
        [JsonProperty(PropertyName = "deviceId", NullValueHandling = NullValueHandling.Ignore)]
        public string DeviceId { get; internal set; }

        /// <summary>
        /// The jobId of the parent orchestration, if any.
        /// </summary>
        [JsonProperty(PropertyName = "parentJobId", NullValueHandling = NullValueHandling.Ignore)]
        public string ParentJobId { get; internal set; }
    }

    /// <summary>
    /// The job counts, e.g., number of failed/succeeded devices.
    /// </summary>
    public class DeviceJobStatistics
    {
        /// <summary>
        /// Number of devices in the job.
        /// </summary>
        [JsonProperty(PropertyName = "deviceCount")]
        public int DeviceCount { get; internal set; }

        /// <summary>
        /// The number of failed jobs.
        /// </summary>
        [JsonProperty(PropertyName = "failedCount")]
        public int FailedCount { get; internal set; }

        /// <summary>
        /// The number of Successed jobs.
        /// </summary>
        [JsonProperty(PropertyName = "succeededCount")]
        public int SucceededCount { get; internal set; }

        /// <summary>
        /// The number of running jobs.
        /// </summary>
        [JsonProperty(PropertyName = "runningCount")]
        public int RunningCount { get; internal set; }

        /// <summary>
        /// The number of pending (scheduled) jobs.
        /// </summary>
        [JsonProperty(PropertyName = "pendingCount")]
        public int PendingCount { get; internal set; }
    }
}
