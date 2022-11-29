﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

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
        [EditorBrowsable(EditorBrowsableState.Never)]
        public IotHubScheduledJobResponse()
        { }

        /// <summary>
        /// The job Id.
        /// </summary>
        [JsonPropertyName("jobId")]
        public string JobId { get; internal set; }

        /// <summary>
        /// Status of the job.
        /// </summary>
        [JsonPropertyName("status")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public JobStatus Status { get; internal set; }

        /// <summary>
        /// Scheduled job start time in UTC.
        /// </summary>
        [JsonPropertyName("createdDateTimeUtc")]
        public DateTimeOffset? CreatedOnUtc { get; internal set; }

        // Some service Jobs APIs use "createdTime" as the key for this value and some others use "createdDateTimeUtc".
        // This private field is a workaround that allows us to deserialize either "createdTime" or "createdDateTimeUtc"
        // as the created time value for this class and expose it either way as CreatedTimeUtc.
        [JsonPropertyName("createdTime")]
        internal DateTimeOffset? AlternateCreatedOnUtc
        {
            get => CreatedOnUtc;
            set => CreatedOnUtc = value;
        }

        /// <summary>
        /// Represents the time the job started processing.
        /// </summary>
        [JsonPropertyName("startTimeUtc")]
        public DateTimeOffset? StartedOnUtc { get; internal set; }

        /// <summary>
        /// Represents the time the job stopped processing.
        /// </summary>
        [JsonPropertyName("endTimeUtc")]
        public DateTimeOffset? EndedOnUtc { get; internal set; }

        // Some service Jobs APIs use "endTime" as the key for this value and some others use "endTimeUtc".
        // This private field is a workaround that allows us to deserialize either "endTime" or "endTimeUtc"
        // as the created time value for this class and expose it either way as EndTimeUtc.
        [JsonPropertyName("endTime")]
        internal DateTimeOffset? AlternateEndedOnUtc
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
        [JsonPropertyName("failureReason")]
        public string FailureReason { get; internal set; }

        /// <summary>
        /// Represents a string containing a message with status about the job execution.
        /// </summary>
        [JsonPropertyName("statusMessage")]
        public string StatusMessage { get; internal set; }
    }
}
