// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Metadata for properties in <see cref="ClientTwinProperties"/>.
    /// </summary>
    public sealed class ClientTwinMetadata
    {
        /// <summary>
        /// When a property was last updated.
        /// </summary>
        [JsonProperty("$lastUpdated", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTimeOffset? LastUpdatedOnUtc { get; set; }

        /// <summary>
        /// The version of the property when last updated.
        /// </summary>
        /// <remarks>
        /// This should be not be included for reported properties metadata and must not be null for desired properties metadata.
        /// </remarks>
        [JsonProperty("$lastUpdatedVersion", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long? LastUpdatedVersion { get; set; }

        [JsonExtensionData]
        internal IDictionary<string, JToken> Properties { get; set; } = new Dictionary<string, JToken>();

        /// <summary>
        /// Gets the specified property's metadata by name as another metadata object.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="propertyValue">The property's metadata.</param>
        /// <returns>True if the property exists and could be converted, otherwise false.</returns>
        public bool TryGetPropertyMetadata(string propertyName, out ClientTwinMetadata propertyValue)
        {
            propertyValue = default;

            if (!Properties.TryGetValue(propertyName, out JToken jTokenValue))
            {
                return false;
            }

            // Try convert.
            try
            {
                propertyValue = jTokenValue.ToObject<ClientTwinMetadata>();
                return true;
            }
            catch (InvalidCastException)
            { }

            return false;
        }
    }
}
