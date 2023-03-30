// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.LongHaul.Service
{
    /// <summary>
    /// The device event message source.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DeviceEventMessageSource
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Device telemetry events.
        /// </summary>
        [EnumMember(Value = "Telemetry")]
        Telemetry,

        /// <summary>
        /// Device connection and disconnection events.
        /// </summary>
        [EnumMember(Value = "deviceConnectionStateEvents")]
        DeviceConnectionStateEvents,

        /// <summary>
        /// Twin change events.
        /// </summary>
        [EnumMember(Value = "twinChangeEvents")]
        TwinChangeEvents,

        /// <summary>
        /// Digital twin change events.
        /// </summary>
        [EnumMember(Value = "digitalTwinChangeEvents")]
        DigitalTwinChangeEvents,
    }
}
