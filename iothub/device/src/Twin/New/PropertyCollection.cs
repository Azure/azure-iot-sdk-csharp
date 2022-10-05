﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    ///
    /// </summary>
    public abstract class PropertyCollection : IEnumerable<KeyValuePair<string, object>>
    {
        internal PropertyCollection(Dictionary<string, object> properties)
        {
            foreach (KeyValuePair<string, object> property in properties)
            {
                _properties.Add(property.Key, property.Value);
            }
        }

        /// <summary>
        ///
        /// </summary>
        protected readonly Dictionary<string, object> _properties = new();

        internal PayloadConvention PayloadConvention { get; set; }

        /// <summary>
        /// Gets the value associated with the <paramref name="propertyKey"/> in the reported property collection.
        /// </summary>
        /// <typeparam name="T">The type to cast the object to.</typeparam>
        /// <param name="propertyKey">The key of the property to get.</param>
        /// <param name="propertyValue">When this method returns true, this contains the value of the object from the collection.
        /// When this method returns false, this contains the default value of the type <c>T</c> passed in.</param>
        /// <returns>True if a value of type <c>T</c> with the specified key was found; otherwise, it returns false.</returns>
        public bool TryGetValue<T>(string propertyKey, out T propertyValue)
        {
            propertyValue = default;

            if (_properties.ContainsKey(propertyKey))
            {
                object retrievedPropertyValue = _properties[propertyKey];

                // Case 1:
                // If the object is of type T or can be cast to type T, go ahead and return it.
                if (ObjectConversionHelper.TryCast(retrievedPropertyValue, out propertyValue))
                {
                    return true;
                }

                try
                {
                    // Case 2:
                    // If the value cannot be cast to <T> directly, we need to try to convert it using the serializer.
                    // If it can be successfully converted, go ahead and return it.
                    propertyValue = PayloadConvention.PayloadSerializer.ConvertFromJsonObject<T>(retrievedPropertyValue);
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

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (KeyValuePair<string, object> property in _properties)
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
