using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains fields used when creating a job to update twin tags and desired properties on one or multiple devices.
    /// </summary>
    public class ScheduledTwinUpdate
    {
        /// <summary>
        /// Query condition to evaluate which devices to run the job on.
        /// </summary>
        [JsonProperty(PropertyName = "queryCondition", Required = Required.Always)]
        public string QueryCondition { get; set; }

        /// <summary>
        /// Twin object to use for the update.
        /// </summary>
        [JsonProperty(PropertyName = "twin", Required = Required.Always)]
        public Twin Twin { get; set; }

        /// <summary>
        /// Date time in UTC to start the job.
        /// </summary>
        [JsonProperty(PropertyName = "startTimeUtc", Required = Required.Always)]
        public DateTime StartTimeUtc { get; set; }
    }
}
