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
            Socket socket = await GetConnectedSocketAsync().ConfigureAwait(false);
            HttpBufferedStream stream = new HttpBufferedStream(new NetworkStream(socket, true));

            var serializer = new HttpRequestResponseSerializer();
            byte[] requestBytes = serializer.SerializeRequest(request);
            await stream.WriteAsync(requestBytes, 0, requestBytes.Length, cancellationToken).ConfigureAwait(false);
            if (request.Content != null)
            {
                await request.Content.CopyToAsync(stream).ConfigureAwait(false);
            }

            HttpResponseMessage response = await serializer.DeserializeResponse(stream, cancellationToken).ConfigureAwait(false);

            return response;
        }

        private async Task<Socket> GetConnectedSocketAsync()
        {
            var endpoint = new UnixDomainSocketEndPoint(_providerUri.LocalPath);
            Socket socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            await socket.ConnectAsync(endpoint).ConfigureAwait(false);

            return socket;
        }
    }
}
