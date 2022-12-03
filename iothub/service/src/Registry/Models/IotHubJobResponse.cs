// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Newtonsoft.Json;

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
        /// Creates an instance of this class. Provided for unit testing purposes and serialization.
        /// </summary>
        /// <remarks>
        /// This class can be inherited from and set by unit tests for mocking purposes.
        /// </remarks>
        protected internal IotHubJobResponse()
        { }

        /// <summary>
        /// The unique Id of the job.
        /// </summary>
        /// <remarks>
        /// This value is created by the service. If specified by the user, it will be ignored.
        /// </remarks>
        [JsonProperty("jobId", NullValueHandling = NullValueHandling.Ignore)]
        public string JobId { get; protected internal set; }

        /// <summary>
        /// When the job started running.
        /// </summary>
        /// <remarks>
        /// This value is created by the service. If specified by the user, it will be ignored.
        /// </remarks>
        [JsonProperty("startTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? StartedOnUtc { get; protected internal set; }

        /// <summary>
        /// When the job finished.
        /// </summary>
        /// <remarks>
        /// This value is created by the service. If specified by the user, it will be ignored.
        /// </remarks>
        [JsonProperty("endTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? EndedOnUtc { get; protected internal set; }

        /// <summary>
        /// The status of the job.
        /// </summary>
        /// <remarks>
        /// This value is created by the service. If specified by the user, it will be ignored.
        /// </remarks>
        [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
        public JobStatus Status { get; protected internal set; }

        /// <summary>
        /// If status == failure, this represents a string containing the reason.
        /// </summary>
        /// <remarks>
        /// This value is created by the service. If specified by the user, it will be ignored.
        /// </remarks>
        [JsonProperty("failureReason", NullValueHandling = NullValueHandling.Ignore)]
        public string FailureReason { get; protected internal set; }

        /// <summary>
        /// Convenience property to determine if the job is in a terminal state, based on <see cref="Status"/>.
        /// </summary>
        [JsonIgnore]
        public bool IsFinished => s_finishedStates.Contains(Status);
    }
}
