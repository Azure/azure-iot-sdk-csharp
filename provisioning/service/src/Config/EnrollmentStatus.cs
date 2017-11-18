// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Enrollment status
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EnrollmentStatus
    {
        /// <summary>
        /// Device has not yet come on-line
        /// </summary>
        Unassigned = 1,

        /// <summary>
        /// Device has connected to the DRS but IoT Hub ID has not yet been returned to the device
        /// </summary>
        Assigning = 2,

        /// <summary>
        /// DRS successfully returned a device ID and connection string to the device
        /// </summary>
        Assigned = 3,

        /// <summary>
        /// Device enrollment failed
        /// </summary>
        Failed = 4,

        /// <summary>
        /// Device is disabled
        /// </summary>
        Disabled = 5
    }
}
