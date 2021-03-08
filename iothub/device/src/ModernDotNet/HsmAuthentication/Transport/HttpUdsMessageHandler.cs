// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.HsmAuthentication.Transport
{
    internal class HttpUdsMessageHandler : HttpMessageHandler
    {
        private readonly Uri _providerUri;

        public HttpUdsMessageHandler(Uri providerUri)
        {
            _providerUri = providerUri;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // We can dispose both the socket and the stream after we construct the response.
            using Socket socket = await GetConnectedSocketAsync().ConfigureAwait(false);
            using var stream = new HttpBufferedStream(new NetworkStream(socket, true));

            byte[] requestBytes = HttpRequestResponseSerializer.SerializeRequest(request);
#if NET451 || NET472 || NETSTANDARD2_0
            await stream.WriteAsync(requestBytes, 0, requestBytes.Length, cancellationToken).ConfigureAwait(false);
#else
            await stream.WriteAsync(requestBytes, cancellationToken).ConfigureAwait(false);
#endif
            if (request.Content != null)
            {
                await request.Content.CopyToAsync(stream).ConfigureAwait(false);
            }

            HttpResponseMessage response = await HttpRequestResponseSerializer.DeserializeResponseAsync(stream, cancellationToken).ConfigureAwait(false);

            return response;
        }

        private async Task<Socket> GetConnectedSocketAsync()
        {
            Socket socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
#if NET451 || NET472 || NETSTANDARD2_0
            var endpoint = new UnixDomainSocketEndPoint(_providerUri.LocalPath);
            await socket.ConnectAsync(endpoint).ConfigureAwait(false);
#else
            var endpoint = new System.Net.Sockets.UnixDomainSocketEndPoint(_providerUri.LocalPath);
            await socket.ConnectAsync(endpoint).ConfigureAwait(false);
#endif

            return socket;
        }
    }
}
