// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Device job type.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DeviceJobType
    {
        /// <summary>
        /// Unknown job type.
        /// </summary>
        [EnumMember(Value = "unknown")]
        Unknown = 0,

        /// <summary>
        /// Schedule direct method job type.
        /// </summary>
        [EnumMember(Value = "scheduleDeviceMethod")]
        ScheduleDeviceMethod = 1,

        /// <summary>
        /// Schedule update twin job type.
        /// </summary>
        [EnumMember(Value = "scheduleUpdateTwin")]
        ScheduleUpdateTwin = 2
    }
}
