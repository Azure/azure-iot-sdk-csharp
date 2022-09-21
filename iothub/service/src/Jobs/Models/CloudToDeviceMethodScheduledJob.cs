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
        /// Serialization constructor.
        /// </summary>
        protected internal CloudToDeviceMethodScheduledJob()
        { }

        /// <summary>
        /// Creates an instance of this class for cloud-to-device method scheduled job.
        /// </summary>
        /// <param name="directMethodRequest">Contains parameters to execute a direct method on a device or module.</param>
        public CloudToDeviceMethodScheduledJob(DirectMethodRequest directMethodRequest)
        {
            DirectMethodRequest = directMethodRequest;
        }

        /// <summary>
        /// Contains parameters to execute a direct method on a device or module.
        /// </summary>
        [JsonProperty(PropertyName = "cloudToDeviceMethod", NullValueHandling = NullValueHandling.Ignore)]
        public DirectMethodRequest DirectMethodRequest { get; set; }
    }
}
