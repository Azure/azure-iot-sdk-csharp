// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// The Device Provisioning Service enrollment level allocation policies.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AllocationPolicy
    {
        /// <summary>
        /// Use hashing during allocation
        /// </summary>
        [EnumMember(Value = "hashed")]
        Hashed = 0,

        /// <summary>
        /// Use geoLatency during allocation
        /// </summary>
        [EnumMember(Value = "geoLatency")]
        GeoLatency = 1,

        /// <summary>
        /// Use a static allocation
        /// </summary>
        [EnumMember(Value = "static")]
        Static = 2,

        /// <summary>
        /// Use a custom allocation
        /// </summary>
        [EnumMember(Value = "custom")]
        Custom = 3
    }
}