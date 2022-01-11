// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The base class for all payloads that accept a <see cref="PayloadConvention"/>.
    /// </summary>
    /// <remarks>
    /// This classes uses the <see cref="NewtonsoftJsonPayloadSerializer"/> and
    /// <see cref="Utf8PayloadEncoder"/> based <see cref="DefaultPayloadConvention"/> by default.
    /// </remarks>
    public abstract class PayloadCollection : IEnumerable<KeyValuePair<string, object>>
    {
        /// <summary>
        /// The underlying collection for the payload.
        /// </summary>
        public IDictionary<string, object> Collection { get; } = new Dictionary<string, object>();

        /// <summary>
        /// The convention to use with this payload.
        /// </summary>
        public PayloadConvention Convention { get; internal set; }

        /// <summary>
        /// Gets the value with the specified key.
        /// </summary>
        /// <remarks>
        /// This accessor is best used to access and cast to simple types.
        /// It is recommended to use <see cref="TryGetValue"/> to deserialize to a complex type.
        /// <para>
        /// For setting component-level property values see <see cref="ClientPropertyCollection.AddComponentProperty(string, string, object)"/>
        /// and <see cref="ClientPropertyCollection.AddComponentProperties(string, IDictionary{string, object})"/> instead
        /// as these convenience methods ensure that component-level properties include the component identifier markers: { "__t": "c" }.
        /// For more information see <see href="https://docs.microsoft.com/azure/iot-pnp/concepts-convention#sample-multiple-components-read-only-property"/>.
        /// </para>
        /// </remarks>
        /// <param name="key">Key of value.</param>
        /// <returns>The specified property.</returns>
        public virtual object this[string key]
        {
            get => Collection[key];
            set => AddOrUpdate(key, value);
        }

        /// <summary>
        /// Adds the key-value pair to the collection.
        /// </summary>
        /// <remarks>
        /// For property operations see <see cref="ClientPropertyCollection.AddRootProperty(string, object)"/>
        /// and <see cref="ClientPropertyCollection.AddComponentProperties(string, IDictionary{string, object})"/> instead.
        /// </remarks>
        /// <inheritdoc cref="AddOrUpdate(string, object)" path="/param['key']"/>
        /// <inheritdoc cref="AddOrUpdate(string, object)" path="/param['value']"/>
        /// <inheritdoc cref="AddOrUpdate(string, object)" path="/exception"/>
        /// <exception cref="ArgumentException">An element with the same key already exists in the collection.</exception>
        public virtual void Add(string key, object value)
        {
            Collection.Add(key, value);
        }

        /// <summary>
        /// Adds or updates the key-value pair to the collection.
        /// </summary>
        /// <remarks>
        /// For property operations see <see cref="ClientPropertyCollection.AddOrUpdateRootProperty(string, object)"/>
        /// and <see cref="ClientPropertyCollection.AddOrUpdateComponentProperties(string, IDictionary{string, object})"/> instead.
        /// </remarks>
        /// <param name="key">The name of the key to be added to the collection.</param>
        /// <param name="value">The value to be added to the collection.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public virtual void AddOrUpdate(string key, object value)
        {
            Collection[key] = value;
        }

        /// <summary>
        /// Gets the collection as a byte array.
        /// </summary>
        /// <remarks>
        /// This will get the fully encoded serialized string using both <see cref="PayloadSerializer.SerializeToString(object)"/>.
        /// and <see cref="PayloadEncoder.EncodeStringToByteArray(string)"/> methods implemented in the <see cref="PayloadConvention"/>.
        /// </remarks>
        /// <returns>A fully encoded serialized string.</returns>
        public virtual byte[] GetPayloadObjectBytes()
        {
            return Convention.GetObjectBytes(Collection);
        }

        /// <summary>
        /// Determines whether the specified key is present.
        /// </summary>
        /// <param name="key">The key in the collection to locate.</param>
        /// <returns><c>true</c> if the specified property is present, otherwise <c>false</c>.</returns>
        public bool Contains(string key)
        {
            return Collection.ContainsKey(key);
        }

        /// <summary>
        /// Gets the value of the object from the collection.
        /// </summary>
        /// <typeparam name="T">The type to cast the object to.</typeparam>
        /// <param name="key">The key of the property to get.</param>
        /// <param name="value">When this method returns true, this contains the value of the object from the collection.
        /// When this method returns false, this contains the default value of the type <c>T</c> passed in.</param>
        /// <returns>True if a value of type <c>T</c> with the specified key was found; otherwise, it returns false.</returns>
        public bool TryGetValue<T>(string key, out T value)
        {
            if (Logging.IsEnabled && Convention == null)
            {
                Logging.Info(this, $"The convention for this collection is not set; this typically means this collection was not created by the client. " +
                    $"TryGetValue will attempt to get the property value but may not behave as expected.", nameof(TryGetValue));
            }

            // If the key is null, empty or whitespace, then return false with the default value of the type <T> passed in.
            if (string.IsNullOrWhiteSpace(key))
            {
                value = default;
                return false;
            }

            // While retrieving the telemetry value from the collection, a simple dictionary indexer should work.
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
            if (Collection.ContainsKey(key))
            {
                object retrievedPropertyValue = Collection[key];

                // If the value associated with the key is null, then return true with the default value of the type <T> passed in.
                if (retrievedPropertyValue == null)
                {
                    value = default;
                    return true;
                }

                // Case 1:
                // If the object is of type T or can be cast to type T, go ahead and return it.
                if (ObjectConversionHelpers.TryCast(retrievedPropertyValue, out value))
                {
                    return true;
                }

                // Case 2:
                // Check if the retrieved value is a writable property update request
                if (retrievedPropertyValue is WritableClientProperty writableClientProperty)
                {
                    object writableClientPropertyValue = writableClientProperty.Value;

                    // If the object is of type T or can be cast or converted to type T, go ahead and return it.
                    if (ObjectConversionHelpers.TryCastOrConvert(writableClientPropertyValue, Convention, out value))
                    {
                        return true;
                    }
                }

                try
                {
                    try
                    {
                        // Case 3a:
                        // Check if the retrieved value is a writable property update acknowledgment
                        var newtonsoftWritablePropertyResponse = Convention.PayloadSerializer.ConvertFromObject<NewtonsoftJsonWritablePropertyResponse>(retrievedPropertyValue);

                        if (typeof(IWritablePropertyResponse).IsAssignableFrom(typeof(T)))
                        {
                            // If T is IWritablePropertyResponse the property value should be of type IWritablePropertyResponse as defined in the PayloadSerializer.
                            // We'll convert the json object to NewtonsoftJsonWritablePropertyResponse and then convert it to the appropriate IWritablePropertyResponse object.
                            value = (T)Convention.PayloadSerializer.CreateWritablePropertyResponse(
                                newtonsoftWritablePropertyResponse.Value,
                                newtonsoftWritablePropertyResponse.AckCode,
                                newtonsoftWritablePropertyResponse.AckVersion,
                                newtonsoftWritablePropertyResponse.AckDescription);
                            return true;
                        }

                        var writablePropertyValue = newtonsoftWritablePropertyResponse.Value;

                        // If the object is of type T or can be cast or converted to type T, go ahead and return it.
                        if (ObjectConversionHelpers.TryCastOrConvert(writablePropertyValue, Convention, out value))
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // In case of an exception ignore it and continue.
                    }

                    // Case 3b, 3c:
                    // If the value is neither a writable property nor can be cast to <T> directly, we need to try to convert it using the serializer.
                    // If it can be successfully converted, go ahead and return it.
                    value = Convention.PayloadSerializer.ConvertFromObject<T>(retrievedPropertyValue);
                    return true;
                }
                catch
                {
                    // In case the value cannot be converted using the serializer,
                    // then return false with the default value of the type <T> passed in.
                }
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Returns a serialized string of this collection from the <see cref="PayloadSerializer.SerializeToString(object)"/> method.
        /// </summary>
        /// <returns>A serialized string of this collection.</returns>
        public virtual string GetSerializedString()
        {
            return Convention.PayloadSerializer.SerializeToString(Collection);
        }

        /// <summary>
        /// Remove all items from the collection.
        /// </summary>
        public void ClearCollection()
        {
            Collection.Clear();
        }

        ///  <inheritdoc />
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (KeyValuePair<string, object> property in Collection)
            {
                yield return property;
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
