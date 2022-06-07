// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Specifies the different states of a device.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1717:OnlyFlagsEnumsShouldHavePluralNames",
        Justification = "Public API cannot change name.")]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DeviceStatus
    {
        /// <summary>
        /// Indicates that a Device is enabled
        /// </summary>
        [EnumMember(Value = "enabled")]
        Enabled = 0,

        /// <summary>
        /// Indicates that a Device is disabled
        /// </summary>
        [EnumMember(Value = "disabled")]
        Disabled,
    }
}