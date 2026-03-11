// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The job counts, e.g., number of failed/succeeded devices.
    /// </summary>
    public class DeviceJobStatistics
    {
        /// <summary>
        /// Number of devices in the job.
        /// </summary>
        [JsonPropertyName("deviceCount")]
        public int DeviceCount { get; protected internal set; }

        /// <summary>
        /// The number of failed jobs.
        /// </summary>
        [JsonPropertyName("failedCount")]
        public int FailedCount { get; protected internal set; }

        /// <summary>
        /// The number of successed jobs.
        /// </summary>
        [JsonPropertyName("succeededCount")]
        public int SucceededCount { get; protected internal set; }

        /// <summary>
        /// The number of running jobs.
        /// </summary>
        [JsonPropertyName("runningCount")]
        public int RunningCount { get; protected internal set; }

        /// <summary>
        /// The number of pending (scheduled) jobs.
        /// </summary>
        [JsonPropertyName("pendingCount")]
        public int PendingCount { get; protected internal set; }
    }
}
