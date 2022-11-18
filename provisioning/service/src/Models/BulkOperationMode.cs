// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// The Device Provisioning Service bulk operation modes.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BulkOperationMode
    {
        /// <summary>
        /// Create mode.
        /// </summary>
        [EnumMember(Value = "create")]
        Create,

        /// <summary>
        /// Update mode.
        /// </summary>
        [EnumMember(Value = "update")]
        Update,

        /// <summary>
        /// Update only if the ETag matches.
        /// </summary>
        [EnumMember(Value = "updateIfMatchETag")]
        UpdateIfMatchETag,

        /// <summary>
        /// Delete mode.
        /// </summary>
        [EnumMember(Value = "delete")]
        Delete,
    }
}
