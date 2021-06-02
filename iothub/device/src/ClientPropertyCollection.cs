// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A collection of properties for the client.
    /// </summary>
    public class ClientPropertyCollection : PayloadCollection
    {
        private const string VersionName = "$version";

        /// <inheritdoc path="/exception['ArgumentException']" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <summary>
        /// Adds the value to the collection.
        /// </summary>
        /// <remarks>
        /// If the collection has a key that matches the property name this method will throw an <see cref="ArgumentException"/>.
        /// <para>
        /// When using this as part of the writable property flow to respond to a writable property update you should pass in the value
        /// as an instance of <see cref="PayloadSerializer.CreateWritablePropertyResponse(object, int, long, string)"/>
        /// to ensure the correct formatting is applied when the object is serialized.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="rootKey"/> is <c>null</c> </exception>
        /// <param name="rootKey">The root key to add to the collection. This can either be a root-level property name or a component name.</param>
        /// <param name="propertyValue">The value of the property to add. This can either be a root-level property value or the complete property collection for a component.</param>
        public override void Add(string rootKey, object propertyValue)
        {
            if (IsDictionary(propertyValue))
            {
                string componentName = rootKey;
                var properties = (IDictionary<string, object>)propertyValue;
                AddInternal(properties, componentName, false);
            }
            else
            {
                Add(null, rootKey, propertyValue);
            }
        }

        /// <inheritdoc path="/remarks" cref="Add(string, object)" />
        /// <inheritdoc path="/seealso" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <inheritdoc path="/exception['ArgumentException']" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <summary>
        /// Adds the value to the collection.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <c>null</c> </exception>
        /// <param name="componentName">The component with the property to add.</param>
        /// <param name="propertyName">The name of the property to add.</param>
        /// <param name="propertyValue">The value of the property to add.</param>
        public void Add(string componentName, string propertyName, object propertyValue)
            => AddInternal(new Dictionary<string, object> { { propertyName, propertyValue } }, componentName, false);

        /// <inheritdoc path="/remarks" cref="Add(string, object)" />
        /// <inheritdoc path="/seealso" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <inheritdoc path="/exception['ArgumentException']" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <summary>
        /// Adds the values to the collection.
        /// </summary>
        /// <param name="properties">A collection of properties to add or update.</param>
        public void Add(IDictionary<string, object> properties)
            => AddInternal(properties, null, false);

        /// <inheritdoc path="/exception['ArgumentException']" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <inheritdoc path="/seealso" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <summary>
        /// Adds a writable property to the collection.
        /// </summary>
        /// <remarks>
        /// This method will use the <see cref="PayloadSerializer.CreateWritablePropertyResponse(object, int, long, string)"/> method to create an instance of <see cref="IWritablePropertyResponse"/> that will be properly serialized.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <c>null</c> </exception>
        /// <param name="propertyName">The name of the property to add or update.</param>
        /// <param name="propertyValue">The value of the property to add or update.</param>
        /// <param name="statusCode"></param>
        /// <param name="version"></param>
        /// <param name="description"></param>
        public void Add(string propertyName, object propertyValue, int statusCode, long version, string description = default)
            => Add(null, propertyName, propertyValue, statusCode, version, description);

        /// <inheritdoc path="/exception['ArgumentException']" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <inheritdoc path="/seealso" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <summary>
        /// Adds a writable property to the collection.
        /// </summary>
        /// <remarks>
        /// This method will use the <see cref="PayloadSerializer.CreateWritablePropertyResponse(object, int, long, string)"/> method to create an instance of <see cref="IWritablePropertyResponse"/> that will be properly serialized.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <c>null</c> </exception>
        /// <param name="propertyName">The name of the property to add or update.</param>
        /// <param name="propertyValue">The value of the property to add or update.</param>
        /// <param name="statusCode"></param>
        /// <param name="version"></param>
        /// <param name="description"></param>
        /// <param name="componentName"></param>
        public void Add(string componentName, string propertyName, object propertyValue, int statusCode, long version, string description = default)
        {
            if (Convention?.PayloadSerializer == null)
            {
                Add(componentName, propertyName, new { value = propertyValue, ac = statusCode, av = version, ad = description });
            }
            else
            {
                Add(componentName, propertyName, Convention.PayloadSerializer.CreateWritablePropertyResponse(propertyValue, statusCode, version, description));
            }
        }

        /// <inheritdoc path="/summary" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <inheritdoc path="/remarks" cref="AddOrUpdate(string, IDictionary{string, object})" />
        /// <inheritdoc path="/exception['ArgumentException']" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <inheritdoc path="/seealso" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <c>null</c> </exception>
        /// <param name="propertyName">The name of the property to add or update.</param>
        /// <param name="propertyValue">The value of the property to add or update.</param>
        public override void AddOrUpdate(string propertyName, object propertyValue)
            => AddOrUpdate(null, propertyName, propertyValue);

        /// <inheritdoc path="/summary" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <inheritdoc path="/remarks" cref="AddOrUpdate(string, IDictionary{string, object})" />
        /// <inheritdoc path="/exception['ArgumentException']" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <inheritdoc path="/seealso" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <c>null</c> </exception>
        /// <param name="componentName">The component with the property to add or update.</param>
        /// <param name="propertyName">The name of the property to add or update.</param>
        /// <param name="propertyValue">The value of the property to add or update.</param>
        public void AddOrUpdate(string componentName, string propertyName, object propertyValue)
            => AddInternal(new Dictionary<string, object> { { propertyName, propertyValue } }, componentName, true);

        /// <inheritdoc path="/summary" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <inheritdoc path="/exception['ArgumentException']" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <inheritdoc path="/seealso" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <remarks>
        /// If the collection has a key that matches this will overwrite the current value. Otherwise it will attempt to add this to the collection.
        /// <para>
        /// When using this as part of the writable property flow to respond to a writable property update
        /// you should pass in the value as an instance of <see cref="PayloadSerializer.CreateWritablePropertyResponse(object, int, long, string)"/>
        /// to ensure the correct formatting is applied when the object is serialized.
        /// </para>
        /// </remarks>
        /// <param name="properties">A collection of properties to add or update.</param>
        public void AddOrUpdate(IDictionary<string, object> properties)
            => AddInternal(properties, null, true);

        /// <inheritdoc path="/summary" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <inheritdoc path="/exception['ArgumentException']" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <inheritdoc path="/seealso" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <remarks>
        /// If the collection has a key that matches this will overwrite the current value. Otherwise it will attempt to add this to the collection.
        /// <para>
        /// When using this as part of the writable property flow to respond to a writable property update
        /// you should pass in the value as an instance of <see cref="PayloadSerializer.CreateWritablePropertyResponse(object, int, long, string)"/>
        /// to ensure the correct formatting is applied when the object is serialized.
        /// </para>
        /// </remarks>
        /// <param name="componentName">The component with the properties to add or update.</param>
        /// <param name="properties">A collection of properties to add or update.</param>
        public void AddOrUpdate(string componentName, IDictionary<string, object> properties)
            => AddInternal(properties, componentName, true);

        /// <inheritdoc path="/exception['ArgumentException']" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <inheritdoc path="/seealso" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <summary>
        /// Adds or updates a type of <see cref="IWritablePropertyResponse"/> to the collection.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <c>null</c> </exception>
        /// <param name="propertyName">The name of the writable property to add or update.</param>
        /// <param name="propertyValue">The value of the writable property to add or update.</param>
        /// <param name="statusCode"></param>
        /// <param name="version"></param>
        /// <param name="description"></param>
        public void AddOrUpdate(string propertyName, object propertyValue, int statusCode, long version, string description = default)
            => AddOrUpdate(null, propertyName, propertyValue, statusCode, version, description);

        /// <inheritdoc path="/remarks" cref="Add(string, object, int, long, string)"/>
        /// <inheritdoc path="/exception['ArgumentException']" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <inheritdoc path="/seealso" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <summary>
        /// Adds or updates a type of <see cref="IWritablePropertyResponse"/> to the collection.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <c>null</c> </exception>
        /// <param name="propertyName">The name of the writable property to add or update.</param>
        /// <param name="propertyValue">The value of the writable property to add or update.</param>
        /// <param name="statusCode"></param>
        /// <param name="version"></param>
        /// <param name="description"></param>
        /// <param name="componentName"></param>
        public void AddOrUpdate(string componentName, string propertyName, object propertyValue, int statusCode, long version, string description = default)
        {
            if (Convention?.PayloadSerializer == null)
            {
                AddOrUpdate(componentName, propertyName, new { value = propertyValue, ac = statusCode, av = version, ad = description });
            }
            else
            {
                AddOrUpdate(componentName, propertyName, Convention.PayloadSerializer.CreateWritablePropertyResponse(propertyValue, statusCode, version, description));
            }
        }

        /// <summary>
        /// Determines whether the specified property is present.
        /// </summary>
        /// <param name="propertyName">The property to locate.</param>
        /// <param name="componentName">The component which holds the required property.</param>
        /// <returns><c>true</c> if the specified property is present; otherwise, <c>false</c>.</returns>
        public bool Contains(string componentName, string propertyName)
        {
            if (!string.IsNullOrEmpty(componentName) && Collection.TryGetValue(componentName, out var component))
            {
                if (component is IDictionary<string, object> nestedDictionary)
                {
                    return nestedDictionary.TryGetValue(propertyName, out var _);
                }
                return Convention.PayloadSerializer.TryGetNestedObjectValue<object>(component, propertyName, out _);
            }
            return Collection.TryGetValue(propertyName, out _);
        }

        /// <summary>
        /// Gets the version of the property collection.
        /// </summary>
        /// <value>A <see cref="long"/> that is used to identify the version of the property collection.</value>
        public long Version { get; protected set; }

        /// <summary>
        /// Gets the value of a component-level property.
        /// </summary>
        /// <remarks>
        /// To get the value of a root-level property use <see cref="PayloadCollection.TryGetValue{T}(string, out T)"/>.
        /// </remarks>
        /// <typeparam name="T">The type to cast the object to.</typeparam>
        /// <param name="componentName">The component which holds the required property.</param>
        /// <param name="propertyName">The property to get.</param>
        /// <param name="propertyValue">The value of the component-level property.</param>
        /// <returns>true if the property collection contains a component level property with the specified key; otherwise, false.</returns>
        public virtual bool TryGetValue<T>(string componentName, string propertyName, out T propertyValue)
        {
            if (Logging.IsEnabled && Convention == null)
            {
                Logging.Info(this, $"The convention for this collection is not set; this typically means this collection was not created by the client. " +
                    $"TryGetValue will attempt to get the property value but may not behave as expected.", nameof(TryGetValue));
            }

            if (Contains(componentName, propertyName))
            {
                object componentProperties = Collection[componentName];

                if (componentProperties is IDictionary<string, object> nestedDictionary)
                {
                    if (nestedDictionary.TryGetValue(propertyName, out object dictionaryElement))
                    {
                        // If the value is null, go ahead and return it.
                        if (dictionaryElement == null)
                        {
                            propertyValue = default;
                            return true;
                        }

                        // If the object is of type T or can be cast to type T, go ahead and return it.
                        if (dictionaryElement is T valueRef
                            || NumericHelpers.TryCastNumericTo(dictionaryElement, out valueRef))
                        {
                            propertyValue = valueRef;
                            return true;
                        }
                    }
                }
                else
                {
                    // If it's not, we need to try to convert it using the serializer.
                    Convention.PayloadSerializer.TryGetNestedObjectValue<T>(componentProperties, propertyName, out propertyValue);
                    return true;
                }
            }

            propertyValue = default;
            return false;
        }

        /// <summary>
        /// Converts a <see cref="TwinCollection"/> collection to a properties collection.
        /// </summary>
        /// <remarks>This internal class is aware of the implementation of the TwinCollection ad will </remarks>
        /// <param name="twinCollection">The TwinCollection object to convert.</param>
        /// <param name="payloadConvention">A convention handler that defines the content encoding and serializer to use for the payload.</param>
        /// <returns>A new instance of the class from an existing <see cref="TwinProperties"/> using an optional <see cref="PayloadConvention"/>.</returns>
        internal static ClientPropertyCollection FromTwinCollection(TwinCollection twinCollection, PayloadConvention payloadConvention)
        {
            if (twinCollection == null)
            {
                throw new ArgumentNullException(nameof(twinCollection));
            }

            var propertyCollectionToReturn = new ClientPropertyCollection
            {
                Convention = payloadConvention,
            };

            foreach (KeyValuePair<string, object> property in twinCollection)
            {
                propertyCollectionToReturn.Add(property.Key, payloadConvention.PayloadSerializer.DeserializeToType<object>(Newtonsoft.Json.JsonConvert.SerializeObject(property.Value)));
            }
            // The version information is not accessible via the enumerator, so assign it separately.
            propertyCollectionToReturn.Version = twinCollection.Version;

            return propertyCollectionToReturn;
        }

        internal static ClientPropertyCollection FromClientTwinDictionary(IDictionary<string, object> clientTwinPropertyDictionary, PayloadConvention payloadConvention)
        {
            if (clientTwinPropertyDictionary == null)
            {
                throw new ArgumentNullException(nameof(clientTwinPropertyDictionary));
            }

            var propertyCollectionToReturn = new ClientPropertyCollection
            {
                Convention = payloadConvention,
            };

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

        /// <summary>
        /// Adds or updates the value for the collection.
        /// </summary>
        /// <seealso cref="PayloadConvention"/>
        /// <seealso cref="PayloadSerializer"/>
        /// <seealso cref="PayloadEncoder"/>
        /// <param name="properties">A collection of properties to add or update.</param>
        /// <param name="componentName">The component with the properties to add or update.</param>
        /// <param name="forceUpdate">Forces the collection to use the Add or Update behavior.
        /// Setting to true will simply overwrite the value. Setting to false will use <see cref="IDictionary{TKey, TValue}.Add(TKey, TValue)"/></param>
        private void AddInternal(IDictionary<string, object> properties, string componentName = default, bool forceUpdate = false)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            // If the componentName is null then simply add the key-value pair to Collection dictionary.
            // This will either insert a property or overwrite it if it already exists.
            if (componentName == null)
            {
                foreach (KeyValuePair<string, object> entry in properties)
                {
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
                // Append this property dictionary to the existing property value dictionary (overwrite entries if they already exist, if forceUpdate is true).
                // Otherwise, if the component name does not exist in the dictionary, then add this as a new entry.
                var componentProperties = new Dictionary<string, object>();
                if (Collection.ContainsKey(componentName))
                {
                    componentProperties = (Dictionary<string, object>)Collection[componentName];
                }
                foreach (KeyValuePair<string, object> entry in properties)
                {
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

                Collection[componentName] = componentProperties;
            }
        }

        private static bool IsDictionary(object objectToCompare)
        {
            if (objectToCompare == null)
            {
                return false;
            }

            return objectToCompare is IDictionary
                && objectToCompare.GetType().IsGenericType
                && objectToCompare.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>));
        }
    }
}
