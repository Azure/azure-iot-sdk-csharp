// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        CreateOrUpdate,

        /// <summary>
        /// If a device does not exist with the specified Id, it is newly registered.
        /// If the device already exists, an error is written to the log file.
        /// </summary>
        Create,

        /// <summary>
        /// If a device already exists with the specified Id, existing information is overwritten with the provided input data without regard to the ETag value.
        /// If the device does not exist, an error is written to the log file.
        /// </summary>
        Update,

        /// <summary>
        /// If a device already exists with the specified Id, existing information is overwritten with the provided input data only if there is an ETag match.
        /// If the device does not exist, or there is an ETag mismatch, an error is written to the log file.
        /// </summary>
        UpdateIfMatchETag,

        /// <summary>
        /// If a device does not exist with the specified Id, it is newly registered.
        /// If the device already exists, existing information is overwritten with the provided input data only if there is an ETag match.
        /// If there is an ETag mismatch, an error is written to the log file.
        /// </summary>
        CreateOrUpdateIfMatchETag,

        /// <summary>
        /// If a device already exists with the specified Id, it is deleted without regard to the ETag value.
        /// If the device does not exist, an error is written to the log file.
        /// </summary>
        Delete,

        /// <summary>
        /// If a device already exists with the specified Id, it is deleted only if there is an ETag match. If the device does not exist, an error is written to the log file.
        /// If there is an ETag mismatch, an error is written to the log file.
        /// </summary>
        DeleteIfMatchETag,

        /// <summary>
        /// If a twin already exists with the specified Id, existing information is overwritten with the provided input data
        /// without regard to the ETag value.
        /// </summary>
        UpdateTwin,

        /// <summary>
        /// If a twin already exists with the specified Id, existing information is overwritten with the provided input data only if there is an ETag match.
        /// The twin's ETag, is processed independently from the device's ETag. If there is a mismatch with the existing twin's ETag,
        /// an error is written to the log file.
        /// </summary>
        UpdateTwinIfMatchETag,
    }
}
