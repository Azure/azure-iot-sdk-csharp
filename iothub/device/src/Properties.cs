// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A container for properties.
    /// </summary>
    public class Properties : IEnumerable<object>
    {
        private const string VersionName = "$version";
        private readonly IDictionary<string, object> _readOnlyProperties = new Dictionary<string, object>();

        /// <summary>
        /// Initializes a new instance of <see cref="Properties"/>
        /// </summary>
        internal Properties()
        {
            Writable = new PropertyCollection();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Properties"/> with the specified collections
        /// </summary>
        /// <param name="writablePropertyCollection">A collection of writable properties returned from IoT Hub</param>
        /// <param name="readOnlyPropertyCollection">A collection of read-only properties returned from IoT Hub</param>
        internal Properties(PropertyCollection writablePropertyCollection, IDictionary<string, object> readOnlyPropertyCollection)
        {
            Writable = writablePropertyCollection;
            _readOnlyProperties = readOnlyPropertyCollection;
        }

        /// <summary>
        ///
        /// </summary>
        public PropertyCollection Writable { get; private set; }

        /// <summary>
        /// Get the property from the propeties collection
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public dynamic this[string propertyName]
        {
            get
            {
                return _readOnlyProperties[propertyName];
            }
        }

        /// <summary>
        /// Determines whether the specified property is present
        /// </summary>
        /// <param name="propertyName">The property to locate</param>
        /// <returns>true if the specified property is present; otherwise, false</returns>
        public bool Contains(string propertyName)
        {
            return _readOnlyProperties.TryGetValue(propertyName, out _);
        }

        /// <summary>
        ///
        /// </summary>
        public long Version => _readOnlyProperties.TryGetValue(VersionName, out object version)
            ? (long)version
            : default;

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public IEnumerator<object> GetEnumerator()
        {
            foreach (object property in _readOnlyProperties)
            {
                yield return property;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Converts a <see cref="TwinProperties"/> collection to a properties collection
        /// </summary>
        /// <param name="twinProperties">The TwinProperties object to convert</param>
        /// <returns></returns>
        internal static Properties FromTwinProperties(TwinProperties twinProperties)
        {
            if (twinProperties == null)
            {
                throw new ArgumentNullException(nameof(twinProperties));
            }

            var writablePropertyCollection = new PropertyCollection();
            foreach (KeyValuePair<string, object> property in twinProperties.Desired)
            {
                writablePropertyCollection.Add(property.Key, property.Value);
            }
            // The version information is not accessible via the enumerator, so assign it separately.
            writablePropertyCollection.Add(VersionName, twinProperties.Desired.Version);

            var propertyCollection = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> property in twinProperties.Reported)
            {
                propertyCollection.Add(property.Key, property.Value);
            }
            // The version information is not accessible via the enumerator, so assign it separately.
            propertyCollection.Add(VersionName, twinProperties.Reported.Version);

            return new Properties(writablePropertyCollection, propertyCollection);
        }
    }
}
