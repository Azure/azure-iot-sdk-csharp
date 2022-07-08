// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Extensions added to simplify the usage of <see cref="HttpContent"/> APIs based on the .NET implementation used.
    /// </summary>
    internal static class HttpContentExtensions
    {
        internal static async Task CopyToStreamAsync(this HttpContent content, Stream stream, CancellationToken cancellationToken)
        {
            // .NET implementations < .NET 5.0 do not support CancellationTokens for HttpContent APIs, so we will discard it.
            _ = cancellationToken;

            await content.CopyToAsync(stream).ConfigureAwait(false);
        }

        internal static Task<Stream> ReadHttpContentAsStream(this HttpContent httpContent, CancellationToken cancellationToken)
        {
            // .NET implementations < .NET 5.0 do not support CancellationTokens for HttpContent APIs, so we will discard it.
            _ = cancellationToken;

            return httpContent.ReadAsStreamAsync();
        }

        internal static Task<byte[]> ReadHttpContentAsByteArrayAsync(this HttpContent content, CancellationToken cancellationToken)
        {
            // .NET implementations < .NET 5.0 do not support CancellationTokens for HttpContent APIs, so we will discard it.
            _ = cancellationToken;

            return content.ReadAsByteArrayAsync();
        }

        internal static Task<string> ReadHttpContentAsStringAsync(this HttpContent content, CancellationToken cancellationToken)
        {
            // .NET implementations < .NET 5.0 do not support CancellationTokens for HttpContent APIs, so we will discard it.
            _ = cancellationToken;

            return content.ReadAsStringAsync();
        }

    }
}