// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Enrollment status
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1717:OnlyFlagsEnumsShouldHavePluralNames",
        Justification = "Public API cannot change name.")]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EnrollmentStatus
    {
        /// <summary>
        /// Device has not yet come on-line
        /// </summary>
        [EnumMember(Value = "unassigned")]
        Unassigned = 1,

        /// <summary>
        /// Device has connected to the DRS but IoT Hub Id has not yet been returned to the device.
        /// </summary>
        [EnumMember(Value = "assigning")]
        Assigning = 2,

        /// <summary>
        /// DRS successfully returned a device Id and connection string to the device.
        /// </summary>
        [EnumMember(Value = "assigned")]
        Assigned = 3,

        /// <summary>
        /// Device enrollment failed.
        /// </summary>
        [EnumMember(Value = "failed")]
        Failed = 4,

        /// <summary>
        /// Device is disabled.
        /// </summary>
        [EnumMember(Value = "disabled")]
        Disabled = 5,
    }
}
