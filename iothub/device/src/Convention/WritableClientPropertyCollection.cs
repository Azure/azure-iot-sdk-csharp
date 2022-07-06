// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// 
    /// </summary>
    public class WritableClientPropertyCollection : IEnumerable<KeyValuePair<string, object>>
    {
        private const string VersionName = "$version";

        // TODO: Unit-testable and mockable

        internal WritableClientPropertyCollection(IDictionary<string, object> writableClientPropertyRequests, PayloadConvention payloadConvention)
        {
            Convention = payloadConvention;
            PopulateWritableClientProperties(writableClientPropertyRequests);
        }

        /// <summary>
        /// Gets the version of the writable property collection.
        /// </summary>
        /// <remarks>
        /// IoT Hub does not preserve writable property update notifications for disconnected devices/modules.
        /// On connecting, the client should retreive the full property document through <see cref="DeviceClient.GetClientPropertiesAsync(System.Threading.CancellationToken)"/>/
        /// <see cref="ModuleClient.GetClientPropertiesAsync(System.Threading.CancellationToken)"/> in addition to subscribing for update notifications
        /// through <see cref="DeviceClient.SubscribeToWritablePropertyUpdateRequestsAsync(Func{WritableClientPropertyCollection, Task}, System.Threading.CancellationToken)"/>/
        /// <see cref="ModuleClient.SubscribeToWritablePropertyUpdateRequestsAsync(Func{WritableClientPropertyCollection, Task}, System.Threading.CancellationToken)"/>.
        /// The client application can ignore all update notifications with version less that or equal to the version of the full document.
        /// </remarks>
        /// <value>A <see cref="long"/> that is used to identify the version of the writable property collection.</value>
        public long Version { get; private set; }

        internal IDictionary<string, object> WritableClientProperties { get; } = new Dictionary<string, object>();

        internal PayloadConvention Convention { get; set; }

        /// <summary>
        /// Determines whether the specified root-level property is present in the received writable property update request.
        /// </summary>
        /// <param name="propertyName">The property to locate.</param>
        /// <returns><c>true</c> if the specified property is present; otherwise, <c>false</c>.</returns>
        public bool Contains(string propertyName)
        {
            return WritableClientProperties.ContainsKey(propertyName);
        }

        /// <summary>
        /// Determines whether the specified component-level property is present in the received writable property update request.
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

            if (WritableClientProperties.TryGetValue(componentName, out object component))
            {
                // The SDK constructed writable property collection is dictionary for root-level only properties. In case component-level properties
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
        /// <param name="propertyName">The property to get.</param>
        /// <param name="propertyValue">When this method returns true, this contains the value of the root-level property.
        /// When this method returns false, this contains an empty <see cref="WritableClientProperty"/>.</param>
        /// <returns><c>true</c> if a root-level property with the specified key was found; otherwise, <c>false</c>.</returns>
        public bool TryGetWritableClientProperty(string propertyName, out WritableClientProperty propertyValue)
        {
            propertyValue = default;

            // If the key is null, empty or whitespace, then return false with an empty WritableClientProperty.
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return false;
            }

            if (Contains(propertyName))
            {
                object retrievedPropertyValue = WritableClientProperties[propertyName];

                // Check if the retrieved value is a writable property update request
                if (retrievedPropertyValue is WritableClientProperty writableClientProperty)
                {
                    propertyValue = writableClientProperty;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the value of a root-level property.
        /// See <see cref="TryGetWritableClientProperty(string, out WritableClientProperty)"/> to get a <see cref="WritableClientProperty"/> object
        /// which has a convenience method to help you build the writable property acknowledgement object.
        /// </summary>
        /// <typeparam name="T">The type to cast the <paramref name="propertyValue"/> to.</typeparam>
        /// <param name="propertyName">The property to get.</param>
        /// <param name="propertyValue">When this method returns true, this contains the value of the root-level property.
        /// When this method returns false, this contains the default value of the type <c>T</c> passed in.</param>
        /// <returns><c>true</c> if a root-level property of type <c>T</c> with the specified key was found; otherwise, <c>false</c>.</returns>
        public bool TryGetValue<T>(string propertyName, out T propertyValue)
        {
            propertyValue = default;

            if (TryGetWritableClientProperty(propertyName, out WritableClientProperty writableClientProperty))
            {
                // If the object is of type T or can be cast or converted to type T, go ahead and return it.
                if (writableClientProperty.TryGetValue(out propertyValue))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the value of a component-level property.
        /// </summary>
        /// <param name="componentName">The component which holds the required property.</param>
        /// <param name="propertyName">The property to get.</param>
        /// <param name="propertyValue">When this method returns true, this contains the value of the component-level property.
        /// When this method returns false, this contains an empty <see cref="WritableClientProperty"/>.</param>
        /// <returns><c>true</c> if a component-level property with the specified key was found; otherwise, <c>false</c>.</returns>
        public bool TryGetWritableClientProperty(string componentName, string propertyName, out WritableClientProperty propertyValue)
        {
            propertyValue = default;

            // If either the component name or the property name is null, empty or whitespace,
            // then return false with an empty WritableClientProperty.
            if (string.IsNullOrWhiteSpace(componentName) || string.IsNullOrWhiteSpace(propertyName))
            {
                return false;
            }

            if (Contains(componentName, propertyName))
            {
                object componentProperties = WritableClientProperties[componentName];

                if (componentProperties is IDictionary<string, object> nestedDictionary)
                {
                    // First verify that the retrieved dictionary contains the component identifier { "__t": "c" }.
                    // If not, then the retrieved nested dictionary is actually a root-level property of type map.
                    if (nestedDictionary.TryGetValue(ConventionBasedConstants.ComponentIdentifierKey, out object componentIdentifierValue)
                        && componentIdentifierValue.ToString() == ConventionBasedConstants.ComponentIdentifierValue)
                    {
                        if (nestedDictionary.TryGetValue(propertyName, out object dictionaryElement))
                        {
                            // Check if the retrieved value is a writable property update request
                            if (dictionaryElement is WritableClientProperty writableClientProperty)
                            {
                                propertyValue = writableClientProperty;
                                return true;
                            }
                        }
                    }
                }
            }

            propertyValue = default;
            return false;
        }

        /// <summary>
        /// Gets the value of a component-level property.
        /// See <see cref="TryGetWritableClientProperty(string, out WritableClientProperty)"/> to get a <see cref="WritableClientProperty"/> object
        /// which has a convenience method to help you build the writable property acknowledgement object.
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

            if (TryGetWritableClientProperty(componentName, propertyName, out WritableClientProperty writableClientProperty))
            {
                // If the object is of type T or can be cast or converted to type T, go ahead and return it.
                if (writableClientProperty.TryGetValue(out propertyValue))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (KeyValuePair<string, object> property in WritableClientProperties)
            {
                yield return property;
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void PopulateWritableClientProperties(IDictionary<string, object> writableClientPropertyRequests)
        {
            // The version information should not be a part of the enumerable ProperyCollection, but rather should be
            // accessible through its dedicated accessor.
            bool versionPresent = writableClientPropertyRequests.TryGetValue(VersionName, out object version);
            Version = versionPresent
                ? (long)version
                : throw new IotHubException("Properties document missing version number. Contact service with logs.");

            foreach (KeyValuePair<string, object> property in writableClientPropertyRequests)
            {
                // Ignore the version entry since we've already saved it off.
                if (property.Key == VersionName)
                {
                    // no-op
                }
                else
                {
                    // Serialize the received property value. You can use the default serializer here as the response has previously been deserialized using the default serializer.
                    object propertyValueAsObject = property.Value;

                    // Check if the property value is for a root property or a component property.
                    // A component property be a JObject and will have the "__t": "c" identifiers.
                    // The component property collection will be a JObject because it has been deserailized into a dictionary using Newtonsoft.Json.
                    bool isComponentProperty = propertyValueAsObject is JObject propertyValueAsJObject
                        && NewtonsoftJsonPayloadSerializer.Instance.TryGetNestedJsonObjectValue(propertyValueAsJObject, ConventionBasedConstants.ComponentIdentifierKey, out string _);

                    if (isComponentProperty)
                    {
                        // If this is a component property then the collection is a JObject with each individual property as a writable property update request.
                        var componentPropertiesAsJObject = (JObject)propertyValueAsObject;
                        var collectionDictionary = new Dictionary<string, object>();

                        foreach (KeyValuePair<string, JToken> componentProperty in componentPropertiesAsJObject)
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
                                    Convention = Convention,
                                    Value = Convention.PayloadSerializer.DeserializeToType<object>(JsonConvert.SerializeObject(componentProperty.Value)),
                                    Version = Version,
                                };
                            }
                            collectionDictionary.Add(componentProperty.Key, individualPropertyValue);
                        }
                        WritableClientProperties.Add(property.Key, collectionDictionary);
                    }
                    else
                    {
                        var individualPropertyValue = new WritableClientProperty
                        {
                            Convention = Convention,
                            Value = Convention.PayloadSerializer.DeserializeToType<object>(JsonConvert.SerializeObject(propertyValueAsObject)),
                            Version = Version,
                        };
                        WritableClientProperties.Add(property.Key, individualPropertyValue);
                    }
                }
            }
        }
    }
}
