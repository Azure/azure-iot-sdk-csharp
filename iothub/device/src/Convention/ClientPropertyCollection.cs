// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A collection of properties for the client.
    /// </summary>
    public class ClientPropertyCollection : PayloadCollection
    {
        private const string VersionName = "$version";

        /// <inheritdoc path="/summary" cref="AddInternal(IDictionary{string, object}, string)" />
        /// <remarks>
        /// When using this as part of the writable property flow to respond to a writable property update, pass in
        /// <paramref name="propertyValue"/> as an instance of
        /// <see cref="PayloadSerializer.CreateWritablePropertyResponse(object, int, long, string)"/>
        /// from <see cref="DeviceClient.PayloadConvention"/> (or <see cref="ModuleClient.PayloadConvention"/>)
        /// to ensure the correct formatting is applied when the object is serialized.
        /// </remarks>
        /// <param name="propertyName">The name of the property to add or update.</param>
        /// <param name="propertyValue">The value of the property to add or update.</param>
        /// <inheritdoc path="/seealso" cref="AddInternal(IDictionary{string, object}, string)" />
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <c>null</c>.</exception>
        public void AddRootProperty(string propertyName, object propertyValue)
            => AddInternal(new Dictionary<string, object> { { propertyName, propertyValue } }, null);

        /// <inheritdoc path="/summary" cref="AddInternal(IDictionary{string, object}, string)" />
        /// <inheritdoc path="/seealso" cref="AddInternal(IDictionary{string, object}, string)" />
        /// <param name="componentName">The component with the property to add or update.</param>
        /// <param name="propertyName">The name of the property to add or update.</param>
        /// <param name="propertyValue">The value of the property to add or update.</param>
        /// <exception cref="ArgumentNullException"><paramref name="componentName"/> or <paramref name="propertyName"/> is <c>null</c>.</exception>
        public void AddComponentProperty(string componentName, string propertyName, object propertyValue)
        {
            if (componentName == null)
            {
                throw new ArgumentNullException(nameof(componentName));
            }

            AddInternal(new Dictionary<string, object> { { propertyName, propertyValue } }, componentName);
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
                return Convention.PayloadSerializer.TryGetNestedJsonObjectValue<object>(component, propertyName, out _);
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
        /// If this instance is in an update request for a writable property, <paramref name="propertyValue"/> should be of type
        /// <see cref="WritableClientProperty"/>.
        /// <para>
        /// To get the value of a top-level property use <see cref="PayloadCollection.TryGetValue{T}(string, out T)"/>.
        /// </para>
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

            // While retrieving the property value from the collection:
            // 1. A property collection constructed by the client application - can be retrieved using dictionary indexer.
            // 2. Client property received through writable property update callbacks - stored internally as a WritableClientProperty.
            // 3. Client property returned through GetClientProperties:
            //  a. Client reported properties sent by the client application in response to writable property update requests - stored as a JSON object
            //      and needs to be converted to an IWritablePropertyResponse implementation using the payload serializer.
            //  b. Client reported properties sent by the client application - stored as a JSON object
            //      and needs to be converted to the expected type using the payload serializer.
            //  c. Writable property update request received - stored as a JSON object
            //      and needs to be converted to the expected type using the payload serializer.

            if (Contains(componentName, propertyName))
            {
                object componentProperties = Collection[componentName];

                // If the ClientPropertyCollection was constructed by the user application (eg. for updating the client properties)
                // or was returned by the application as a writable property update request then the componentProperties are retrieved as a dictionary.
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

                            // Case 1:
                            // If the object is of type T or can be cast to type T, go ahead and return it.
                            if (dictionaryElement is T valueRef
                                || ObjectConversionHelpers.TryCastNumericTo(dictionaryElement, out valueRef))
                            {
                                propertyValue = valueRef;
                                return true;
                            }

                            // Case 2:
                            // Check if the retrieved value is a writable property update request
                            if (dictionaryElement is WritableClientProperty writableClientProperty)
                            {
                                object writableClientPropertyValue = writableClientProperty.Value;

                                // If the object is of type T or can be cast or converted to type T, go ahead and return it.
                                if (ObjectConversionHelpers.TryCastOrConvert(writableClientPropertyValue, Convention, out propertyValue))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
                else
                {
                    try
                    {
                        // First verify that the retrieved dictionary contains the component identifier { "__t": "c" }.
                        // If not, then the retrieved nested dictionary is actually a root-level property of type map.
                        if (Convention
                            .PayloadSerializer
                            .TryGetNestedJsonObjectValue(componentProperties, ConventionBasedConstants.ComponentIdentifierKey, out string componentIdentifierValue)
                            && componentIdentifierValue == ConventionBasedConstants.ComponentIdentifierValue)
                        {
                            Convention.PayloadSerializer.TryGetNestedJsonObjectValue(componentProperties, propertyName, out object retrievedPropertyValue);

                            try
                            {
                                // Case 3a:
                                // Check if the retrieved value is a writable property update acknowledgment
                                var newtonsoftWritablePropertyResponse = Convention.PayloadSerializer.ConvertFromJsonObject<NewtonsoftJsonWritablePropertyResponse>(retrievedPropertyValue);

                                if (typeof(IWritablePropertyResponse).IsAssignableFrom(typeof(T)))
                                {
                                    // If T is IWritablePropertyResponse the property value should be of type IWritablePropertyResponse as defined in the PayloadSerializer.
                                    // We'll convert the json object to NewtonsoftJsonWritablePropertyResponse and then convert it to the appropriate IWritablePropertyResponse object.
                                    propertyValue = (T)Convention.PayloadSerializer.CreateWritablePropertyResponse(
                                        newtonsoftWritablePropertyResponse.Value,
                                        newtonsoftWritablePropertyResponse.AckCode,
                                        newtonsoftWritablePropertyResponse.AckVersion,
                                        newtonsoftWritablePropertyResponse.AckDescription);
                                    return true;
                                }

                                object writablePropertyValue = newtonsoftWritablePropertyResponse.Value;

                                // If the object is of type T or can be cast or converted to type T, go ahead and return it.
                                if (ObjectConversionHelpers.TryCastOrConvert(writablePropertyValue, Convention, out propertyValue))
                                {
                                    return true;
                                }
                            }
                            catch
                            {
                                // In case of an exception ignore it and continue.
                            }

                            // Case 3b, 3c:
                            // Since the value cannot be cast to <T> directly, we need to try to convert it using the serializer.
                            // If it can be successfully converted, go ahead and return it.
                            if (Convention.PayloadSerializer.TryGetNestedJsonObjectValue<T>(componentProperties, propertyName, out propertyValue))
                            {
                                return true;
                            }
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

        // This method is used to convert the received twin into client properties (reported + desired).
        internal static ClientPropertyCollection FromClientPropertiesAsDictionary(IDictionary<string, object> clientProperties, PayloadConvention payloadConvention)
        {
            if (clientProperties == null)
            {
                throw new ArgumentNullException(nameof(clientProperties));
            }

            var propertyCollectionToReturn = new ClientPropertyCollection
            {
                Convention = payloadConvention,
            };

            foreach (KeyValuePair<string, object> property in clientProperties)
            {
                // The version information should not be a part of the enumerable ProperyCollection, but rather should be
                // accessible through its dedicated accessor.
                if (property.Key == VersionName)
                {
                    propertyCollectionToReturn.Version = (long)property.Value;
                }
                else
                {
                    propertyCollectionToReturn.Add(property.Key, payloadConvention.PayloadSerializer.DeserializeToType<object>(JsonConvert.SerializeObject(property.Value)));
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
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="properties"/> is <c>null</c>.</exception>
        private void AddInternal(IDictionary<string, object> properties, string componentName = default)
        {
            // If the componentName is null then simply add the key-value pair to Collection dictionary.
            // This will either insert a property or overwrite it if it already exists.
            if (componentName == null)
            {
                // If both the component name and properties collection are null then throw an ArgumentNullException.
                // This is not a valid use-case.
                if (properties == null)
                {
                    throw new ArgumentNullException(nameof(properties));
                }

                foreach (KeyValuePair<string, object> entry in properties)
                {
                    // A null property key is not allowed. Throw an ArgumentNullException.
                    if (entry.Key == null)
                    {
                        throw new ArgumentNullException(nameof(entry.Key));
                    }

                    Collection[entry.Key] = entry.Value;
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
                        // A null property key is not allowed. Throw an ArgumentNullException.
                        if (entry.Key == null)
                        {
                            throw new ArgumentNullException(nameof(entry.Key));
                        }

                        componentProperties[entry.Key] = entry.Value;
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
