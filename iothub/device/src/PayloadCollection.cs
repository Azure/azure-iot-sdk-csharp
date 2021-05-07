// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The base class for all payloads that accept a <see cref="PayloadConvention"/>
    /// </summary>
    public abstract class PayloadCollection : IEnumerable<object>
    {
        /// <summary>
        /// The underlying collection for the payload.
        /// </summary>
        public IDictionary<string, object> Collection { get; private set; } = new Dictionary<string, object>();

        /// <summary>
        /// The convention to use with this payload.
        /// </summary>
        public PayloadConvention Convention { get; internal set; }

        /// <summary>
        /// Get the value at the specified key
        /// </summary>
        /// <param name="key">Key of value</param>
        /// <remarks>
        /// This accessor is best used to access simple types. It is recommended to use <see cref="TryGetValue"/> to cast a complex type.
        /// </remarks>
        /// <returns>The specified property.</returns>
        public virtual object this[string key]
        {
            get => Collection[key];
            set => AddOrUpdate(key, value);
        }

        /// <summary>
        /// Adds the key-value pair to the collection.
        /// </summary>
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
        /// <param name="key">The name of the telemetry.</param>
        /// <param name="value">The value of the telemetry.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c> </exception>
        public virtual void AddOrUpdate(string key, object value)
        {
            Collection[key] = value;
        }

        /// <summary>
        /// Gets the collection as a byte array
        /// </summary>
        /// <remarks>
        /// This will get the fully encoded serialized string using both <see cref="PayloadSerializer.SerializeToString(object)"/>
        /// and <see cref="PayloadEncoder.EncodeStringToByteArray(string)"/> methods implemented in the <see cref="PayloadConvention"/>.
        /// </remarks>
        /// <returns>A fully encoded serialized string.</returns>
        public virtual byte[] GetPayloadObjectBytes()
        {
            return Convention.GetObjectBytes(Collection);
        }

        /// <summary>
        /// Gets the value of the object from the collection.
        /// </summary>
        /// <remarks>
        /// This class is used for both sending and receiving properties for the device.
        /// </remarks>
        /// <typeparam name="T">The type to cast the object to.</typeparam>
        /// <param name="key">The key of the property to get.</param>
        /// <param name="value">The value of the object from the collection.</param>
        /// <returns>true if the collection contains an element with the specified key; otherwise, false.</returns>
        public virtual bool TryGetValue<T>(string key, out T value)
        {
            if (Collection.ContainsKey(key))
            {
                // If the object is of type T go ahead and return it.
                if (Collection[key] is T valueRef)
                {
                    value = valueRef;
                    return true;
                }
                // If it's not we need to try to convert it using the serializer.
                // JObject or JsonElement
                value = Convention.PayloadSerializer.ConvertFromObject<T>(Collection[key]);
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Returns a serailized string of this collection from the <see cref="PayloadSerializer.SerializeToString(object)"/> method.
        /// </summary>
        /// <returns></returns>
        public virtual string GetSerializedString()
        {
            return Convention.PayloadSerializer.SerializeToString(Collection);
        }

        ///  <inheritdoc />
        public IEnumerator<object> GetEnumerator()
        {
            foreach (object property in Collection)
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
        /// Will set the underlying <see cref="Collection"/> of the payload collection.
        /// </summary>
        /// <param name="payloadCollection">The collection to get the underlying dictionary from</param>
        protected void SetCollection(PayloadCollection payloadCollection)
        {
            if (payloadCollection == null)
            {
                throw new ArgumentNullException();
            }

            Collection = payloadCollection.Collection;
        }
    }
}
