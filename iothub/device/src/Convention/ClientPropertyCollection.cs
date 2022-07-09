// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Devices.Client.Exceptions;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The collection of properties reported by the client.
    /// </summary>
    /// <remarks>
    /// Client reported properties can either be <see href="https://docs.microsoft.com/azure/iot-develop/concepts-convention#read-only-properties">Read-only properties</see>
    /// or they can be <see href="https://docs.microsoft.com/azure/iot-pnp/concepts-convention#writable-properties">Writable property acknowledgements</see>.
    /// </remarks>
    public class ClientPropertyCollection : IEnumerable<ClientProperty>
    {
        private const string VersionName = "$version";

        // TODO: Unit-testable and mockable

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <remarks>
        /// Use the <see cref="AddRootProperty(string, object)"/> and/or <see cref="AddComponentProperty(string, string, object)"/> methods
        /// to add properties into the collection.
        /// This collection can be reported to service using 
        /// <see cref="DeviceClient.UpdateClientPropertiesAsync(ClientPropertyCollection, System.Threading.CancellationToken)"/> or
        /// <see cref="ModuleClient.UpdateClientPropertiesAsync(ClientPropertyCollection, System.Threading.CancellationToken)"/>.
        /// </remarks>
        public ClientPropertyCollection()
        {
            // This is intended to be called when creating a collection that will be reported back.

            // The Convention for user created ClientPropertyCollection is set in the InternalClient
            // right before the payload bytes are sent to the transport layer.
        }

        internal ClientPropertyCollection(IDictionary<string, object> clientPropertiesReported, PayloadConvention payloadConvention)
        {
            // This is intended to be called when deserializing the ReportedByClient properties as a part of the GetClientPropertiesAsync() flow.

            Convention = payloadConvention;
            PopulateClientPropertiesReported(clientPropertiesReported);
        }

        /// <summary>
        /// Gets the version of the client reported property collection.
        /// </summary>
        /// <value>A <see cref="long"/> that is used to identify the version of the client reported property collection.</value>
        public long Version { get; private set; }

        internal IList<ClientProperty> ClientPropertiesReported { get; } = new List<ClientProperty>();

        internal PayloadConvention Convention { get; set; }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <remarks>
        /// If getting or setting root-level properties, pass in the property name as the <paramref name="key"/> and the property value as the <c>value</c>.
        /// If getting or setting a component, pass in the component name as the <paramref name="key"/> and the correctly formatted component-level properties as the <c>value</c>.
        /// <para>
        /// Also, see the convenience methods <see cref="AddRootProperty(string, object)"/> and <see cref="AddComponentProperty(string, string, object)"/>
        /// for adding properties to the collection, and <see cref="TryGetValue{T}(string, out T)"/> and <see cref="TryGetValue{T}(string, string, out T)"/>
        /// for retrieving properties from the collection.
        /// </para>
        /// </remarks>
        /// <param name="key">
        /// The key of the value to get or set.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <exception cref="KeyNotFoundException">The property is retrieved and the <paramref name="key"/> does not exist in the collection.</exception>
        public virtual object this[string key]
        {
            get => ClientPropertiesReported.Where(property => property.PropertyName == key).FirstOrDefault();
            set
            {
                if (value is IDictionary<string, object> componentProperties)
                {
                    foreach(KeyValuePair<string, object> property in componentProperties)
                    {
                        var individualProperty = new ClientProperty
                        {
                            ComponentName = key,
                            PropertyName = property.Key,
                            Value = property.Value,
                        };

                        // If a property with the same key already exists in the collection, then remove it.
                        IEnumerable<ClientProperty> existingProperty = ClientPropertiesReported
                            .Where(prop =>
                                prop.PropertyName == property.Key
                                && prop.ComponentName == key);

                        if (existingProperty.Any())
                        {
                            ClientPropertiesReported.Remove(existingProperty.First());
                        }
                        ClientPropertiesReported.Add(individualProperty);
                    }
                }
                else
                {
                    var individualProperty = new ClientProperty
                    {
                        PropertyName = key,
                        Value = value,
                    };

                    // If a property with the same key already exists in the collection, then remove it.
                    IEnumerable<ClientProperty> existingProperty = ClientPropertiesReported
                        .Where(property =>
                            property.PropertyName == key);

                    if (existingProperty.Any())
                    {
                        ClientPropertiesReported.Remove(existingProperty.First());
                    }
                    ClientPropertiesReported.Add(individualProperty);
                }
            }
        }

        /// <summary>
        /// Adds or updates the value to the collection.
        /// </summary>
        /// <remarks>
        /// Use this when reporting a property that is not a response of a writable property update request.
        /// When using this as part of the writable property flow to respond to a writable property update,
        /// see <see cref="AddWritableClientPropertyAcknowledgement(WritableClientPropertyAcknowledgement)"/>.
        /// </remarks>
        /// <param name="propertyName">The name of the property to add or update.</param>
        /// <param name="propertyValue">The value of the property to add or update.</param>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <c>null</c>.</exception>
        public void AddRootProperty(string propertyName, object propertyValue)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            AddInternal(new Dictionary<string, object> { { propertyName, propertyValue } }, null);
        }

        /// <summary>
        /// Adds or updates the value to the collection.
        /// </summary>
        /// <remarks>
        /// Use this when reporting a property that is not a response of a writable property update request.
        /// When using this as part of the writable property flow to respond to a writable property update,
        /// see <see cref="AddWritableClientPropertyAcknowledgement(WritableClientPropertyAcknowledgement)"/>.
        /// </remarks>
        /// <param name="componentName">The component with the property to add or update.</param>
        /// <param name="propertyName">The name of the property to add or update.</param>
        /// <param name="propertyValue">The value of the property to add or update.</param>
        /// <exception cref="ArgumentNullException"><paramref name="componentName"/> is <c>null</c>.</exception>
        public void AddComponentProperty(string componentName, string propertyName, object propertyValue)
        {
            if (componentName == null)
            {
                throw new ArgumentNullException(nameof(componentName));
            }

            // The property name can be null if this is an operation to remove the component at service-end.
            if (propertyName == null)
            {
                AddInternal(null, componentName);
            }
            else
            {
                AddInternal(new Dictionary<string, object> { { propertyName, propertyValue } }, componentName);
            }
        }

        /// <summary>
        /// Adds or updates a writable property acknowledgement value to the collection.
        /// </summary>
        /// <remarks>
        /// Use this as part of the writable property flow to respond to a writable property update.
        /// You can use the convenience method <see cref="WritableClientProperty.AcknowledgeWith(int, string)"/> to create this acknowledgement payload.
        /// </remarks>
        /// <param name="writableClientPropertyAcknowledgement">The writable property update acknowledgement payload.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writableClientPropertyAcknowledgement"/> is <c>null</c>.</exception>
        public void AddWritableClientPropertyAcknowledgement(WritableClientPropertyAcknowledgement writableClientPropertyAcknowledgement)
        {
            if (writableClientPropertyAcknowledgement == null)
            {
                throw new ArgumentNullException(nameof(writableClientPropertyAcknowledgement));
            }

            if (writableClientPropertyAcknowledgement.ComponentName == null)
            {
                // This is an acknowledgement for a root-level writable property update request.
                AddRootProperty(
                    writableClientPropertyAcknowledgement.PropertyName,
                    writableClientPropertyAcknowledgement.Payload);
            }
            else
            {
                // This is an acknowledgement for a component-level writable property update request.
                AddComponentProperty(
                    writableClientPropertyAcknowledgement.ComponentName,
                    writableClientPropertyAcknowledgement.PropertyName,
                    writableClientPropertyAcknowledgement.Payload);
            }
        }

        /// <summary>
        /// Determines whether the specified root-level property is present in the client reported property collection.
        /// </summary>
        /// <param name="propertyName">The property to locate.</param>
        /// <returns><c>true</c> if the specified property is present; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <c>null</c>.</exception>
        public bool Contains(string propertyName)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            return GetMatches(null, propertyName).Any();
        }

        /// <summary>
        /// Determines whether the specified component-level property is present in the client reported property collection.
        /// </summary>
        /// <param name="propertyName">The property to locate.</param>
        /// <param name="componentName">The component which holds the required property.</param>
        /// <returns><c>true</c> if the specified property is present; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="componentName"/> is <c>null</c>.</exception>
        public bool Contains(string componentName, string propertyName)
        {
            if (componentName == null)
            {
                throw new ArgumentNullException(nameof(componentName));
            }

            return GetMatches(componentName, propertyName).Any();
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

            return Contains(propertyName)
                && TryGetPropertyValue(null, propertyName, out propertyValue);
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

            // If the component name is null, empty or whitespace,
            // then return false with the default value of the type <T> passed in.
            // The property name can be null if this is an operation to remove the component at service-end.
            if (string.IsNullOrWhiteSpace(componentName))
            {
                return false;
            }

            return Contains(componentName, propertyName)
                && TryGetPropertyValue(componentName, propertyName, out propertyValue);
        }

        /// <inheritdoc/>
        public IEnumerator<ClientProperty> GetEnumerator()
        {
            foreach (ClientProperty property in ClientPropertiesReported)
            {
                yield return property;
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // TODO: IMPORTANT - this needs to ensure the proper dictionary and "__t: c" formatting is applied
        internal byte[] GetPayloadObjectBytes()
        {
            return Convention.GetObjectBytes(ClientPropertiesReported);
        }

        private void PopulateClientPropertiesReported(IDictionary<string, object> clientPropertiesReported)
        {
            // The version information should not be a part of the enumerable ProperyCollection, but rather should be
            // accessible through its dedicated accessor.
            bool versionPresent = clientPropertiesReported.TryGetValue(VersionName, out object version);

            Version = versionPresent && ObjectConversionHelpers.TryCastNumericTo(version, out long longVersion)
                ? longVersion
                : throw new IotHubException("Properties document either missing version number or not formatted as expected. Contact service with logs.");

            foreach (KeyValuePair<string, object> property in clientPropertiesReported)
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
                        // If this is a component property then the collection is a JObject with each individual property as a client reported property.
                        var componentPropertiesAsJObject = (JObject)propertyValueAsObject;

                        foreach (KeyValuePair<string, JToken> componentProperty in componentPropertiesAsJObject)
                        {
                            if (componentProperty.Key == ConventionBasedConstants.ComponentIdentifierKey)
                            {
                                // Ignore it. We won't be saving the component identifiers into the collection that we return to the user.
                            }
                            else
                            {
                                var individualProperty = new ClientProperty
                                {
                                    ComponentName = property.Key,
                                    PropertyName = componentProperty.Key,
                                    Value = componentProperty.Value,
                                    Convention = Convention,
                                };
                                ClientPropertiesReported.Add(individualProperty);
                            }
                        }
                    }
                    else
                    {
                        var individualProperty = new ClientProperty
                        {
                            PropertyName = property.Key,
                            Value = property.Value,
                            Convention = Convention,
                        };
                        ClientPropertiesReported.Add(individualProperty);
                    }
                }
            }
        }

        private void AddInternal(IDictionary<string, object> properties, string componentName = default)
        {
            // If both the component name and properties collection are null then throw an ArgumentNullException.
            // This is not a valid use-case.
            if (componentName == null && properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            // If the supplied properties are non-null, then add or update the supplied property dictionary to the collection.
            if (properties != null)
            {
                foreach (KeyValuePair<string, object> entry in properties)
                {
                    // A null property key is not allowed. Throw an ArgumentNullException.
                    if (entry.Key == null)
                    {
                        throw new ArgumentNullException(nameof(entry.Key));
                    }

                    var individualProperty = new ClientProperty
                    {
                        ComponentName = componentName,
                        PropertyName = entry.Key,
                        Value = entry.Value,
                        // The Convention for user created ClientPropertyCollection is set in the InternalClient
                        // right before the payload bytes are sent to the transport layer.
                    };

                    // If a property with the same key already exists in the collection, then remove it.
                    IEnumerable<ClientProperty> existingProperty = ClientPropertiesReported
                        .Where(property =>
                            property.PropertyName == entry.Key
                            && property.ComponentName == componentName);

                    if (existingProperty.Any())
                    {
                        ClientPropertiesReported.Remove(existingProperty.First());
                    }
                    ClientPropertiesReported.Add(individualProperty);
                }
            }
            else
            {
                // If the supplied properties are null, then this operation is to remove a component from the client's twin representation.

                var individualProperty = new ClientProperty
                {
                    ComponentName = componentName,
                    PropertyName = null,
                    Value = null,
                };

                // If a property with the same key already exists in the collection, then remove it.
                IEnumerable<ClientProperty> existingProperty = ClientPropertiesReported
                    .Where(property =>
                        property.ComponentName == componentName);

                if (existingProperty.Any())
                {
                    ClientPropertiesReported.Remove(existingProperty.First());
                }
                ClientPropertiesReported.Add(individualProperty);
            }
        }

        private IEnumerable<ClientProperty> GetMatches(string componentName, string propertyName)
        {
            return ClientPropertiesReported
                .Where(property =>
                    property.ComponentName == componentName
                    && property.PropertyName == propertyName);
        }

        // While retrieving the property value from the collection:
        // 1. A property collection constructed by the client application - can be retrieved using dictionary indexer.
        // 2. Client property returned through GetClientProperties:
        //  a. Client reported properties sent by the client application in response to writable property update requests - stored as a JSON object
        //      and needs to be converted to an IWritablePropertyAcknowledgementValue implementation using the payload serializer.
        //  b. Client reported properties sent by the client application - stored as a JSON object
        //      and needs to be converted to the expected type using the payload serializer.
        private bool TryGetPropertyValue<T>(string componentName, string propertyName, out T propertyValue)
        {
            propertyValue = default;

            IEnumerable<ClientProperty> matches = GetMatches(componentName, propertyName);

            // There will only be a single entry for a specific property name, so we can safely return the first element in the list.
            ClientProperty retrievedProperty = matches.FirstOrDefault();

            // If the value associated with the key is null, then return true with the default value of the type <T> passed in.
            if (retrievedProperty == null || retrievedProperty.Value == null)
            {
                return true;
            }

            object retrievedPropertyValue = retrievedProperty.Value;

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
                    NewtonsoftJsonWritablePropertyAcknowledgementPayload newtonsoftWritablePropertyAcknowledgementValue = NewtonsoftJsonPayloadSerializer
                        .Instance
                        .ConvertFromJsonObject<NewtonsoftJsonWritablePropertyAcknowledgementPayload>(retrievedPropertyValue);

                    if (typeof(IWritablePropertyAcknowledgementPayload).IsAssignableFrom(typeof(T)))
                    {
                        // If T is IWritablePropertyAcknowledgementValue the property value should be of type IWritablePropertyAcknowledgementValue as defined in the PayloadSerializer.
                        // We'll convert the json object to NewtonsoftJsonWritablePropertyAcknowledgementValue and then convert it to the appropriate IWritablePropertyAcknowledgementValue object.
                        propertyValue = (T)Convention.PayloadSerializer.CreateWritablePropertyAcknowledgementPayload(
                            newtonsoftWritablePropertyAcknowledgementValue.Value,
                            newtonsoftWritablePropertyAcknowledgementValue.AckCode,
                            newtonsoftWritablePropertyAcknowledgementValue.AckVersion,
                            newtonsoftWritablePropertyAcknowledgementValue.AckDescription);
                        return true;
                    }

                    object writablePropertyValue = newtonsoftWritablePropertyAcknowledgementValue.Value;

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
                propertyValue = Convention.PayloadSerializer.ConvertFromJsonObject<T>(retrievedPropertyValue);
                return true;
            }
            catch
            {
                // In case the value cannot be converted using the serializer,
                // then return false with the default value of the type <T> passed in.
            }

            return false;
        }
    }
}
