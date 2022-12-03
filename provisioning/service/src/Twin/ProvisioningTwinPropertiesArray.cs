﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json.Linq;
using static Microsoft.Azure.Devices.Provisioning.Service.InitialTwinPropertyCollection;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Represents a property array in a <see cref="InitialTwinPropertyCollection"/>.
    /// </summary>
    public class ProvisioningTwinPropertiesArray : JArray
    {
        private readonly JObject _metadata;

        internal ProvisioningTwinPropertiesArray(JArray jArray, JObject metadata)
            : base(jArray)
        {
            _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }

        /// <summary>
        /// Gets the value for the given property name.
        /// </summary>
        /// <param name="propertyName">Property Name to lookup.</param>
        /// <returns>Property value, if present</returns>
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
                    _ => throw new InvalidOperationException($"{nameof(ProvisioningTwinPropertiesArray)} does not contain a definition for '{propertyName}'."),
                };
            }
        }

        /// <summary>
        /// Gets the metadata for this property.
        /// </summary>
        /// <returns>Metadata instance representing the metadata for this property.</returns>
        public ProvisioningTwinMetadata GetMetadata()
        {
            return new ProvisioningTwinMetadata(GetLastUpdatedOnUtc(), GetLastUpdatedVersion());
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
