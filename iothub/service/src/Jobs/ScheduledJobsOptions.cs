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
