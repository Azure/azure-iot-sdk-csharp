using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains fields used when creating a job to run a device method on one or multiple devices.
    /// </summary>
    public class ScheduledDeviceMethod
    {
        /// <summary>
        /// Query condition to evaluate which devices to run the job on.
        /// </summary>
        [JsonProperty(PropertyName = "QueryCondition", Required = Required.Always)]
        public string QueryCondition { get; set; }

        /// <summary>
        /// Method call parameters.
        /// </summary>
        [JsonProperty(PropertyName = "CloudToDeviceMethod", Required = Required.Always)]
        public CloudToDeviceMethod CloudToDeviceMethod { get; set; }

        /// <summary>
        /// Date time in UTC to start the job.
        /// </summary>\
        [JsonProperty(PropertyName = "StartTimeUtc", Required = Required.Always)]
        public DateTime StartTimeUtc { get; set; }

        /// <summary>
        /// Max execution time in seconds, i.e., ttl duration the job can run.
        /// </summary>\
        [JsonProperty(PropertyName = "MaxExecutionTimeInSeconds", Required = Required.Always)]
        public long MaxExecutionTimeInSeconds { get; set; }
    }
}
