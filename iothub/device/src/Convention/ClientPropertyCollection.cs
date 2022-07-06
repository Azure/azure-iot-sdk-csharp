// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Client.Exceptions;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// 
    /// </summary>
    public class ClientPropertyCollection : IEnumerable<KeyValuePair<string, object>>
    {
        private const string VersionName = "$version";

        // TODO: Unit-testable and mockable

        /// <summary>
        /// 
        /// </summary>
        public ClientPropertyCollection()
        {
            // The Convention for user created ClientPropertyCollection is set in the InternalClient
            // right before the payload bytes are sent to the transport layer.
        }

        internal ClientPropertyCollection(IDictionary<string, object> clientPropertiesReported, PayloadConvention payloadConvention)
        {
            Convention = payloadConvention;
            PopulateClientPropertiesReported(clientPropertiesReported);
        }

        /// <summary>
        /// Gets the version of the client reported property collection.
        /// </summary>
        /// <value>A <see cref="long"/> that is used to identify the version of the client reported property collection.</value>
        public long Version { get; private set; }

        internal IDictionary<string, object> ClientPropertiesReported { get; } = new Dictionary<string, object>();

        internal PayloadConvention Convention { get; set; }

        /// <summary>
        /// Required to allow new ClientPropertyCollection { [prop] = ... } initialization.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual object this[string key]
        {
            get => ClientPropertiesReported[key];
            set => Add(key, value);
        }

        /// <summary>
        ///  Required to allow new ClientPropertyCollection { { .... } } initialization.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public virtual void Add(string key, object value)
        {
            ClientPropertiesReported[key] = value;
        }

        /// <summary>
        /// Adds or updates the value for the collection.
        /// </summary>
        /// <remarks>
        /// When using this as part of the writable property flow to respond to a writable property update, pass in
        /// <paramref name="propertyValue"/> as an instance of
        /// <see cref="PayloadSerializer.CreateWritablePropertyResponse(object, int, long, string)"/>
        /// from <see cref="DeviceClient.PayloadConvention"/> (or <see cref="ModuleClient.PayloadConvention"/>)
        /// to ensure the correct formatting is applied when the object is serialized.
        /// You can use the convenience method <see cref="WritableClientProperty.AcknowledgeWith(int, string)"/> to create this acknowledgement object.
        /// </remarks>
        /// <inheritdoc path="/seealso" cref="AddInternal(IDictionary{string, object}, string)" />
        /// <param name="propertyName">The name of the property to add or update.</param>
        /// <param name="propertyValue">The value of the property to add or update.</param>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <c>null</c>.</exception>
        public void AddRootProperty(string propertyName, object propertyValue)
            => AddInternal(new Dictionary<string, object> { { propertyName, propertyValue } }, null);

        /// <summary>
        /// Adds or updates the value for the collection.
        /// </summary>
        /// <remarks>
        /// When using this as part of the writable property flow to respond to a writable property update, pass in
        /// <paramref name="propertyValue"/> as an instance of
        /// <see cref="PayloadSerializer.CreateWritablePropertyResponse(object, int, long, string)"/>
        /// from <see cref="DeviceClient.PayloadConvention"/> (or <see cref="ModuleClient.PayloadConvention"/>)
        /// to ensure the correct formatting is applied when the object is serialized.
        /// You can use the convenience method <see cref="WritableClientProperty.AcknowledgeWith(int, string)"/> to create this acknowledgement object.
        /// </remarks>
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
        /// Determines whether the specified root-level property is present in the client reported property collection.
        /// </summary>
        /// <param name="propertyName">The property to locate.</param>
        /// <returns><c>true</c> if the specified property is present; otherwise, <c>false</c>.</returns>
        public bool Contains(string propertyName)
        {
            return ClientPropertiesReported.ContainsKey(propertyName);
        }

        /// <summary>
        /// Determines whether the specified component-level property is present in the client reported property collection.
        /// </summary>
        /// <param name="propertyName">The property to locate.</param>
        /// <param name="componentName">The component which holds the required property.</param>
        /// <returns><c>true</c> if the specified property is present; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="componentName"/> is null. See the root-level <see cref="Contains(string)"/> instead.</exception>
        public bool Contains(string componentName, string propertyName)
        {
            if (componentName == null)
            {
                throw new ArgumentNullException(nameof(componentName), "It looks like you are trying determine if a root-level property is present in the collection. " +
                    "Use the method Contains(string propertyName) instead.");
            }

            if (ClientPropertiesReported.TryGetValue(componentName, out object component))
            {
                // The SDK constructed client reported property collection is dictionary for root-level only properties. In case component-level properties
                // are also present, it is then a multi-level nested dictionary.
                if (component is IDictionary<string, object> componentPropertiesCollection)
                {
                    return componentPropertiesCollection.ContainsKey(propertyName);
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the value of a root-level property.
        /// </summary>
        /// <typeparam name="T">The type to cast the <paramref name="propertyValue"/> to.</typeparam>
        /// <param name="propertyName">The property to get.</param>
        /// <param name="propertyValue">When this method returns true, this contains the value of the root-level property.
        /// When this method returns false, this contains the default value of the type <c>T</c> passed in.</param>
        /// <returns><c>true</c> if a root-level property of type <c>T</c> with the specified key was found; otherwise, <c>false</c>.</returns>
        public bool TryGetValue<T>(string propertyName, out T propertyValue)
        {
            propertyValue = default;

            // If the key is null, empty or whitespace, then return false with the default value of the type <T> passed in.
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return false;
            }

            // While retrieving the property value from the collection:
            // 1. A property collection constructed by the client application - can be retrieved using dictionary indexer.
            // 2. Client property returned through GetClientProperties:
            //  a. Client reported properties sent by the client application in response to writable property update requests - stored as a JSON object
            //      and needs to be converted to an IWritablePropertyResponse implementation using the payload serializer.
            //  b. Client reported properties sent by the client application - stored as a JSON object
            //      and needs to be converted to the expected type using the payload serializer.

            if (Contains(propertyName))
            {
                object retrievedPropertyValue = ClientPropertiesReported[propertyName];

                // If the value associated with the key is null, then return true with the default value of the type <T> passed in.
                if (retrievedPropertyValue == null)
                {
                    return true;
                }

                // Case 1:
                // If the object is of type T or can be cast to type T, go ahead and return it.
                if (ObjectConversionHelpers.TryCast(retrievedPropertyValue, out propertyValue))
                {
                    return true;
                }

                try
                {
                    try
                    {
                        // Case 2a:
                        // Check if the retrieved value is a writable property update acknowledgment
                        NewtonsoftJsonWritablePropertyResponse newtonsoftWritablePropertyResponse = Convention
                            .PayloadSerializer
                            .ConvertFromJsonObject<NewtonsoftJsonWritablePropertyResponse>(retrievedPropertyValue);

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

                    // Case 2b:
                    // If the value cannot be cast to <T> directly we need to try to convert it using the serializer.
                    // If it can be successfully converted, go ahead and return it.
                    propertyValue = Convention.PayloadSerializer.ConvertFromJsonObject<T>(retrievedPropertyValue);
                    return true;
                }
                catch
                {
                    // In case the value cannot be converted using the serializer,
                    // then return false with the default value of the type <T> passed in.
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the value of a component-level property.
        /// </summary>
        /// <typeparam name="T">The type to cast the <paramref name="propertyValue"/> to.</typeparam>
        /// <param name="componentName">The component which holds the required property.</param>
        /// <param name="propertyName">The property to get.</param>
        /// <param name="propertyValue">When this method returns true, this contains the value of the component-level property.
        /// When this method returns false, this contains the default value of the type <c>T</c> passed in.</param>
        /// <returns><c>true</c> if a component-level property of type <c>T</c> with the specified key was found; otherwise, <c>false</c>.</returns>
        public bool TryGetValue<T>(string componentName, string propertyName, out T propertyValue)
        {
            propertyValue = default;

            // If either the component name or the property name is null, empty or whitespace,
            // then return false with the default value of the type <T> passed in.
            if (string.IsNullOrWhiteSpace(componentName) || string.IsNullOrWhiteSpace(propertyName))
            {
                return false;
            }

            // While retrieving the property value from the collection:
            // 1. A property collection constructed by the client application - can be retrieved using dictionary indexer.
            // 2. Client property returned through GetClientProperties:
            //  a. Client reported properties sent by the client application in response to writable property update requests - stored as a JSON object
            //      and needs to be converted to an IWritablePropertyResponse implementation using the payload serializer.
            //  b. Client reported properties sent by the client application - stored as a JSON object
            //      and needs to be converted to the expected type using the payload serializer.

            if (Contains(componentName, propertyName))
            {
                object componentProperties = ClientPropertiesReported[componentName];

                // The componentProperties should be retrieved as a dictionary.
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

                            try
                            {
                                try
                                {
                                    // Case 2a:
                                    // Check if the retrieved value is a writable property update acknowledgment
                                    NewtonsoftJsonWritablePropertyResponse newtonsoftWritablePropertyResponse = Convention
                                        .PayloadSerializer
                                        .ConvertFromJsonObject<NewtonsoftJsonWritablePropertyResponse>(dictionaryElement);

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

                                // Case 2b:
                                // Since the value cannot be cast to <T> directly, we need to try to convert it using the serializer.
                                // If it can be successfully converted, go ahead and return it.
                                propertyValue = Convention.PayloadSerializer.ConvertFromJsonObject<T>(dictionaryElement);
                                return true;
                            }
                            catch
                            {
                                // In case the value cannot be converted using the serializer,
                                // then return false with the default value of the type <T> passed in.
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (KeyValuePair<string, object> property in ClientPropertiesReported)
            {
                yield return property;
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Gets the collection as a byte array.
        /// </summary>
        /// <remarks>
        /// This will get the fully encoded serialized string using both <see cref="PayloadSerializer.SerializeToString(object)"/>.
        /// and <see cref="PayloadEncoder.EncodeStringToByteArray(string)"/> methods implemented in the <see cref="PayloadConvention"/>.
        /// </remarks>
        /// <returns>A fully encoded serialized string.</returns>
        internal virtual byte[] GetPayloadObjectBytes()
        {
            return Convention.GetObjectBytes(ClientPropertiesReported);
        }

        private void PopulateClientPropertiesReported(IDictionary<string, object> clientPropertiesReported)
        {
            foreach (KeyValuePair<string, object> property in clientPropertiesReported)
            {
                // The version information should not be a part of the enumerable ProperyCollection, but rather should be
                // accessible through its dedicated accessor.
                if (property.Key == VersionName)
                {
                    Version = (long)property.Value;
                }
                else
                {
                    // Serialize the received property value. You can use the default serializer here as the response has previously been deserialized using the default serializer.
                    object propertyValueAsObject = property.Value;
                    string propertyValueAsString = DefaultPayloadConvention.Instance.PayloadSerializer.SerializeToString(propertyValueAsObject);

                    // Check if the property value is for a root property or a component property.
                    // A component property be a JObject and will have the "__t": "c" identifiers.
                    // The component property collection will be a JObject because it has been deserailized into a dictionary using Newtonsoft.Json.
                    bool isComponentProperty = propertyValueAsObject is JObject
                        && Convention.PayloadSerializer.TryGetNestedJsonObjectValue(propertyValueAsString, ConventionBasedConstants.ComponentIdentifierKey, out string _);

                    if (isComponentProperty)
                    {
                        // If this is a component property then the collection is a JObject with each individual property as a client reported property.
                        var componentPropertiesAsJObject = (JObject)propertyValueAsObject;
                        var collectionDictionary = new Dictionary<string, object>();

                        foreach (KeyValuePair<string, JToken> componentProperty in componentPropertiesAsJObject)
                        {
                            collectionDictionary.Add(componentProperty.Key, componentProperty.Value);
                        }
                        ClientPropertiesReported.Add(property.Key, collectionDictionary);
                    }
                    else
                    {
                        ClientPropertiesReported.Add(property.Key, propertyValueAsObject);
                    }
                }
            }
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

                    ClientPropertiesReported[entry.Key] = entry.Value;
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
                    if (ClientPropertiesReported.ContainsKey(componentName))
                    {
                        componentProperties = (Dictionary<string, object>)ClientPropertiesReported[componentName];
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

                ClientPropertiesReported[componentName] = componentProperties;
            }
        }
    }
}
