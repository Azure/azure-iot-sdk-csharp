// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Specifies the various job status for a job.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum JobStatus
    {
        /// <summary>
        /// Unknown status.
        /// </summary>
        [EnumMember(Value = "unknown")]
        Unknown,

        /// <summary>
        /// Indicates that a job is in the queue for execution.
        /// </summary>
        [EnumMember(Value = "enqueued")]
        Enqueued,

        /// <summary>
        /// Indicates that a job is running.
        /// </summary>
        [EnumMember(Value = "running")]
        Running,

        /// <summary>
        /// Indicates that a job execution is completed.
        /// </summary>
        [EnumMember(Value = "completed")]
        Completed,

        /// <summary>
        /// Indicates that a job execution failed.
        /// </summary>
        [EnumMember(Value = "failed")]
        Failed,

        /// <summary>
        /// Indicates that a job execution was cancelled.
        /// </summary>
        [EnumMember(Value = "cancelled")]
        Cancelled,

        /// <summary>
        /// Indicates that a job is scheduled for a future datetime.
        /// </summary>
        [EnumMember(Value = "scheduled")]
        Scheduled,

        /// <summary>
        /// Indicates that a job is in the queue for execution (synonym for enqueued to be deprecated).
        /// </summary>
        [EnumMember(Value = "queued")]
        Queued,
    }
}
