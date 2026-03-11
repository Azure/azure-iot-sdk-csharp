// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

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
        public CloudToDeviceMethodScheduledJob(DirectMethodServiceRequest directMethodRequest)
        {
            JobType = JobType.ScheduleDeviceMethod;
            DirectMethodRequest = directMethodRequest;
        }

        /// <summary>
        /// Contains parameters to execute a direct method on a device or module.
        /// </summary>
        [JsonPropertyName("cloudToDeviceMethod")]
        public DirectMethodServiceRequest DirectMethodRequest { get; set; }
    }
}
