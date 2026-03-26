// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Job input.
    /// </summary>
    public sealed class JobRequest
    {
        /// <summary>
        /// Job identifier.
        /// </summary>
        [JsonPropertyName("jobId")]
        public string JobId { get; set; }

        /// <summary>
        /// [Required] The type of job to execute.
        /// </summary>
        [JsonPropertyName("type")]
        public JobType JobType { get; set; }

        /// <summary>
        /// The method type and parameters.
        /// </summary>
        /// <remarks>
        /// Required if jobType is cloud-to-device method.
        /// </remarks>
        [JsonPropertyName("cloudToDeviceMethod")]
        public DirectMethodServiceRequest DirectMethodRequest { get; set; }

        /// <summary>
        /// The Update twin tags and desired properties.
        /// </summary>
        /// <remarks>
        /// Required if the job type is update twin.
        /// </remarks>
        [JsonPropertyName("updateTwin")]
        public ClientTwin UpdateTwin { get; set; }

        /// <summary>
        /// Condition for device query to get devices to execute the job on.
        /// </summary>
        /// <remarks>
        /// Required if job type is update twin or cloud-to-device method.
        /// </remarks>
        [JsonPropertyName("queryCondition")]
        public string QueryCondition { get; set; }

        /// <summary>
        /// ISO 8601 date time to start the job.
        /// </summary>
        [JsonPropertyName("startTime")]
        public DateTimeOffset StartOn { get; set; }

        /// <summary>
        /// Max execution time in seconds (TTL duration).
        /// </summary>
        [JsonPropertyName("maxExecutionTimeInSeconds")]
        public long? MaxExecutionTimeInSeconds { get; set; }
    }
}
