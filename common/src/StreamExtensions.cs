// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Extensions added to simplify the usage of <see cref="Stream"/> APIs based on the .NET implementation used.
    /// </summary>
    internal static class StreamExtensions
    {
        internal static async Task WriteToStreamAsync(this Stream stream, byte[] requestBytes, CancellationToken cancellationToken)
        {
#if NETSTANDARD2_0
            await stream.WriteAsync(requestBytes, 0, requestBytes.Length, cancellationToken).ConfigureAwait(false);
#else
            await stream.WriteAsync(requestBytes, cancellationToken).ConfigureAwait(false);
#endif
        }
    }
}