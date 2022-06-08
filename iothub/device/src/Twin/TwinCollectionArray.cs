// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Represents a property array in a <see cref="TwinCollection"/>
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
        /// Gets the value for the given property name
        /// </summary>
        /// <param name="propertyName">Property Name to lookup</param>
        /// <returns>Property value, if present</returns>
        public dynamic this[string propertyName]
        {
            get
            {
                return propertyName switch
                {
                    TwinCollection.MetadataName => GetMetadata(),
                    TwinCollection.LastUpdatedName => GetLastUpdated(),
                    TwinCollection.LastUpdatedVersionName => GetLastUpdatedVersion(),
                    _ => throw new ArgumentException($"{nameof(TwinCollectionArray)} does not contain a definition for '{propertyName}'."),
                };
            }
        }

        /// <summary>
        /// Gets the Metadata for this property
        /// </summary>
        /// <returns>Metadata instance representing the metadata for this property</returns>
        public Metadata GetMetadata()
        {
            return new Metadata(GetLastUpdated(), GetLastUpdatedVersion());
        }

        /// <summary>
        /// Gets the LastUpdated time for this property
        /// </summary>
        /// <returns>DateTime instance representing the LastUpdated time for this property</returns>
        public DateTime GetLastUpdated()
        {
            return (DateTime)_metadata[TwinCollection.LastUpdatedName];
        }

        /// <summary>
        /// Gets the LastUpdatedVersion for this property
        /// </summary>
        /// <returns>LastUpdatdVersion if present, null otherwise</returns>
        public long? GetLastUpdatedVersion()
        {
            return (long?)_metadata[TwinCollection.LastUpdatedVersionName];
        }
    }
}
