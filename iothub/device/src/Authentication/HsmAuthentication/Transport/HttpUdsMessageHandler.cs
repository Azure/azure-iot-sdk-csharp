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
            await stream.WriteToStreamAsync(requestBytes, cancellationToken).ConfigureAwait(false);

            if (request.Content != null)
            {
                await request.Content.CopyToStreamAsync(stream, cancellationToken).ConfigureAwait(false);
            }

            HttpResponseMessage response = await HttpRequestResponseSerializer.DeserializeResponseAsync(stream, cancellationToken).ConfigureAwait(false);

            return response;
        }

        private async Task<Socket> GetConnectedSocketAsync()
        {
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

            // The Edge Agent uses unix sockets for communication with the modules deployed in docker for HSM.
            // For netstandard 2.0 there was no implementation for a Unix Domain Socket (UDS) so we used a version
            // that was part of a test that was reused in a number of libraries on the internet.
            //
            // https://github.com/dotnet/corefx/blob/12b51c6bf153cc237b251a4e264d5e7c0ee84a33/src/System.IO.Pipes/src/System/Net/Sockets/UnixDomainSocketEndPoint.cs
            // https://github.com/dotnet/corefx/blob/12b51c6bf153cc237b251a4e264d5e7c0ee84a33/src/System.Net.Sockets/tests/FunctionalTests/UnixDomainSocketTest.cs#L248
            //
            // Since then the UnixDomainSocketEndpoint has been added to the dotnet framework and there has been considerable work
            // around unix sockets in the BCL. For older versions of the framework we will continue to use the existing class since it works
            // fine. For netcore 2.1 and greater as well as .NET 5.0 and greater we'll use the native framework version.

#if NETSTANDARD2_0
            var endpoint = new Microsoft.Azure.Devices.Client.HsmAuthentication.Transport.UnixDomainSocketEndPoint(_providerUri.LocalPath);
#else
            var endpoint = new System.Net.Sockets.UnixDomainSocketEndPoint(_providerUri.LocalPath);
#endif
            await socket.ConnectAsync(endpoint).ConfigureAwait(false);
            return socket;
        }
    }
}
