// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Specifies the configuration status.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ConfigurationStatus
    {
        /// <summary>
        /// Configuration targeted.
        /// </summary>
        [EnumMember(Value = "targeted")]
        Targeted = 1,

        /// <summary>
        /// Configuration applied.
        /// </summary>
        [EnumMember(Value = "applied")]
        Applied = 2,
    }
}
