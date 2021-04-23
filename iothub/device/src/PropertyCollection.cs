// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A collection of properties for the device.
    /// </summary>
    public class PropertyCollection : PayloadCollection
    {
        private const string VersionName = "$version";

        /// <inheritdoc/>
        public PropertyCollection(PayloadConvention payloadConvention = default)
            : base(payloadConvention)
        {
        }

        /// <summary>
        /// Adds the value for the collection.
        /// </summary>
        /// <inheritdoc path="/remarks" cref="Add(IDictionary{string, object}, string, bool)" />
        /// <inheritdoc path="/exception['ArgumentException']" cref="Add(IDictionary{string, object}, string, bool)" />
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <c>null</c> </exception>
        /// <param name="propertyName">The name of the property to add.</param>
        /// <param name="propertyValue">The value of the property to add.</param>
        /// <param name="componentName">The component with the property to add.</param>
        public void Add(string propertyName, object propertyValue, string componentName = default)
            => Add(new Dictionary<string, object> { { propertyName, propertyValue } }, componentName, false);

        /// <inheritdoc path="/remarks" cref="Add(IDictionary{string, object}, string, bool)"/>
        /// <inheritdoc path="/exception['ArgumentException']" cref="Add(IDictionary{string, object}, string, bool)" />
        /// <summary>
        /// Adds the value for the collection.
        /// </summary>
        /// <param name="properties">A collection of properties to add or update.</param>
        /// <param name="componentName">The component with the properties to add or update.</param>
        public void Add(IDictionary<string, object> properties, string componentName = default)
        => Add(properties, componentName, false);

        /// <inheritdoc path="/summary" cref="Add(IDictionary{string, object}, string, bool)" />
        /// <inheritdoc path="/remarks" cref="Add(IDictionary{string, object}, string, bool)" />
        /// <inheritdoc path="/exception['ArgumentException']" cref="Add(IDictionary{string, object}, string, bool)" />
        /// <param name="properties">A collection of properties to add or update.</param>
        /// <param name="componentName">The component with the properties to add or update.</param>
        public void AddOrUpdate(IDictionary<string, object> properties, string componentName = default)
            => Add(properties, componentName, true);

        /// <inheritdoc path="/summary" cref="Add(IDictionary{string, object}, string, bool)" />
        /// <inheritdoc path="/remarks" cref="Add(IDictionary{string, object}, string, bool)" />
        /// <inheritdoc path="/exception['ArgumentException']" cref="Add(IDictionary{string, object}, string, bool)" />
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <c>null</c> </exception>
        /// <param name="propertyName">The name of the property to add or update.</param>
        /// <param name="propertyValue">The value of the property to add or update.</param>
        /// <param name="componentName">The component with the property to add or update.</param>
        public void AddOrUpdate(string propertyName, object propertyValue, string componentName = default)
            => Add(new Dictionary<string, object> { { propertyName, propertyValue } }, componentName, true);

        /// <summary>
        /// Adds or updates the value for the collection.
        /// </summary>
        /// <remarks>
        /// When adding a type of <see cref="IWritablePropertyResponse"/> the <see cref="Serializer.CheckWritablePropertyResponseType(object)"/>
        /// checks to make sure it can serialize this type correctly.
        /// This will only be evaluated at runtime so it will throw an exception if the type does not pass the check.
        /// </remarks>
        /// <exception cref="ArgumentException">This is thrown when the object is of <see cref="IWritablePropertyResponse"/>
        /// and does not pass the check from <see cref="Serializer.CheckWritablePropertyResponseType(object)"/> method.</exception>
        /// <param name="properties">A collection of properties to add or update.</param>
        /// <param name="componentName">The component with the properties to add or update.</param>
        /// <param name="forceUpdate">Forces the collection to use the Add or Update behavior. Setting to true will simply overwrite the value; setting to false will use <see cref="IDictionary{TKey, TValue}.Add(TKey, TValue)"/></param>
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
                    if (entry.Value is IWritablePropertyResponse && !Convention.PayloadSerializer.CheckWritablePropertyResponseType(entry.Value))
                    {
                        throw new ArgumentException("Please use the proper class implemented from IWritablePropertyResponse to match your payload convention.");
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
                    if (entry.Value is IWritablePropertyResponse && !Convention.PayloadSerializer.CheckWritablePropertyResponseType(entry.Value))
                    {
                        throw new ArgumentException("Please use the proper class implemented from IWritablePropertyResponse to match your payload convention.");
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
                if (!componentProperties.ContainsKey(ConventionBasedConstants.ComponentIdentifierKey))
                {
                    componentProperties[ConventionBasedConstants.ComponentIdentifierKey] = ConventionBasedConstants.ComponentIdentifierValue;
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
        /// Determines whether the specified property is present.
        /// </summary>
        /// <param name="propertyName">The property to locate.</param>
        /// <returns><c>true</c> if the specified property is present; otherwise, <c>false</c>.</returns>
        public bool Contains(string propertyName)
        {
            return Collection.TryGetValue(propertyName, out _);
        }

        /// <summary>
        /// Gets the version of the property collection.
        /// </summary>
        /// <value>A <see cref="long"/> that is used to identify the version of the property collection.</value>
        public long Version { get; private set; }

        /// <summary>
        /// Converts a <see cref="TwinCollection"/> collection to a properties collection.
        /// </summary>
        /// <param name="twinCollection">The TwinCollection object to convert.</param>
        /// <param name="payloadConvention">A convention handler that defines the content encoding and serializer to use for the payload.</param>
        /// <returns>A new instance of of the class from an existing <see cref="TwinProperties"/> using an optional <see cref="PayloadConvention"/>.</returns>
        /// <remarks>This internala class is aware of the implemention of the TwinCollection ad will </remarks>
        internal static PropertyCollection FromTwinCollection(TwinCollection twinCollection, PayloadConvention payloadConvention = default)
        {
            if (twinCollection == null)
            {
                throw new ArgumentNullException(nameof(twinCollection));
            }

            payloadConvention ??= DefaultPayloadConvention.Instance;

            var propertyCollectionToReturn = new PropertyCollection(payloadConvention);
            foreach (KeyValuePair<string, object> property in twinCollection)
            {
                propertyCollectionToReturn.Add(property.Key, payloadConvention.PayloadSerializer.DeserializeToType<object>(Newtonsoft.Json.JsonConvert.SerializeObject(property.Value)));
            }
            // The version information is not accessible via the enumerator, so assign it separately.
            propertyCollectionToReturn.Version = twinCollection.Version;

            return propertyCollectionToReturn;
        }

        internal static PropertyCollection FromClientTwinDictionary(IDictionary<string, object> clientTwinPropertyDictionary, PayloadConvention payloadConvention)
        {
            if (clientTwinPropertyDictionary == null)
            {
                throw new ArgumentNullException(nameof(clientTwinPropertyDictionary));
            }

            payloadConvention ??= DefaultPayloadConvention.Instance;

            var propertyCollectionToReturn = new PropertyCollection(payloadConvention);
            foreach (KeyValuePair<string, object> property in clientTwinPropertyDictionary)
            {
                // The version information should not be a part of the enumerable ProperyCollection, but rather should be
                // accessible through its dedicated accessor.
                if (property.Key == VersionName)
                {
                    propertyCollectionToReturn.Version = (long)property.Value;
                }
                else
                {
                    propertyCollectionToReturn.Add(property.Key, payloadConvention.PayloadSerializer.DeserializeToType<object>(Newtonsoft.Json.JsonConvert.SerializeObject(property.Value)));
                }
            }

            return propertyCollectionToReturn;
        }
    }
}
