// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    ///
    /// </summary>
    public class PropertyCollection : IEnumerable<object>
    {
        private const string VersionName = "$version";

        private readonly IDictionary<string, object> _properties = new Dictionary<string, object>();
        private readonly PropertyConvention _propertyConvention;

        internal PropertyCollection()
        {
        }

        internal PropertyCollection(IDictionary<string, object> properties, PropertyConvention propertyConvention)
        {
            _properties = properties;
            _propertyConvention = propertyConvention;
        }

        /// <summary>
        /// Determines whether the specified property is present
        /// </summary>
        /// <param name="propertyName">The property to locate</param>
        /// <returns>true if the specified property is present; otherwise, false</returns>
        public bool Contains(string propertyName)
        {
            return _properties.TryGetValue(propertyName, out _);
        }

        /// <summary>
        ///
        /// </summary>
        public long Version => _properties.TryGetValue(VersionName, out object version)
            ? (long)version
            : default;

        /// <summary>
        ///
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public dynamic this[string propertyName]
        {
            get
            {
                return _properties[propertyName];
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            return _propertyConvention?.SerializeToString(_properties);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public IEnumerator<object> GetEnumerator()
        {
            foreach (object property in _properties)
            {
                yield return property;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal void AddPropertyToCollection(string propertyKey, object propertyValue)
        {
            _properties.Add(propertyKey, propertyValue);
        }

        /// <summary>
        /// Converts a <see cref="TwinCollection"/> collection to a properties collection
        /// </summary>
        /// <param name="twinCollection">The TwinCollection object to convert</param>
        /// <returns></returns>
        internal static PropertyCollection FromTwinCollection(TwinCollection twinCollection)
        {
            if (twinCollection == null)
            {
                throw new ArgumentNullException(nameof(twinCollection));
            }

            var writablePropertyCollection = new PropertyCollection();
            foreach (KeyValuePair<string, object> property in twinCollection)
            {
                writablePropertyCollection.AddPropertyToCollection(property.Key, property.Value);
            }
            // The version information is not accessible via the enumerator, so assign it separately.
            writablePropertyCollection.AddPropertyToCollection(VersionName, twinCollection.Version);

            return writablePropertyCollection;
        }
    }
}
