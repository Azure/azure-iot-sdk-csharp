// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains properties set by the service for scheduled job.
    /// </summary>
    public abstract class IotHubScheduledJobResponse
    {
        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes and serialization.
        /// </summary>
        protected internal IotHubScheduledJobResponse()
        { }

        /// <summary>
        /// The job Id.
        /// </summary>
        [JsonProperty(PropertyName = "jobId", NullValueHandling = NullValueHandling.Ignore)]
        public string JobId { get; internal set; }

        /// <summary>
        /// Status of the job.
        /// </summary>
        [JsonProperty(PropertyName = "status", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public JobStatus Status { get; internal set; }

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
        /// Represents the time the job started processing.
        /// </summary>
        [JsonProperty(PropertyName = "startTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? StartedOnUtc { get; internal set; }

        /// <summary>
        /// Represents the time the job stopped processing.
        /// </summary>
        [JsonProperty(PropertyName = "endTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? EndedOnUtc { get; internal set; }

        // Some service Jobs APIs use "endTime" as the key for this value and some others use "endTimeUtc".
        // This private field is a workaround that allows us to deserialize either "endTime" or "endTimeUtc"
        // as the created time value for this class and expose it either way as EndTimeUtc.
        [JsonProperty(PropertyName = "endTime")]
        internal DateTime? AlternateEndedOnUtc
        {
            get => EndedOnUtc;
            set => EndedOnUtc = value;
        }

        /// <summary>
        /// Represents the reason for job failure.
        /// </summary>
        /// <remarks>
        /// If status is <see cref="JobStatus.Failed"/>, this represents a string containing the reason.
        /// </remarks>
        [JsonProperty(PropertyName = "failureReason", NullValueHandling = NullValueHandling.Ignore)]
        public string FailureReason { get; internal set; }

        /// <summary>
        /// Represents a string containing a message with status about the job execution.
        /// </summary>
        [JsonProperty(PropertyName = "statusMessage", NullValueHandling = NullValueHandling.Ignore)]
        public string StatusMessage { get; internal set; }
    }
}
