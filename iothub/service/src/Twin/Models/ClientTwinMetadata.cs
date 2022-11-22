// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Metadata of properties in <see cref="ClientTwinPropertyCollection"/>.
    /// </summary>
    public sealed class ClientTwinMetadata
    {
        /// <summary>
        /// When a property was last updated.
        /// </summary>
        [JsonPropertyName("$lastUpdated")]
        public DateTimeOffset LastUpdatedOnUtc { get; set; }

        /// <summary>
        /// The version of the property when last updated.
        /// </summary>
        /// <remarks>
        /// This should be null for reported properties metadata and must not be null for desired properties metadata.
        /// </remarks>
        [JsonPropertyName("$lastUpdatedVersion")]

        public long? LastUpdatedVersion { get; set; }

        /// <summary>
        /// Metadata about each property.
        /// </summary>
        public IDictionary<string, ClientTwinMetadata> Metadata { get; } = new Dictionary<string, ClientTwinMetadata>();

        [JsonExtensionData]
        internal IDictionary<string, JsonElement> SerializedMetadata
        {
            //get => new Dictionary<string, JsonElement>(0);
            set
            {
                foreach (KeyValuePair<string, JsonElement> kvp in value)
                {
                    Metadata.Add(kvp.Key, JsonSerializer.Deserialize<ClientTwinMetadata>(kvp.Value.GetRawText()));
                }
            }
        }
    }
}
