// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Job input.
    /// </summary>
    public class JobRequest
    {
        /// <summary>
        /// Job identifier.
        /// </summary>
        [JsonProperty(PropertyName = "jobId", Required = Required.Always)]
        public string JobId { get; set; }

        /// <summary>
        /// Required.
        /// The type of job to execute.
        /// </summary>
        [JsonProperty(PropertyName = "type", Required = Required.Always)]
        public JobType JobType { get; set; }

        /// <summary>
        /// The method type and parameters.
        /// </summary>
        /// /// <remarks>
        /// Required if jobType is cloudToDeviceMethod.
        /// </remarks>
        [JsonProperty(PropertyName = "cloudToDeviceMethod")]
        public CloudToDeviceMethod CloudToDeviceMethod { get; set; }

        /// <summary>
        /// The Update Twin tags and desired properties.
        /// </summary>
        /// <remarks>
        /// Required if the job type is UpdateTwin.
        /// </remarks>
        [JsonProperty(PropertyName = "updateTwin")]
        public Twin UpdateTwin { get; set; }

        /// <summary>
        /// Condition for device query to get devices to execute the job on.
        /// </summary>
        /// <remarks>
        /// Required if job type is UpdateTwin or CloudToDeviceMethod.
        /// </remarks>
        [JsonProperty(PropertyName = "queryCondition")]
        public string QueryCondition { get; set; }

        /// <summary>
        /// ISO 8601 date time to start the job.
        /// </summary>
        [JsonProperty(PropertyName = "startTimeUtc")]
        [JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime StartTimeUtc { get; set; }

        /// <summary>
        /// Max execution time in seconds (TTL duration).
        /// </summary>
        [JsonIgnore]
        public TimeSpan? MaxExecutionTime { get; set; }

        [JsonProperty(PropertyName = "maxExecutionTimeInSeconds", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal long? MaxExecutionTimeInSeconds
        {
            get => (long)MaxExecutionTime?.TotalSeconds;
            set => MaxExecutionTime = value != null ? TimeSpan.FromSeconds((int)value) : null;
        }
    }
}
