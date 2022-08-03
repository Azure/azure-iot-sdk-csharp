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
        [JsonProperty(PropertyName = "queryCondition", Required = Required.Always)]
        public string queryCondition { get; set; }

        /// <summary>
        /// Method call parameters
        /// </summary>
        [JsonProperty(PropertyName = "cloudToDeviceMethod", Required = Required.Always)]
        public CloudToDeviceMethod cloudToDeviceMethod { get; set; }

        /// <summary>
        /// Date time in Utc to start the job
        /// </summary>\
        [JsonProperty(PropertyName = "startTimeUtc", Required = Required.Always)]
        public DateTime startTimeUtc { get; set; }

        /// <summary>
        /// Max execution time in seconds, i.e., ttl duration the job can run
        /// </summary>\
        [JsonProperty(PropertyName = "maxExecutionTimeInSeconds", Required = Required.Always)]
        public long maxExecutionTimeInSeconds { get; set; }
    }
}
