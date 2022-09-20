// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains the properties set by the service import/export job.
    /// </summary>
    public abstract class Job
    {
        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        internal Job()
        { }

        /// <summary>
        /// The unique Id of the job.
        /// </summary>
        /// <remarks>
        /// This value is created by the service. If specified by the user, it will be ignored.
        /// </remarks>
        [JsonProperty(PropertyName = "jobId", NullValueHandling = NullValueHandling.Ignore)]
        public string JobId { get; internal set; }

        /// <summary>
        /// When the job started running.
        /// </summary>
        /// <remarks>
        /// This value is created by the service. If specified by the user, it will be ignored.
        /// </remarks>
        [JsonProperty(PropertyName = "startTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? StartedOnUtc { get; internal set; }

        /// <summary>
        /// When the job finished.
        /// </summary>
        /// <remarks>
        /// This value is created by the service. If specified by the user, it will be ignored.
        /// </remarks>
        [JsonProperty(PropertyName = "endTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? EndedOnUtc { get; internal set; }

        /// <summary>
        /// The status of the job.
        /// </summary>
        /// <remarks>
        /// This value is created by the service. If specified by the user, it will be ignored.
        /// </remarks>
        [JsonProperty(PropertyName = "status", NullValueHandling = NullValueHandling.Ignore)]
        public JobStatus Status { get; internal set; }

        /// <summary>
        /// If status == failure, this represents a string containing the reason.
        /// </summary>
        /// <remarks>
        /// This value is created by the service. If specified by the user, it will be ignored.
        /// </remarks>
        [JsonProperty(PropertyName = "failureReason", NullValueHandling = NullValueHandling.Ignore)]
        public string FailureReason { get; internal set; }
    }
}
