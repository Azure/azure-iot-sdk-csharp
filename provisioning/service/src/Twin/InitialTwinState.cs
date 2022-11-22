// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single twin initial state.
    /// </summary>
    public sealed class InitialTwinState
    {
        /// <summary>
        /// Getter and setter the desired properties.
        /// </summary>
        [JsonPropertyName("properties")]
        public InitialTwinProperties Properties { get; set; } = new();

        /// <summary>
        /// Gets and sets the twin tags.
        /// </summary>
        [JsonPropertyName("tags")]
        public IDictionary<string, object> Tags { get; set; } = new Dictionary<string, object>();
    }
}
