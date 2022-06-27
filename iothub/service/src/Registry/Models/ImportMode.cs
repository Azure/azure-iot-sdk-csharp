// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ---------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Identifies the behavior when merging a device to the registry during import actions.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ImportMode
    {
        /// <summary>
        /// If a device does not exist with the specified Id, it is newly registered.
        /// If the device already exists, existing information is overwritten with the provided input data without regard to the ETag value.
        /// </summary>
        [EnumMember(Value = "createOrUpdate")]
        CreateOrUpdate = 0,

        /// <summary>
        /// If a device does not exist with the specified Id, it is newly registered.
        /// If the device already exists, an error is written to the log file.
        /// </summary>
        [EnumMember(Value = "create")]
        Create = 1,

        /// <summary>
        /// If a device already exists with the specified Id, existing information is overwritten with the provided input data without regard to the ETag value.
        /// If the device does not exist, an error is written to the log file.
        /// </summary>
        [EnumMember(Value = "update")]
        Update = 2,

        /// <summary>
        /// If a device already exists with the specified Id, existing information is overwritten with the provided input data only if there is an ETag match.
        /// If the device does not exist, or there is an ETag mismatch, an error is written to the log file.
        /// </summary>
        [EnumMember(Value = "updateIfMatchETag")]
        UpdateIfMatchETag = 3,

        /// <summary>
        /// If a device does not exist with the specified Id, it is newly registered.
        /// If the device already exists, existing information is overwritten with the provided input data only if there is an ETag match.
        /// If there is an ETag mismatch, an error is written to the log file.
        /// </summary>
        [EnumMember(Value = "createOrUpdateIfMatchETag")]
        CreateOrUpdateIfMatchETag = 4,

        /// <summary>
        /// If a device already exists with the specified Id, it is deleted without regard to the ETag value.
        /// If the device does not exist, an error is written to the log file.
        /// </summary>
        [EnumMember(Value = "delete")]
        Delete = 5,

        /// <summary>
        /// If a device already exists with the specified Id, it is deleted only if there is an ETag match. If the device does not exist, an error is written to the log file.
        /// If there is an ETag mismatch, an error is written to the log file.
        /// </summary>
        [EnumMember(Value = "deleteIfMatchETag")]
        DeleteIfMatchETag = 6,

        /// <summary>
        /// If a twin already exists with the specified Id, existing information is overwritten with the provided input data
        /// without regard to the ETag value.
        /// </summary>
        [EnumMember(Value = "updateTwin")]
        UpdateTwin = 7,

        /// <summary>
        /// If a twin already exists with the specified Id, existing information is overwritten with the provided input data only if there is an ETag match.
        /// The twin's ETag, is processed independently from the device's ETag. If there is a mismatch with the existing twin's ETag,
        /// an error is written to the log file.
        /// </summary>
        [EnumMember(Value = "updateTwinIfMatchETag")]
        UpdateTwinIfMatchETag = 8
    }
}
