// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains optional fields when creating a job.
    /// </summary>
    public class ScheduledJobsOptions
    {
        /// <summary>
        /// Unique Id for the job.
        /// </summary>
        [JsonPropertyName("jobId")]
        public string JobId { get; set; }

        /// <summary>
        /// Max execution time (TTL duration).
        /// </summary>
        /// <remarks>The precision on this is in seconds.</remarks>
        [JsonPropertyName("maxExecutionTimeInSeconds")]
        public long MaxExecutionTimeInSeconds { get; set; }
    }
}
