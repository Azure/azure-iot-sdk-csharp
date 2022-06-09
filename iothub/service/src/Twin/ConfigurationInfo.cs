// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Details of the Configuration
    /// </summary>
    public class ConfigurationInfo
    {
        /// <summary>
        /// Configuration status.
        /// </summary>
        [JsonProperty("status")]
        public ConfigurationStatus Status { get; set; }
    }
}
