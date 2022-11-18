// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using static Microsoft.Azure.Devices.ClientTwinProperties;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Represents a property value in a <see cref="ClientTwinProperties"/>.
    /// </summary>
    public class ClientTwinPropertyValue : JValue
    {
        private readonly JObject _metadata;

        internal ClientTwinPropertyValue(JValue jValue, JObject metadata)
            : base(jValue)
        {
            _metadata = metadata;
        }

        /// <summary>
        /// Gets the value for the given property name.
        /// </summary>
        /// <param name="propertyName">Property name to look up.</param>
        /// <returns>Property value, if present.</returns>
        /// <exception cref="InvalidOperationException">When the specified <paramref name="propertyName"/> does not exist in the collection.</exception>
        public dynamic this[string propertyName]
        {
            get
            {
                return propertyName switch
                {
                    MetadataName => GetMetadata(),
                    LastUpdatedName => GetLastUpdatedOnUtc(),
                    LastUpdatedVersionName => GetLastUpdatedVersion(),
                    _ => throw new InvalidOperationException($"{nameof(ClientTwinPropertyValue)} does not contain a definition for '{propertyName}'."),
                };
            }
        }

        /// <summary>
        /// Gets the metadata for this property.
        /// </summary>
        /// <returns>Metadata instance representing the metadata for this property.</returns>
        public ClientTwinMetadata GetMetadata()
        {
            return new ClientTwinMetadata(GetLastUpdatedOnUtc(), GetLastUpdatedVersion());
        }

        /// <summary>
        /// Gets the time when this property was last updated in UTC.
        /// </summary>
        public DateTimeOffset GetLastUpdatedOnUtc()
        {
            if (_metadata != null
                && _metadata.TryGetValue(LastUpdatedName, out JToken lastUpdatedName)
                && (DateTimeOffset)lastUpdatedName is DateTimeOffset lastUpdatedOnUtc)
            {
                return lastUpdatedOnUtc;
            }

            return default;
        }

        /// <summary>
        /// Gets the last updated version for this property.
        /// </summary>
        /// <returns>Last updated version if present, null otherwise.</returns>
        public long? GetLastUpdatedVersion()
        {
            return (long?)_metadata?[LastUpdatedVersionName];
        }
    }
}
