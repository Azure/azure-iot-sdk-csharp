// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains the properties set by the service import/export job.
    /// </summary>
    public class IotHubJobResponse
    {
        private static readonly JobStatus[] s_finishedStates = new[]
        {
            JobStatus.Completed,
            JobStatus.Failed,
            JobStatus.Cancelled
        };

        /// <summary>
        /// The unique Id of the job.
        /// </summary>
        /// <remarks>
        /// This value is created by the service. If specified by the user, it will be ignored.
        /// </remarks>
        [JsonPropertyName("jobId")]
        public string JobId { get; set; }

        /// <summary>
        /// When the job started running.
        /// </summary>
        /// <remarks>
        /// This value is created by the service. If specified by the user, it will be ignored.
        /// </remarks>
        [JsonPropertyName("startTimeUtc")]
        public DateTimeOffset? StartedOnUtc { get; set; }

        /// <summary>
        /// When the job finished.
        /// </summary>
        /// <remarks>
        /// This value is created by the service. If specified by the user, it will be ignored.
        /// </remarks>
        [JsonPropertyName("endTimeUtc")]
        public DateTimeOffset? EndedOnUtc { get; set; }

        /// <summary>
        /// The status of the job.
        /// </summary>
        /// <remarks>
        /// This value is created by the service. If specified by the user, it will be ignored.
        /// </remarks>
        [JsonPropertyName("status")]
        public JobStatus Status { get; set; }

        /// <summary>
        /// If status == failure, this represents a string containing the reason.
        /// </summary>
        /// <remarks>
        /// This value is created by the service. If specified by the user, it will be ignored.
        /// </remarks>
        [JsonPropertyName("failureReason")]
        public string FailureReason { get; set; }

        /// <summary>
        /// Convenience property to determine if the job is in a terminal state, based on <see cref="Status"/>.
        /// </summary>
        [JsonIgnore]
        public bool IsFinished => s_finishedStates.Contains(Status);
    }
}
