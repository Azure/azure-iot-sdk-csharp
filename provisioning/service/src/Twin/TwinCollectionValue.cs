// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json.Linq;
using static Microsoft.Azure.Devices.Provisioning.Service.TwinCollection;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Represents a property value in a <see cref="TwinCollection"/>.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Justification = "Public API cannot change name.")]
    [SuppressMessage("Microsoft.Design", "CA1036:OverrideMethodsOnComparableTypes",
        Justification = "Uses default JValue comparison, equality and hashing implementations.")]
    public class TwinCollectionValue : JValue
    {
        private readonly JObject _metadata;

        internal TwinCollectionValue(JValue jValue, JObject metadata)
            : base(jValue)
        {
            _metadata = metadata;
        }

        /// <summary>
        /// Gets the value for the given property name.
        /// </summary>
        /// <param name="propertyName">Property Name to lookup.</param>
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
                    _ => throw new InvalidOperationException($"{nameof(TwinCollectionValue)} does not contain a definition for '{propertyName}'."),
                };
            }
        }

        /// <summary>
        /// Gets the Metadata for this property.
        /// </summary>
        /// <returns>Metadata instance representing the metadata for this property.</returns>
        public TwinMetadata GetMetadata()
        {
            return new TwinMetadata(GetLastUpdatedOnUtc(), GetLastUpdatedVersion());
        }

        /// <summary>
        /// Gets the time when this property was last updated.
        /// </summary>
        public DateTimeOffset GetLastUpdatedOnUtc()
        {
            if (_metadata != null
                && _metadata.TryGetValue(LastUpdatedName, out JToken lastUpdatedName)
                && (DateTimeOffset)lastUpdatedName is DateTimeOffset lastUpdatedOnUtc)
            {
                return lastUpdatedOnUtc;
            }

            return DateTimeOffset.MinValue;
        }

        /// <summary>
        /// Gets the LastUpdatedVersion for this property.
        /// </summary>
        /// <returns>LastUpdatdVersion if present, null otherwise.</returns>
        public long? GetLastUpdatedVersion()
        {
            return (long?)_metadata?[LastUpdatedVersionName];
        }
    }
}
