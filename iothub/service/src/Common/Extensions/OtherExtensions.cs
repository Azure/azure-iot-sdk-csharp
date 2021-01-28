// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;

namespace Microsoft.Azure.Devices.Common.Extensions
{
    /// <summary>
    /// Extension helper methods.
    /// </summary>
    [Obsolete("Not recommended for external use.")]
    public static class OtherExtensions
    {
        /// <summary>
        /// Gets the value associated with specified key.
        /// </summary>
        /// <typeparam name="T">The type of the value parameter.</typeparam>
        /// <param name="dictionary">The dictionary containing the specified key.</param>
        /// <param name="key">The key whose value to get.</param>
        /// <returns>The value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.</returns>
        public static T GetValueOrDefault<T>(this IDictionary<string, object> dictionary, string key)
        {
            return dictionary.TryGetValue(key, out object o) && o is T t
                ? t
                : default;
        }

        /// <summary>
        /// Gets the first value associated with the supplied header name.
        /// </summary>
        /// <param name="headers">The collection of HTTP headers and their values.</param>
        /// <param name="name">The header name whose value to get.</param>
        /// <returns>The first value corresponding to the supplied header name, if the name is found in the collection; otherwise, an empty string.</returns>
        public static string GetFirstValueOrNull(this HttpHeaders headers, string name)
        {
            IEnumerable<string> values = headers.GetValuesOrNull(name) ?? Enumerable.Empty<string>();
            return values.FirstOrDefault();
        }

        /// <summary>
        /// Gets the values associated with the supplied header name.
        /// </summary>
        /// <param name="headers">The collection of HTTP headers and their values.</param>
        /// <param name="name">The header name whose value to get.</param>
        /// <returns>The values corresponding to the supplied header name, if the name is found in the collection; otherwise, null.</returns>
        public static IEnumerable<string> GetValuesOrNull(this HttpHeaders headers, string name)
        {
            headers.TryGetValues(name, out IEnumerable<string> values);
            return values;
        }
    }
}
