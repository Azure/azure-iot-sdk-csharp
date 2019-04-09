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
        /// Shard by hashcode 
        /// </summary>
        [EnumMember(Value = "hashed")]
        Hashed = 0,

        /// <summary>
        /// Shard by geo location 
        /// </summary>
        [EnumMember(Value = "geoLatency")]
        GeoLatency = 1,

        /// <summary>
        /// Shard by static setting
        /// </summary>
        [EnumMember(Value = "static")]
        Static = 2,

        /// <summary>
        /// Shard by static customized setting
        /// </summary>
        [EnumMember(Value = "custom")]
        Custom = 3
    }
}