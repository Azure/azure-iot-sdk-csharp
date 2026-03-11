// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

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
        [JsonProperty("deviceCount")]
        public int DeviceCount { get; protected internal set; }

        /// <summary>
        /// The number of failed jobs.
        /// </summary>
        [JsonProperty("failedCount")]
        public int FailedCount { get; protected internal set; }

        /// <summary>
        /// The number of successed jobs.
        /// </summary>
        [JsonProperty("succeededCount")]
        public int SucceededCount { get; protected internal set; }

        /// <summary>
        /// The number of running jobs.
        /// </summary>
        [JsonProperty("runningCount")]
        public int RunningCount { get; protected internal set; }

        /// <summary>
        /// The number of pending (scheduled) jobs.
        /// </summary>
        [JsonProperty("pendingCount")]
        public int PendingCount { get; protected internal set; }
    }
}
