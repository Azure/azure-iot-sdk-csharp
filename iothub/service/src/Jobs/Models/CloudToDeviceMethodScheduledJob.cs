// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains properties of a cloud-to-device method scheduled job.
    /// </summary>
    public class CloudToDeviceMethodScheduledJob : ScheduledJob
    {
        /// <summary>
        /// [Required] The method type and parameters.
        /// </summary>
        [JsonProperty(PropertyName = "cloudToDeviceMethod", NullValueHandling = NullValueHandling.Ignore)]
        public DirectMethodRequest DirectMethodRequest { get; set; }
    }
}
