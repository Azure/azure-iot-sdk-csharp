// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Identifies the behavior when merging a configuration to the registry during import actions.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ConfigurationImportMode
    {
        /// <summary>
        /// If a configuration does not exist with the specified Id, it is newly registered.
        /// If the configuration already exists, existing information is overwritten with the provided input data only if there is an ETag match.
        /// If there is an ETag mismatch, an error is written to the log file.
        /// </summary>
        [EnumMember(Value = "createOrUpdateIfMatchETag")]
        CreateOrUpdateIfMatchETag,
    }
}
