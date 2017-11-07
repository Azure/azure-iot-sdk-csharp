// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Identifies the behavior when merging a device to the registry during import actions.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ImportMode
    {
        [EnumMember(Value = "createOrUpdate")]
        CreateOrUpdate = 0,

        [EnumMember(Value = "create")]
        Create = 1,

        [EnumMember(Value = "update")]
        Update = 2,

        [EnumMember(Value = "updateIfMatchETag")]
        UpdateIfMatchETag = 3,

        [EnumMember(Value = "createOrUpdateIfMatchETag")]
        CreateOrUpdateIfMatchETag = 4,

        [EnumMember(Value = "delete")]
        Delete = 5,

        [EnumMember(Value = "deleteIfMatchETag")]
        DeleteIfMatchETag = 6,

        [EnumMember(Value = "updateTwin")]
        UpdateTwin = 7,

        [EnumMember(Value = "updateTwinIfMatchETag")]
        UpdateTwinIfMatchETag = 8
    }
}
