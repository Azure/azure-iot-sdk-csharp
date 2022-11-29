// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains properties of a cloud-to-device method scheduled job.
    /// </summary>
    public class CloudToDeviceMethodScheduledJob : ScheduledJob
    {
        /// <summary>
        /// For deserialization.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public CloudToDeviceMethodScheduledJob()
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
