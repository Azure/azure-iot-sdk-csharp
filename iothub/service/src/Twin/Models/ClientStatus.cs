// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Specifies the different states of a device.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ClientStatus
    {
        /// <summary>
        /// Indicates that a device is enabled.
        /// </summary>
        [EnumMember(Value = "enabled")]
        Enabled = 0,

        /// <summary>
        /// Indicates that a device is disabled.
        /// </summary>
        [EnumMember(Value = "disabled")]
        Disabled,
    }
}
