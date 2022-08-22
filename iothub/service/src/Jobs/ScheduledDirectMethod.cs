// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains fields used when creating a job to run a device method on one or multiple devices.
    /// </summary>
    public class ScheduledDirectMethod
    {
        /// <summary>
        /// Query condition to evaluate which devices to run the job on.
        /// </summary>
        [JsonProperty(PropertyName = "queryCondition", Required = Required.Always)]
        public string QueryCondition { get; set; }

        /// <summary>
        /// Method call parameters.
        /// </summary>
        [JsonProperty(PropertyName = "cloudToDeviceMethod", Required = Required.Always)]
        public DirectMethodRequest DirectMethodRequest { get; set; }

        /// <summary>
        /// Date time in UTC to start the job.
        /// </summary>\
        [JsonProperty(PropertyName = "startTimeUtc", Required = Required.Always)]
        public DateTime StartTimeUtc { get; set; }
    }
}
