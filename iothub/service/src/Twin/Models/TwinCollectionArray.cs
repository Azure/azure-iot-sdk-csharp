// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Represents a property array in a <see cref="TwinCollection"/>.
    /// </summary>
    public class TwinCollectionArray : JArray
    {
        private readonly JObject _metadata;

        internal TwinCollectionArray(JArray jArray, JObject metadata)
            : base(jArray)
        {
            _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }

        /// <summary>
        /// Gets the value for the given property name.
        /// </summary>
        /// <param name="propertyName">Property name to look up.</param>
        /// <returns>Property value, if present.</returns>
        public dynamic this[string propertyName]
        {
            get
            {
                return propertyName switch
                {
                    TwinCollection.MetadataName => GetMetadata(),
                    TwinCollection.LastUpdatedName => GetLastUpdatedOnUtc(),
                    TwinCollection.LastUpdatedVersionName => GetLastUpdatedVersion(),
                    _ => throw new ArgumentException($"{nameof(TwinCollectionArray)} does not contain a definition for '{propertyName}'."),
                };
            }
        }

        /// <summary>
        /// Gets the metadata for this property.
        /// </summary>
        /// <returns>Metadata instance representing the metadata for this property.</returns>
        public TwinMetadata GetMetadata()
        {
            return new TwinMetadata(GetLastUpdatedOnUtc(), GetLastUpdatedVersion());
        }

        /// <summary>
        /// Gets the last updated time for this property.
        /// </summary>
        /// <returns>Date-time instance representing the last updated time for this property.</returns>
        public DateTimeOffset GetLastUpdatedOnUtc()
        {
            return (DateTimeOffset)_metadata[TwinCollection.LastUpdatedName];
        }

        /// <summary>
        /// Gets the last updated version for this property.
        /// </summary>
        /// <returns>Last updated version if present, null otherwise.</returns>
        public long? GetLastUpdatedVersion()
        {
            return (long?)_metadata[TwinCollection.LastUpdatedVersionName];
        }
    }
}
