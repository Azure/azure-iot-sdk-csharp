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
    internal static class HttpMessageHelper
    {
        internal static async Task WriteToStreamAsync(this Stream stream, byte[] requestBytes, CancellationToken cancellationToken)
        {
#if NET451 || NET472 || NETSTANDARD2_0
            await stream.WriteAsync(requestBytes, 0, requestBytes.Length, cancellationToken).ConfigureAwait(false);
#else
            await stream.WriteAsync(requestBytes, cancellationToken).ConfigureAwait(false);
#endif
        }
    }
}