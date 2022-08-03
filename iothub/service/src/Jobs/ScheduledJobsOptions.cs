using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// 
    /// </summary>
    public class ScheduledJobsOptions
    {
        /// <summary>
        /// Unique Job Id for the twin update job.
        /// </summary>
        [JsonProperty(PropertyName = "JobId", Required = Required.Default)]
        public string JobId { get; set; }

        /// <summary>
        /// Max execution time in seconds, i.e., ttl duration the job can run.
        /// </summary>
        [JsonProperty(PropertyName = "MaxExecutionTimeInSeconds", Required = Required.Default)]
        public long MaxExecutionTimeInSeconds { get; set; }
    }
}
