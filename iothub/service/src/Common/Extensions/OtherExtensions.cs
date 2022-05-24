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
        /// Gets the first value associated with the supplied header name.
        /// </summary>
        /// <param name="headers">The collection of HTTP headers and their values.</param>
        /// <param name="name">The header name whose value to get.</param>
        /// <returns>The first value corresponding to the supplied header name, if the name is found in the collection; otherwise, an empty string.</returns>
        internal static string GetFirstValueOrNull(this HttpHeaders headers, string name)
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
        internal static IEnumerable<string> GetValuesOrNull(this HttpHeaders headers, string name)
        {
            headers.TryGetValues(name, out IEnumerable<string> values);
            return values;
        }
    }
}
