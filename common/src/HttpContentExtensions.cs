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
#if NET5_0_OR_GREATER
            await content.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
#else
            // .NET implementations < .NET 5.0 do not support CancellationTokens for HttpContent APIs, so we will discard it.
            _ = cancellationToken;

            await content.CopyToAsync(stream).ConfigureAwait(false);
#endif
        }

        internal static Task<Stream> ReadHttpContentAsStream(this HttpContent httpContent, CancellationToken cancellationToken)
        {
#if NET5_0_OR_GREATER
            return httpContent.ReadAsStreamAsync(cancellationToken);
#else
            // .NET implementations < .NET 5.0 do not support CancellationTokens for HttpContent APIs, so we will discard it.
            _ = cancellationToken;

            return httpContent.ReadAsStreamAsync();
#endif
        }

        internal static Task<byte[]> ReadHttpContentAsByteArrayAsync(this HttpContent content, CancellationToken cancellationToken)
        {
#if NET5_0_OR_GREATER
            return content.ReadAsByteArrayAsync(cancellationToken);
#else
            // .NET implementations < .NET 5.0 do not support CancellationTokens for HttpContent APIs, so we will discard it.
            _ = cancellationToken;

            return content.ReadAsByteArrayAsync();
#endif
        }

        internal static Task<string> ReadHttpContentAsStringAsync(this HttpContent content, CancellationToken cancellationToken)
        {
#if NET5_0_OR_GREATER
            return content.ReadAsStringAsync(cancellationToken);
#else
            // .NET implementations < .NET 5.0 do not support CancellationTokens for HttpContent APIs, so we will discard it.
            _ = cancellationToken;

            return content.ReadAsStringAsync();
#endif
        }

    }
}