// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Specifies the various job status for a job.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum JobStatus
    {
        /// <summary>
        /// Unknown status.
        /// </summary>
        Unknown,

        /// <summary>
        /// Indicates that a job is in the queue for execution.
        /// </summary>
        Enqueued,

        /// <summary>
        /// Indicates that a job is running.
        /// </summary>
        Running,

        /// <summary>
        /// Indicates that a job execution is completed.
        /// </summary>
        Completed,

        /// <summary>
        /// Indicates that a job execution failed.
        /// </summary>
        Failed,

        /// <summary>
        /// Indicates that a job execution was cancelled.
        /// </summary>
        Cancelled,

        /// <summary>
        /// Indicates that a job is scheduled for a future datetime.
        /// </summary>
        Scheduled,

        /// <summary>
        /// Indicates that a job is in the queue for execution (synonym for enqueued to be deprecated).
        /// </summary>
        Queued,
    }
}
