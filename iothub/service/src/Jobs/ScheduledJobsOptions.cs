// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains optional fields when creating a job.
    /// </summary>
    public class ScheduledJobsOptions
    {
        /// <summary>
        /// Unique job Id for the job.
        /// </summary>
        [JsonProperty(PropertyName = "jobId", Required = Required.Default)]
        public string JobId { get; set; }

        /// <summary>
        /// Max execution time in seconds (TTL duration).
        /// </summary>
        [JsonIgnore]
        public TimeSpan MaxExecutionTime { get; set; }

        [JsonProperty(PropertyName = "maxExecutionTimeInSeconds", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal long MaxExecutionTimeInSeconds
        {
            get => (long)MaxExecutionTime.TotalSeconds;
            set => MaxExecutionTime = TimeSpan.FromSeconds(value);
        }
    }
}
