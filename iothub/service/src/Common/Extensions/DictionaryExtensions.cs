// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Common.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IDictionary{TKey, TValue}"/> class.
    /// </summary>
    [Obsolete("Not recommended for external use.")]
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Gets the value associated with specified key.
        /// </summary>
        /// <typeparam name="TKey">The type of the key parameter.</typeparam>
        /// <typeparam name="TValue">The type of the value parameter.</typeparam>
        /// <param name="dictionary">The dictionary containing the specified key.</param>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="defaultValue">The default value for the type of the value parameter.</param>
        /// <returns>The value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.</returns>
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
        {
            return dictionary.TryGetValue(key, out TValue value)
                ? value
                : defaultValue;
        }

        /// <summary>
        /// Gets or adds the value associated with specified key.
        /// </summary>
        /// <typeparam name="TKey">The type of the key parameter.</typeparam>
        /// <typeparam name="TValue">The type of the value parameter.</typeparam>
        /// <param name="dictionary">The dictionary containing the specified key.</param>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="valueProvider">The value provider function.</param>
        /// <returns>The value associated with the specified key, if the key is found; otherwise, it retrieves the value from the value provider function,
        /// adds it to the dictionary and returns it .</returns>
        public static TValue GetValueOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueProvider)
        {
            if (valueProvider == null)
            {
                throw new ArgumentNullException(nameof(valueProvider), "The value provider function cannot be null.");
            }
            if (!dictionary.TryGetValue(key, out TValue value))
            {
                value = valueProvider(key);
                dictionary.Add(key, value);
            }
            return value;
        }

        /// <summary>
        /// Removes the value associated with specified key.
        /// </summary>
        /// <typeparam name="TKey">The type of the key parameter.</typeparam>
        /// <typeparam name="TValue">The type of the value parameter.</typeparam>
        /// <param name="dictionary">The dictionary containing the specified key.</param>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">The value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.</param>
        /// <returns>True if the dictionary contains an element with the specified key; otherwise, false.</returns>
        public static bool TryRemove<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, out TValue value)
        {
            if (dictionary.TryGetValue(key, out value))
            {
                dictionary.Remove(key);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets or adds the value associated with specified key.
        /// </summary>
        /// <typeparam name="TKey">The type of the key parameter.</typeparam>
        /// <typeparam name="TValue">The type of the value parameter.</typeparam>
        /// <param name="dictionary">The dictionary containing the specified key.</param>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="valueFactory">The value provider function.</param>
        /// <returns>The value associated with the specified key, if the key is found; otherwise the default value for the type of the value parameter.</returns>
        public static TValue GetOrAddNonNull<TKey, TValue>(
            this ConcurrentDictionary<TKey, TValue> dictionary,
            TKey key,
            Func<TKey, TValue> valueFactory)
            where TValue : class
        {
            if (valueFactory == null)
            {
                throw new ArgumentNullException(nameof(valueFactory), "The value factory function cannot be null.");
            }
            if (dictionary.TryGetValue(key, out TValue value))
            {
                return value;
            }
            value = valueFactory(key);
            if (value == null)
            {
                return null;
            }
            return dictionary.GetOrAdd(key, value);
        }
    }
}
