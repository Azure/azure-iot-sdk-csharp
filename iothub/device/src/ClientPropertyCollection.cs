// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A collection of properties for the client.
    /// </summary>
    public class ClientPropertyCollection : PayloadCollection
    {
        private const string VersionName = "$version";

        /// <summary>
        /// Adds the value to the collection.
        /// </summary>
        /// <remarks>
        /// If the collection already has a key matching a property name supplied this method will throw an <see cref="ArgumentException"/>.
        /// <para>
        /// When using this as part of the writable property flow to respond to a writable property update you should pass in the value
        /// as an instance of <see cref="PayloadSerializer.CreateWritablePropertyResponse(object, int, long, string)"/>
        /// to ensure the correct formatting is applied when the object is serialized.
        /// </para>
        /// </remarks>
        /// <param name="propertyName">The name of the property to add.</param>
        /// <param name="propertyValue">The value of the property to add.</param>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="propertyName"/> already exists in the collection.</exception>
        public void AddRootProperty(string propertyName, object propertyValue)
            => AddInternal(new Dictionary<string, object> { { propertyName, propertyValue } }, null, false);

        /// <inheritdoc path="/remarks" cref="AddRootProperty(string, object)" />
        /// <inheritdoc path="/seealso" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <inheritdoc path="/exception" cref="AddRootProperty(string, object)" />
        /// <summary>
        /// Adds the value to the collection.
        /// </summary>
        /// <param name="componentName">The component with the property to add.</param>
        /// <param name="propertyName">The name of the property to add.</param>
        /// <param name="propertyValue">The value of the property to add.</param>
        public void AddComponentProperty(string componentName, string propertyName, object propertyValue)
            => AddInternal(new Dictionary<string, object> { { propertyName, propertyValue } }, componentName, false);

        /// <inheritdoc path="/remarks" cref="AddRootProperty(string, object)" />
        /// <inheritdoc path="/seealso" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <summary>
        /// Adds the value to the collection.
        /// </summary>
        /// <param name="componentName">The component with the properties to add.</param>
        /// <param name="properties">A collection of properties to add.</param>
        /// <exception cref="ArgumentException">A property name in <paramref name="properties"/> already exists in the collection.</exception>
        public void AddComponentProperties(string componentName, IDictionary<string, object> properties)
            => AddInternal(properties, componentName, true);

        /// <inheritdoc path="/seealso" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <inheritdoc path="/exception" cref="AddRootProperty(string, object)" />
        /// <summary>
        /// Adds the collection of root-level property values to the collection.
        /// </summary>
        /// <remarks>
        /// If the collection already has a key matching a property name supplied this method will throw an <see cref="ArgumentException"/>.
        /// <para>
        /// When using this as part of the writable property flow to respond to a writable property update you should pass in the value
        /// as an instance of <see cref="PayloadSerializer.CreateWritablePropertyResponse(object, int, long, string)"/>
        /// to ensure the correct formatting is applied when the object is serialized.
        /// </para>
        /// </remarks>
        /// <param name="properties">A collection of properties to add.</param>
        /// <exception cref="ArgumentNullException"><paramref name="properties"/> is <c>null</c>.</exception>
        public void AddRootProperties(IDictionary<string, object> properties)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            properties
                .ToList()
                .ForEach(entry => Collection.Add(entry.Key, entry.Value));
        }

        /// <inheritdoc path="/summary" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <inheritdoc path="/remarks" cref="AddOrUpdateComponentProperties(string, IDictionary{string, object})" />
        /// <inheritdoc path="/seealso" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <param name="propertyName">The name of the property to add or update.</param>
        /// <param name="propertyValue">The value of the property to add or update.</param>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <c>null</c>.</exception>
        public void AddOrUpdateRootProperty(string propertyName, object propertyValue)
            => AddInternal(new Dictionary<string, object> { { propertyName, propertyValue } }, null, true);

        /// <inheritdoc path="/summary" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <inheritdoc path="/remarks" cref="AddOrUpdateComponentProperties(string, IDictionary{string, object})" />
        /// <inheritdoc path="/seealso" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <param name="componentName">The component with the property to add or update.</param>
        /// <param name="propertyName">The name of the property to add or update.</param>
        /// <param name="propertyValue">The value of the property to add or update.</param>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <c>null</c>.</exception>
        public void AddOrUpdateComponentProperty(string componentName, string propertyName, object propertyValue)
            => AddInternal(new Dictionary<string, object> { { propertyName, propertyValue } }, componentName, true);

        /// <inheritdoc path="/summary" cref="AddInternal(IDictionary{string, object}, string, bool)" />
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
        public void AddOrUpdateComponentProperties(string componentName, IDictionary<string, object> properties)
            => AddInternal(properties, componentName, true);

        /// <inheritdoc path="/summary" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <inheritdoc path="/seealso" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <inheritdoc path="/exception" cref="AddInternal(IDictionary{string, object}, string, bool)" />
        /// <remarks>
        /// If the collection has a key that matches this will overwrite the current value. Otherwise it will attempt to add this to the collection.
        /// <para>
        /// When using this as part of the writable property flow to respond to a writable property update you should pass in the value
        /// as an instance of <see cref="PayloadSerializer.CreateWritablePropertyResponse(object, int, long, string)"/>
        /// to ensure the correct formatting is applied when the object is serialized.
        /// </para>
        /// </remarks>
        /// <param name="properties">A collection of properties to add or update.</param>
        /// <exception cref="ArgumentNullException"><paramref name="properties"/> is <c>null</c>.</exception>
        public void AddOrUpdateRootProperties(IDictionary<string, object> properties)
            => properties
                .ToList()
                .ForEach(entry => Collection[entry.Key] = entry.Value);

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
        /// To get the value of a top-level property use <see cref="PayloadCollection.TryGetValue{T}(string, out T)"/>.
        /// </remarks>
        /// <typeparam name="T">The type to cast the object to.</typeparam>
        /// <param name="componentName">The component which holds the required property.</param>
        /// <param name="propertyName">The property to get.</param>
        /// <param name="propertyValue">When this method returns true, this contains the value of the component-level property.
        /// When this method returns false, this contains the default value of the type <c>T</c> passed in.</param>
        /// <returns>True if a component-level property of type <c>T</c> with the specified key was found; otherwise, it returns false.</returns>
        public virtual bool TryGetValue<T>(string componentName, string propertyName, out T propertyValue)
        {
            if (Logging.IsEnabled && Convention == null)
            {
                Logging.Info(this, $"The convention for this collection is not set; this typically means this collection was not created by the client. " +
                    $"TryGetValue will attempt to get the property value but may not behave as expected.", nameof(TryGetValue));
            }

            // If either the component name or the property name is null, empty or whitespace,
            // then return false with the default value of the type <T> passed in.
            if (string.IsNullOrWhiteSpace(componentName) || string.IsNullOrWhiteSpace(propertyName))
            {
                propertyValue = default;
                return false;
            }

            if (Contains(componentName, propertyName))
            {
                object componentProperties = Collection[componentName];

                // If the ClientPropertyCollection was constructed by the user application (eg. for updating the client properties)
                // then the componentProperties are retrieved as a dictionary.
                // The required property value can be fetched from the dictionary directly.
                if (componentProperties is IDictionary<string, object> nestedDictionary)
                {
                    // First verify that the retrieved dictionary contains the component identifier { "__t": "c" }.
                    // If not, then the retrieved nested dictionary is actually a root-level property of type map.
                    if (nestedDictionary.TryGetValue(ConventionBasedConstants.ComponentIdentifierKey, out object componentIdentifierValue)
                        && componentIdentifierValue.ToString() == ConventionBasedConstants.ComponentIdentifierValue)
                    {
                        if (nestedDictionary.TryGetValue(propertyName, out object dictionaryElement))
                        {
                            // If the value associated with the key is null, then return true with the default value of the type <T> passed in.
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
                }
                else
                {
                    // If the ClientPropertyCollection was constructed by the SDK (eg. when retrieving the client properties)
                    // then the componentProperties are retrieved as the json object that is defined in the PayloadConvention.
                    // The required property value then needs to be deserialized accordingly.
                    try
                    {
                        // First verify that the retrieved dictionary contains the component identifier { "__t": "c" }.
                        // If not, then the retrieved nested dictionary is actually a root-level property of type map.
                        if (Convention
                            .PayloadSerializer
                            .TryGetNestedObjectValue(componentProperties, ConventionBasedConstants.ComponentIdentifierKey, out string componentIdentifierValue)
                            && componentIdentifierValue == ConventionBasedConstants.ComponentIdentifierValue)
                        {
                            // Since the value cannot be cast to <T> directly, we need to try to convert it using the serializer.
                            // If it can be successfully converted, go ahead and return it.
                            Convention.PayloadSerializer.TryGetNestedObjectValue<T>(componentProperties, propertyName, out propertyValue);
                            return true;
                        }
                    }
                    catch
                    {
                        // In case the value cannot be converted using the serializer,
                        // then return false with the default value of the type <T> passed in.
                    }
                }
            }

            propertyValue = default;
            return false;
        }

        /// <summary>
        /// Converts a <see cref="TwinCollection"/> collection to a properties collection.
        /// </summary>
        /// <remarks>This method is used to translate the twin desired properties into writable property update requests.
        /// This internal class is aware of the implementation of the TwinCollection.</remarks>
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
                object propertyValueAsObject = property.Value;

                // Check if the property value is for a root property or a component property.
                // A component property be a JObject and will have the "__t": "c" identifiers.
                bool isComponentProperty = propertyValueAsObject is JObject
                    && payloadConvention.PayloadSerializer.TryGetNestedObjectValue(propertyValueAsObject, ConventionBasedConstants.ComponentIdentifierKey, out string _);

                if (isComponentProperty)
                {
                    var collectionDictionary = new Dictionary<string, object>();

                    // If this is a component property then the collection is a JObject with each individual property as a writable property update request.
                    var propertyValueAsJObject = (JObject)propertyValueAsObject;

                    foreach (KeyValuePair<string, JToken> componentProperty in propertyValueAsJObject)
                    {
                        object individualPropertyValue;
                        if (componentProperty.Key == ConventionBasedConstants.ComponentIdentifierKey)
                        {
                            individualPropertyValue = componentProperty.Value;
                        }
                        else
                        {
                            individualPropertyValue = new WritableClientProperty
                            {
                                Convention = payloadConvention,
                                Value = payloadConvention.PayloadSerializer.DeserializeToType<object>(Newtonsoft.Json.JsonConvert.SerializeObject(componentProperty.Value)),
                                Version = twinCollection.Version,
                            };
                        }
                        collectionDictionary.Add(componentProperty.Key, individualPropertyValue);
                    }
                    propertyCollectionToReturn.Add(property.Key, collectionDictionary);
                }
                else
                {
                    var writableProperty = new WritableClientProperty
                    {
                        Convention = payloadConvention,
                        Value = payloadConvention.PayloadSerializer.DeserializeToType<object>(Newtonsoft.Json.JsonConvert.SerializeObject(propertyValueAsObject)),
                        Version = twinCollection.Version,
                    };
                    propertyCollectionToReturn.Add(property.Key, writableProperty);
                }
            }
            // The version information is not accessible via the enumerator, so assign it separately.
            propertyCollectionToReturn.Version = twinCollection.Version;

            return propertyCollectionToReturn;
        }

        // This method is used to convert the received twin into client properties (reported + desired).
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
        /// <exception cref="ArgumentNullException"><paramref name="properties"/> is <c>null</c> for a top-level property operation.</exception>
        private void AddInternal(IDictionary<string, object> properties, string componentName = default, bool forceUpdate = false)
        {
            // If the componentName is null then simply add the key-value pair to Collection dictionary.
            // This will either insert a property or overwrite it if it already exists.
            if (componentName == null)
            {
                // If both the component name and properties collection are null then throw a ArgumentNullException.
                // This is not a valid use-case.
                if (properties == null)
                {
                    throw new ArgumentNullException(nameof(properties));
                }

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
                Dictionary<string, object> componentProperties = null;

                // If the supplied properties are non-null, then add or update the supplied property dictionary to the collection.
                // If the supplied properties are null, then this operation is to remove a component from the client's twin representation.
                // It is added to the collection as-is.
                if (properties != null)
                {
                    // If the component name already exists within the dictionary, then the value is a dictionary containing the component level property key and values.
                    // Otherwise, it is added as a new entry.
                    componentProperties = new Dictionary<string, object>();
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
                }

                Collection[componentName] = componentProperties;
            }
        }
    }
}
