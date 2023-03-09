// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Extension helper methods for HttpHeaders.
    /// </summary>
    internal static class HttpHeadersExtensions
    {
        /// <summary>
        /// Gets the first value associated with the supplied header name.
        /// </summary>
        /// <param name="headers">The collection of HTTP headers and their values.</param>
        /// <param name="name">The header name whose value to get.</param>
        /// <returns>The first value corresponding to the supplied header name, if the name is found in the collection; otherwise, an empty string.</returns>
        internal static string SafeGetValue(this HttpHeaders headers, string name)
        {
            return headers.TryGetValues(name, out IEnumerable<string> values)
                ? values.FirstOrDefault()
                : null;
        }
    }
}
