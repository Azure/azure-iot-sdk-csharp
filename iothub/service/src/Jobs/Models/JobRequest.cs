﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Job input.
    /// </summary>
    internal class JobRequest
    {
        /// <summary>
        /// Job identifier.
        /// </summary>
        [JsonProperty("jobId")]
        internal string JobId { get; set; }

        /// <summary>
        /// [Required] The type of job to execute.
        /// </summary>
        [JsonProperty("type")]
        internal JobType JobType { get; set; }

        /// <summary>
        /// The method type and parameters.
        /// </summary>
        /// <remarks>
        /// Required if jobType is cloud-to-device method.
        /// </remarks>
        [JsonProperty("cloudToDeviceMethod")]
        internal DirectMethodServiceRequest DirectMethodRequest { get; set; }

        /// <summary>
        /// The Update twin tags and desired properties.
        /// </summary>
        /// <remarks>
        /// Required if the job type is update twin.
        /// </remarks>
        [JsonProperty("updateTwin")]
        internal ClientTwin UpdateTwin { get; set; }

        /// <summary>
        /// Condition for device query to get devices to execute the job on.
        /// </summary>
        /// <remarks>
        /// Required if job type is update twin or cloud-to-device method.
        /// </remarks>
        [JsonProperty("queryCondition")]
        internal string QueryCondition { get; set; }

        /// <summary>
        /// ISO 8601 date time to start the job.
        /// </summary>
        [JsonProperty("startTime")]
        [JsonConverter(typeof(IsoDateTimeConverter))]
        internal DateTimeOffset StartOn { get; set; }

        /// <summary>
        /// Max execution time in seconds (TTL duration).
        /// </summary>
        [JsonIgnore]
        internal TimeSpan? MaxExecutionTime { get; set; }

        [JsonProperty("maxExecutionTimeInSeconds", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal long? MaxExecutionTimeInSeconds
        {
            get => MaxExecutionTime != null ? (long)MaxExecutionTime?.TotalSeconds : null;
            set => MaxExecutionTime = value != null ? TimeSpan.FromSeconds((int)value) : null;
        }
    }
}
