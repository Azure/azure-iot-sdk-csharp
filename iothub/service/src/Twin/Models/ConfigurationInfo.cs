// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Details of the configuration.
    /// </summary>
    public sealed class ConfigurationInfo
    {
        /// <summary>
        /// Configuration status.
        /// </summary>
        [JsonPropertyName("status")]
        public ConfigurationStatus Status { get; set; }
    }
}
