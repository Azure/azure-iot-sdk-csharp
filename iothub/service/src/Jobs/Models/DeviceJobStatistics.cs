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
        [JsonProperty(PropertyName = "deviceCount")]
        public int DeviceCount { get; internal set; }

        /// <summary>
        /// The number of failed jobs.
        /// </summary>
        [JsonProperty(PropertyName = "failedCount")]
        public int FailedCount { get; internal set; }

        /// <summary>
        /// The number of successed jobs.
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
