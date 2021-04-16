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
    public class PropertyCollection : PayloadCollection, IEnumerable<object>
    {
        private const string VersionName = "$version";

        /// <summary>
        ///
        /// </summary>
        /// <param name="payloadConvention"></param>
        public PropertyCollection(IPayloadConvention payloadConvention = default)
            : base(payloadConvention)
        {
        }

        /// <summary>
        /// highlight both "readonly" and "writable property response" propertyValue patches
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        /// <param name="componentName"></param>
        public void Add(string propertyName, object propertyValue, string componentName = default)
            => Add(new Dictionary<string, object> { { propertyName, propertyValue } }, componentName, false);

        /// <summary>
        ///
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="componentName"></param>
        public void Add(IDictionary<string, object> properties, string componentName = default)
        => Add(properties, componentName, false);

        /// <summary>
        ///
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="componentName"></param>
        public void AddOrUpdate(IDictionary<string, object> properties, string componentName = default)
            => Add(properties, componentName, true);

        /// <summary>
        /// highlight both "readonly" and "writable property response" propertyValue patches
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        /// <param name="componentName"></param>
        public void AddOrUpdate(string propertyName, object propertyValue, string componentName = default)
            => Add(new Dictionary<string, object> { { propertyName, propertyValue } }, componentName, true);

        private void Add(IDictionary<string, object> properties, string componentName = default, bool forceUpdate = false)
        {

            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            // If the componentName is null then simply add the key-value pair to Collection dictionary.
            // this will either insert a property or overwrite it if it already exists.
            if (componentName == null)
            {
                foreach (KeyValuePair<string, object> entry in properties)
                {
                    var checkType = entry.Value is WritablePropertyBase;
                    if (entry.Value is WritablePropertyBase && !Convention.PayloadSerializer.CheckType(entry.Value))
                    {
                        throw new ArgumentException("Please use the proper class extended from WritablePropertyBase to match your payload convention.");
                    }
                    if (forceUpdate)
                    {
                        Collection[entry.Key] = entry.Value;
                    }
                    else
                    {
                        Collection.Add(entry.Key, entry.Value);
                    }

                }
            }
            else
            {
                // If the component name already exists within the dictionary, then the value is a dictionary containing the component level property key and values.
                // Append this property dictionary to the existing property value dictionary (overwrite entries if they already exist).
                // Otherwise, add this as a new entry.
                var componentProperties = new Dictionary<string, object>();
                if (Collection.ContainsKey(componentName))
                {
                    componentProperties = (Dictionary<string, object>)Collection[componentName];
                }
                foreach (KeyValuePair<string, object> entry in properties)
                {
                    var checkType = entry.Value is WritablePropertyBase;
                    if (entry.Value is WritablePropertyBase && !Convention.PayloadSerializer.CheckType(entry.Value))
                    {
                        throw new ArgumentException("Please use the proper class extended from WritablePropertyBase to match your payload convention.");
                    }

                    if (forceUpdate)
                    {
                        componentProperties[entry.Key] = entry.Value;
                    }
                    else
                    {
                        componentProperties.Add(entry.Key, entry.Value);
                    }

                }

                // For a component level property, the property patch needs to contain the {"__t": "c"} component identifier.
                if (!componentProperties.ContainsKey(PropertyConvention.ComponentIdentifierKey))
                {
                    componentProperties[PropertyConvention.ComponentIdentifierKey] = PropertyConvention.ComponentIdentifierValue;
                }

                if (forceUpdate)
                {
                    Collection[componentName] = componentProperties;
                }
                else
                {
                    Collection.Add(componentName, componentProperties);
                }
            }
        }

        /// <summary>
        /// Determines whether the specified property is present
        /// </summary>
        /// <param name="propertyName">The property to locate</param>
        /// <returns>true if the specified property is present; otherwise, false</returns>
        public bool Contains(string propertyName)
        {
            return Collection.TryGetValue(propertyName, out _);
        }

        /// <summary>
        ///
        /// </summary>
        public long Version => Collection.TryGetValue(VersionName, out object version)
            ? (long)version
            : default;

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            return Convention?.PayloadSerializer?.SerializeToString(Collection);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public IEnumerator<object> GetEnumerator()
        {
            foreach (object property in Collection)
            {
                yield return property;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
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
                writablePropertyCollection.Add(property.Key, property.Value);
            }
            // The version information is not accessible via the enumerator, so assign it separately.
            writablePropertyCollection.Add(VersionName, twinCollection.Version);

            return writablePropertyCollection;
        }
    }
}
