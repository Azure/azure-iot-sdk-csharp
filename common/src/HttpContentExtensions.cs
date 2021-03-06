// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Shared
{
    internal static class HttpContentExtensions
    {
        internal static async Task CopyToStreamAsync(this HttpContent content, Stream stream, CancellationToken cancellationToken)
        {
#if NET5_0
            await content.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
#else
            _ = cancellationToken;
            await content.CopyToAsync(stream).ConfigureAwait(false);
#endif
        }

        internal static Task<Stream> ReadHttpContentAsStream(this HttpContent httpContent, CancellationToken cancellationToken)
        {
#if NET5_0
            return httpContent.ReadAsStreamAsync(cancellationToken);
#else
            _ = cancellationToken;
            return httpContent.ReadAsStreamAsync();
#endif
        }

        internal static Task<byte[]> ReadHttpContentAsByteArrayAsync(this HttpContent content, CancellationToken cancellationToken)
        {
#if NET5_0
            return content.ReadAsByteArrayAsync(cancellationToken);
#else
            _ = cancellationToken;
            return content.ReadAsByteArrayAsync();
#endif
        }

        internal static Task<string> ReadHttpContentAsStringAsync(this HttpContent content, CancellationToken cancellationToken)
        {
#if NET5_0
            return content.ReadAsStringAsync(cancellationToken);
#else
            _ = cancellationToken;
            return content.ReadAsStringAsync();
#endif
        }

    }
}