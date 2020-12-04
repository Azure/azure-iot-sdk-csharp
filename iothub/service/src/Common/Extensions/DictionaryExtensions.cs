// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Common.Extensions
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
        {
            if (dictionary.TryGetValue(key, out TValue value))
            {
                return value;
            }
            return defaultValue;
        }

        public static TValue GetValueOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueProvider)
        {
            if (valueProvider == null)
            {
                throw new ArgumentNullException(nameof(valueProvider), "The value provider function cannot be null.");
            }
            TValue value;
            if (!dictionary.TryGetValue(key, out value))
            {
                value = valueProvider(key);
                dictionary.Add(key, value);
            }
            return value;
        }

        public static bool TryRemove<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, out TValue value)
        {
            if (dictionary.TryGetValue(key, out value))
            {
                dictionary.Remove(key);
                return true;
            }
            return false;
        }

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
            TValue value;
            if (dictionary.TryGetValue(key, out value))
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
