// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp.Transport;
using Microsoft.Azure.Devices.Common;

namespace Microsoft.Azure.Devices
{
    // http://tools.ietf.org/html/rfc6455
    internal class IotHubClientWebSocket
    {
        private const string HttpGetHeaderFormat = "GET {0} HTTP/1.1\r\n";
        private const string EndOfLineSuffix = "\r\n";
        private const byte FIN = 0x80;
        private const byte RSV = 0x00;
        private const byte Mask = 0x80;
        private const byte PayloadMask = 0x7F;
        private const byte Continuation = 0x00;
        private const byte Text = 0x01;
        private const byte Binary = 0x02;
        private const byte Close = 0x08;
        private const byte Ping = 0x09;
        private const byte Pong = 0x0A;
        private const byte MediumSizeFrame = 126;
        private const byte LargeSizeFrame = 127;

        private const string HostHeaderPrefix = "Host: ";
        private const string Separator = ": ";
        private const string Upgrade = "Upgrade";
        private const string Websocket = "websocket";
        private const string ConnectionHeaderName = "Connection";
        private const string FramingPrematureEOF = "More data was expected, but EOF was reached.";
        private const string ClientWebSocketNotInOpenStateDuringReceive = "IotHubClientWebSocket not in Open State during Receive.";
        private const string ClientWebSocketNotInOpenStateDuringSend = "IotHubClientWebSocket not in Open State during Send.";
        private const string ServerRejectedUpgradeRequest = "The server rejected the upgrade request.";
        private const string UpgradeProtocolNotSupported = "Protocol Type {0} was sent to a service that does not support that type of upgrade.";
        private const string SizeExceedsRemainingBufferSpace = "The specified size exceeds the remaining buffer space bytes.";

        private static readonly byte[] s_maskingKey = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        private static readonly SHA1 s_sha1CryptoServiceProvider = InitCryptoServiceProvider();

        private readonly string _webSocketRole;
        private readonly string _requestPath;
        private string _webSocketKey;
        private string _host;

        private static class Headers
        {
            public const string SecWebSocketAccept = "Sec-WebSocket-Accept";
            public const string SecWebSocketProtocol = "Sec-WebSocket-Protocol";
            public const string SecWebSocketKey = "Sec-WebSocket-Key";
            public const string SecWebSocketVersion = "Sec-WebSocket-Version";
        }

        public IotHubClientWebSocket(string webSocketRole)
            : this(webSocketRole, WebSocketConstants.UriSuffix)
        {
        }

        public IotHubClientWebSocket(string webSocketRole, string requestPath)
        {
            State = WebSocketState.Initial;
            _webSocketRole = webSocketRole;
            _requestPath = requestPath;
        }

        public enum WebSocketMessageType
        {
            Binary,
            Close,
            Text,
        }

        public enum WebSocketState
        {
            Initial,
            Connecting,
            Open,
            Closed,
            Aborted,
            Faulted,
        }

        public EndPoint LocalEndpoint => TcpClient?.Client?.LocalEndPoint;

        public EndPoint RemoteEndpoint => TcpClient?.Client?.RemoteEndPoint;

        internal WebSocketState State { get; private set; }

        private TcpClient TcpClient { get; set; }

        private Stream WebSocketStream { get; set; }

        public void Abort()
        {
            if (State == WebSocketState.Aborted || State == WebSocketState.Closed || State == WebSocketState.Faulted)
            {
                return;
            }

            State = WebSocketState.Aborted;
            try
            {
                CleanUp();
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }
            }
        }

        public async Task ConnectAsync(string host, int port, string scheme, TimeSpan timeout)
        {
            _host = host;
            bool succeeded = false;
            try
            {
                // Connect without proxy
                TcpClient = new TcpClient();
                await TcpClient.ConnectAsync(host, port).ConfigureAwait(false);

                if (string.Equals(WebSocketConstants.Scheme, scheme, StringComparison.OrdinalIgnoreCase))
                {
                    // In the real world, web-socket will happen over HTTPS
                    var sslStream = new SslStream(TcpClient.GetStream(), false, IotHubConnection.OnRemoteCertificateValidation);
                    var x509CertificateCollection = new X509Certificate2Collection();
                    await sslStream
                        .AuthenticateAsClientAsync(
                            host,
                            x509CertificateCollection,
                            TlsVersions.Instance.Preferred,
                            checkCertificateRevocation: false)
                        .ConfigureAwait(false);
                    WebSocketStream = sslStream;
                }
                else
                {
                    WebSocketStream = TcpClient.GetStream();
                }

                string upgradeRequest = BuildUpgradeRequest();
                byte[] upgradeRequestBytes = Encoding.ASCII.GetBytes(upgradeRequest);

                TcpClient.Client.SendTimeout = GetSocketTimeoutInMilliSeconds(timeout);

                // Send WebSocket Upgrade request
                await WriteToStreamAsync(WebSocketStream, upgradeRequestBytes).ConfigureAwait(false);

                // receive WebSocket Upgrade response
                byte[] responseBuffer = new byte[8 * 1024];

                var upgradeResponse = new HttpResponse(TcpClient, WebSocketStream, responseBuffer);

                await upgradeResponse.ReadAsync(timeout).ConfigureAwait(false);

                if (upgradeResponse.StatusCode != HttpStatusCode.SwitchingProtocols)
                {
                    // the HTTP response code was not 101
                    if (TcpClient.Connected)
                    {
                        WebSocketStream.Close();
                        TcpClient.Close();
                    }

                    throw new IOException(ServerRejectedUpgradeRequest + " " + upgradeResponse);
                }

                if (!VerifyWebSocketUpgradeResponse(upgradeResponse.Headers))
                {
                    if (TcpClient.Connected)
                    {
                        WebSocketStream.Close();
                        TcpClient.Close();
                    }

                    throw new IOException(UpgradeProtocolNotSupported.FormatInvariant(WebSocketConstants.SubProtocols.Amqpwsb10));
                }

                State = WebSocketState.Open;
                succeeded = true;
            }
            finally
            {
                if (!succeeded)
                {
                    Abort();
                }
            }
        }

        public async Task<int> ReceiveAsync(byte[] buffer, int offset, TimeSpan timeout)
        {
            byte[] header = new byte[2];

            Fx.AssertAndThrow(State == WebSocketState.Open, ClientWebSocketNotInOpenStateDuringReceive);
            TcpClient.ReceiveTimeout = (int)timeout.TotalMilliseconds;

            bool succeeded = false;
            try
            {
                byte payloadLength;
                bool pongFrame;

                // TODO: rewrite this section to handle all control frames (including ping)
                int totalBytesRead;
                int bytesRead;
                do
                {
                    // Ignore pong frame and start over
                    totalBytesRead = 0;
                    totalBytesRead = await ReadFromStreamAsync(WebSocketStream, header).ConfigureAwait(false);

                    if (totalBytesRead == 0)
                    {
                        throw new IOException(FramingPrematureEOF, new InvalidDataException("IotHubClientWebSocket was expecting more bytes"));
                    }

                    if (!ParseWebSocketFrameHeader(header, out payloadLength, out pongFrame))
                    {
                        // Encountered a close frame or error in parsing frame from server. Close connection
                        byte[] closeHeader = PrepareWebSocketHeader(0, WebSocketMessageType.Close);

                        await WriteToStreamAsync(WebSocketStream, closeHeader).ConfigureAwait(false);

                        State = WebSocketState.Closed;
                        WebSocketStream.Close();
                        TcpClient.Close();
                        return 0; // TODO: throw exception?
                    }

                    if (pongFrame && payloadLength > 0)
                    {
                        totalBytesRead = 0;
                        byte[] tempBuffer = new byte[payloadLength];
                        while (totalBytesRead < payloadLength)
                        {
                            bytesRead = await ReadFromStreamAsync(WebSocketStream, tempBuffer, totalBytesRead, payloadLength - totalBytesRead).ConfigureAwait(false);
                            if (bytesRead == 0)
                            {
                                throw new IOException(FramingPrematureEOF, new InvalidDataException("IotHubClientWebSocket was expecting more bytes"));
                            }

                            totalBytesRead += bytesRead;
                        }
                    }
                }
                while (pongFrame);

                totalBytesRead = 0;

                if (buffer.Length < payloadLength)
                {
                    throw Fx.Exception.AsError(new InvalidOperationException(SizeExceedsRemainingBufferSpace));
                }

                if (payloadLength < MediumSizeFrame)
                {
                    while (totalBytesRead < payloadLength)
                    {
                        bytesRead = await ReadFromStreamAsync(WebSocketStream, buffer, offset + totalBytesRead, payloadLength - totalBytesRead).ConfigureAwait(false);

                        if (bytesRead == 0)
                        {
                            throw new IOException(FramingPrematureEOF, new InvalidDataException("IotHubClientWebSocket was expecting more bytes"));
                        }

                        totalBytesRead += bytesRead;
                    }
                }
                else
                {
                    switch (payloadLength)
                    {
                        case MediumSizeFrame:
                            // read payload length (< 64K)
                            do
                            {
                                bytesRead = await ReadFromStreamAsync(WebSocketStream, header, totalBytesRead, header.Length - totalBytesRead).ConfigureAwait(false);

                                if (bytesRead == 0)
                                {
                                    throw new IOException(FramingPrematureEOF, new InvalidDataException("IotHubClientWebSocket was expecting more bytes"));
                                }

                                totalBytesRead += bytesRead;
                            }
                            while (totalBytesRead < header.Length);

                            totalBytesRead = 0;
                            ushort extendedPayloadLength = (ushort)((header[0] << 8) | header[1]);

                            // read payload
                            if (buffer.Length >= extendedPayloadLength)
                            {
                                while (totalBytesRead < extendedPayloadLength)
                                {
                                    bytesRead = await ReadFromStreamAsync(WebSocketStream, buffer, offset + totalBytesRead, extendedPayloadLength - totalBytesRead).ConfigureAwait(false);

                                    if (bytesRead == 0)
                                    {
                                        throw new IOException(FramingPrematureEOF, new InvalidDataException("IotHubClientWebSocket was expecting more bytes"));
                                    }

                                    totalBytesRead += bytesRead;
                                }
                            }
                            else
                            {
                                throw Fx.Exception.AsError(new InvalidOperationException(SizeExceedsRemainingBufferSpace));
                            }

                            break;

                        case LargeSizeFrame:
                            // read payload length (>= 64K)
                            byte[] payloadLengthBuffer = new byte[8];
                            do
                            {
                                bytesRead = await ReadFromStreamAsync(WebSocketStream, payloadLengthBuffer, totalBytesRead, payloadLengthBuffer.Length - totalBytesRead).ConfigureAwait(false);

                                if (bytesRead == 0)
                                {
                                    throw new IOException(FramingPrematureEOF, new InvalidDataException("IotHubClientWebSocket was expecting more bytes"));
                                }

                                totalBytesRead += bytesRead;
                            }
                            while (totalBytesRead < payloadLengthBuffer.Length);

                            totalBytesRead = 0;

                            // ignore bytes 0-3 - length cannot be larger than a 32-bit number
                            uint superExtendedPayloadLength = (uint)((payloadLengthBuffer[4] << 24) | (payloadLengthBuffer[5] << 16) | (payloadLengthBuffer[6] << 8) | payloadLengthBuffer[7]);

                            // read payload
                            if (buffer.Length >= superExtendedPayloadLength)
                            {
                                while (totalBytesRead < superExtendedPayloadLength)
                                {
                                    bytesRead = await ReadFromStreamAsync(WebSocketStream, buffer, offset + totalBytesRead, (int)(superExtendedPayloadLength - totalBytesRead)).ConfigureAwait(false);

                                    if (bytesRead == 0)
                                    {
                                        throw new IOException(FramingPrematureEOF, new InvalidDataException("IotHubClientWebSocket was expecting more bytes"));
                                    }

                                    totalBytesRead += bytesRead;
                                }
                            }
                            else
                            {
                                throw Fx.Exception.AsError(new InvalidOperationException(SizeExceedsRemainingBufferSpace));
                            }

                            break;
                    }
                }

                succeeded = true;
                return totalBytesRead;
            }
            finally
            {
                if (!succeeded)
                {
                    Fault();
                }
            }
        }

        public async Task SendAsync(byte[] buffer, int offset, int size, WebSocketMessageType webSocketMessageType, TimeSpan timeout)
        {
            Fx.AssertAndThrow(State == WebSocketState.Open, ClientWebSocketNotInOpenStateDuringSend);
            TcpClient.Client.SendTimeout = (int)timeout.TotalMilliseconds;
            bool succeeded = false;
            try
            {
                byte[] webSocketHeader = PrepareWebSocketHeader(size, webSocketMessageType);
                await WebSocketStream.WriteAsync(webSocketHeader, 0, webSocketHeader.Length).ConfigureAwait(false);
                MaskWebSocketData(buffer, offset, size);
                await WebSocketStream.WriteAsync(buffer, offset, size).ConfigureAwait(false);
                succeeded = true;
            }
            finally
            {
                if (!succeeded)
                {
                    Fault();
                }
            }
        }

        public async Task CloseAsync()
        {
            State = WebSocketState.Closed;
            bool succeeded = false;
            try
            {
                if (TcpClient.Connected)
                {
                    byte[] webSocketHeader = PrepareWebSocketHeader(0, WebSocketMessageType.Close);

                    await WriteToStreamAsync(WebSocketStream, webSocketHeader, 0, webSocketHeader.Length).ConfigureAwait(false);

                    WebSocketStream.Close();
                    TcpClient.Close();
                }

                succeeded = true;
            }
            catch (Exception exception)
            {
                if (Fx.IsFatal(exception))
                {
                    throw;
                }
            }
            finally
            {
                if (!succeeded)
                {
                    Fault();
                }
            }
        }

        [SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms", Justification = "SHA-1 Hash mandated by RFC 6455")]
        private static SHA1 InitCryptoServiceProvider()
        {
            return SHA1.Create();
        }

        // Socket.ReceiveTimeout/SendTimeout 0 means infinite/no-timeout. When dealing with cascading timeouts
        // if the remaining time reaches TimeSpan.Zero we don't want to turn off timeouts on the socket, instead
        // we want to use a very small timeout.
        private static int GetSocketTimeoutInMilliSeconds(TimeSpan timeout)
        {
            if (timeout == TimeSpan.MaxValue)
            {
                return Timeout.Infinite;
            }

            if (timeout == TimeSpan.Zero)
            {
                // Socket.ReceiveTimeout/SendTimeout 0 means no timeout which is not what we want.
                // Use a small number instead
                return 1;
            }

            long ticks = Ticks.FromTimeSpan(timeout);
            return ticks / TimeSpan.TicksPerMillisecond > int.MaxValue
                ? int.MaxValue
                : Ticks.ToMilliseconds(ticks);
        }

        private static byte[] PrepareWebSocketHeader(int bufferLength, WebSocketMessageType webSocketMessageType)
        {
            byte[] octet;

            if (bufferLength < MediumSizeFrame)
            {
                // Handle small payloads and control frames
                octet = new byte[6];

                // Octet0
                octet[0] = PrepareOctet0(webSocketMessageType);

                // Octet 1
                octet[1] = (byte)(bufferLength | Mask);

                // Octets 2-5 (Masking Key)
                octet[2] = s_maskingKey[0];
                octet[3] = s_maskingKey[1];
                octet[4] = s_maskingKey[2];
                octet[5] = s_maskingKey[3];
            }
            else if (bufferLength <= ushort.MaxValue)
            {
                // Handle medium payloads
                octet = new byte[8];

                // Octet 0
                octet[0] = PrepareOctet0(webSocketMessageType);

                // Octet 1
                octet[1] = MediumSizeFrame | Mask;

                // Octet 2-3 Payload Length
                octet[2] = (byte)((bufferLength >> 8) & 0x00FF);
                octet[3] = (byte)(bufferLength & 0x00FF);

                // Octets 4-7 (Masking Key)
                octet[4] = s_maskingKey[0];
                octet[5] = s_maskingKey[1];
                octet[6] = s_maskingKey[2];
                octet[7] = s_maskingKey[3];
            }
            else
            {
                // Handle large payloads
                octet = new byte[14];

                // Octet 0
                octet[0] = PrepareOctet0(webSocketMessageType);

                // Octet 1
                octet[1] = LargeSizeFrame | Mask;

                // Octet 2-9 Payload Length

                // ignore anything larger than a 32-bit number
                // octet[2] = octet[3] = octet[4] = octet[5] = 0; These are already set to 0
                octet[6] = (byte)((bufferLength >> 24) & 0x00FF);
                octet[7] = (byte)((bufferLength >> 16) & 0x00FF);
                octet[8] = (byte)((bufferLength >> 8) & 0x00FF);
                octet[9] = (byte)(bufferLength & 0x00FF);

                // Octets 10-13 (Masking Key)
                octet[10] = s_maskingKey[0];
                octet[11] = s_maskingKey[1];
                octet[12] = s_maskingKey[2];
                octet[13] = s_maskingKey[3];
            }

            return octet;
        }

        private static byte PrepareOctet0(WebSocketMessageType webSocketMessageType)
        {
            byte octet0 = FIN | RSV;
            if (webSocketMessageType.Equals(WebSocketMessageType.Binary))
            {
                octet0 |= Binary;
            }
            else if (webSocketMessageType.Equals(WebSocketMessageType.Text))
            {
                octet0 |= Text;
            }
            else
            {
                octet0 |= Close;
            }

            return octet0;
        }

        private static void MaskWebSocketData(byte[] buffer, int offset, int size)
        {
            Utils.ValidateBufferBounds(buffer, offset, size);

            for (int i = 0; i < size; i++)
            {
                buffer[i + offset] ^= s_maskingKey[i % 4];
            }
        }

        private static bool ParseWebSocketFrameHeader(byte[] buffer, out byte payloadLength, out bool pongFrame)
        {
            payloadLength = 0;
            bool finalFragment;
            int fin = buffer[0] & FIN;
            if (fin == FIN)
            {
                // this is the final fragment
                finalFragment = true;
            }
            else
            {
                // TODO add fragmented message support
                throw Fx.Exception.AsError(new NotImplementedException("Client Websocket implementation lacks fragmentation support"));
            }

            // TODO: check RSV?
            int opcode = buffer[0] & 0x0F;

            pongFrame = false;

            switch (opcode)
            {
                case Continuation:
                    {
                        if (finalFragment)
                        {
                            return false; // This is a protocol violation. A final frame cannot also be a continuation frame
                        }

                        break;
                    }

                case Text:
                case Binary:
                    // WebSocket implementation can handle both text and binary messages
                    break;

                case Close:
                    return false;   // Close frame received - We can close the connection
                case Ping:
                    throw Fx.Exception.AsError(new NotImplementedException("Client Websocket implementation lacks ping message support"));

                // break;
                case Pong:
                    pongFrame = true;
                    break;

                default:
                    return false;
            }

            int mask = buffer[1] & Mask;
            if (mask == Mask)
            {
                // This is an error. We received a masked frame from server - Close connection as per RFC 6455
                return false;
            }

            payloadLength = (byte)(buffer[1] & PayloadMask);
            return true;
        }

        private void Fault()
        {
            State = WebSocketState.Faulted;
            CleanUp();
        }

        private void CleanUp()
        {
            if (WebSocketStream != null)
            {
                WebSocketStream.Close();
                WebSocketStream.Dispose();
                WebSocketStream = null;
            }

            if (TcpClient != null)
            {
                TcpClient.Close();
                TcpClient.Dispose();
                TcpClient = null;
            }
        }

        private bool VerifyWebSocketUpgradeResponse(NameValueCollection webSocketHeaders)
        {
            // verify that Upgrade header is present with a value of websocket
            string upgradeHeaderValue;
            if (null == (upgradeHeaderValue = webSocketHeaders.Get(Upgrade)))
            {
                // Server did not respond with an upgrade header
                return false;
            }

            if (!string.Equals(upgradeHeaderValue, Websocket, StringComparison.OrdinalIgnoreCase))
            {
                // Server did not include the string websocket in the upgrade header
                return false;
            }

            // verify connection header is present with a value of Upgrade
            string connectionHeaderValue;
            if (null == (connectionHeaderValue = webSocketHeaders.Get(ConnectionHeaderName)))
            {
                // Server did not respond with an connection header
                return false;
            }

            if (!string.Equals(connectionHeaderValue, Upgrade, StringComparison.OrdinalIgnoreCase))
            {
                // Server did not include the string upgrade in the connection header
                return false;
            }

            // verify that a SecWebSocketAccept header is present with appropriate hash value string
            string secWebSocketAcceptHeaderValue;
            if (null == (secWebSocketAcceptHeaderValue = webSocketHeaders.Get(Headers.SecWebSocketAccept)))
            {
                // Server did not include the SecWebSocketAcceptHeader in the response
                return false;
            }

            if (!ComputeHash(_webSocketKey).Equals(secWebSocketAcceptHeaderValue, StringComparison.Ordinal))
            {
                // Server Hash Value of Client's Nonce was invalid
                return false;
            }

            if (!string.IsNullOrEmpty(_webSocketRole))
            {
                // verify SecWebSocketProtocol contents
                string secWebSocketProtocolHeaderValue;
                if (null != (secWebSocketProtocolHeaderValue = webSocketHeaders.Get(Headers.SecWebSocketProtocol)))
                {
                    // Check SecWebSocketProtocolHeader with requested protocol
                    if (!StringComparer.OrdinalIgnoreCase.Equals(_webSocketRole, secWebSocketProtocolHeaderValue))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private string BuildUpgradeRequest()
        {
            _webSocketKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            var sb = new StringBuilder();

            // GET {0} HTTP/1.1\r\n
            sb.AppendFormat(CultureInfo.InvariantCulture, HttpGetHeaderFormat, _requestPath);

            // Setup Host Header
            sb.Append(HostHeaderPrefix).Append(_host).Append(EndOfLineSuffix);

            // Setup Upgrade Header
            sb.Append(Upgrade).Append(Separator).Append(Websocket).Append(EndOfLineSuffix);

            // Setup Connection Header
            sb.Append(ConnectionHeaderName).Append(Separator).Append(Upgrade).Append(EndOfLineSuffix);

            // Setup SecWebSocketKey Header
            sb.Append(Headers.SecWebSocketKey)
              .Append(Separator)
              .Append(_webSocketKey)
              .Append(EndOfLineSuffix);

            if (!string.IsNullOrEmpty(_webSocketRole))
            {
                // Setup SecWebSocketProtocol Header
                sb.Append(Headers.SecWebSocketProtocol)
                    .Append(Separator)
                    .Append(_webSocketRole)
                    .Append(EndOfLineSuffix);
            }

            // Setup SecWebSocketVersion Header
            sb.Append(Headers.SecWebSocketVersion)
              .Append(Separator)
              .Append(WebSocketConstants.Version)
              .Append(EndOfLineSuffix);

            // Add an extra EndOfLine at the end
            sb.Append(EndOfLineSuffix);

            return sb.ToString();
        }

        private static string ComputeHash(string key)
        {
            const string webSocketGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

            string modifiedString = key + webSocketGuid;
            byte[] modifiedStringBytes = Encoding.ASCII.GetBytes(modifiedString);

            byte[] hashBytes;
            lock (s_sha1CryptoServiceProvider)
            {
                hashBytes = s_sha1CryptoServiceProvider.ComputeHash(modifiedStringBytes);
            }

            return Convert.ToBase64String(hashBytes);
        }

        private class HttpResponse
        {
            private int _bodyStartIndex;

            private int _bytesRead;

            public HttpResponse(TcpClient tcpClient, Stream stream, byte[] buffer)
            {
                TcpClient = tcpClient;
                Stream = stream;
                Buffer = buffer;
            }

            private TcpClient TcpClient { get; set; }

            private Stream Stream { get; set; }

            private byte[] Buffer { get; set; }

            private int TotalBytesRead { get; set; }

            public async Task ReadAsync(TimeSpan timeout)
            {
                do
                {
                    TcpClient.Client.ReceiveTimeout = GetSocketTimeoutInMilliSeconds(timeout);
                    _bytesRead = 0;

                    _bytesRead = await ReadFromStreamAsync(Stream, Buffer, TotalBytesRead, Buffer.Length - TotalBytesRead).ConfigureAwait(false);

                    TotalBytesRead += _bytesRead;
                    if (_bytesRead == 0 || TryParseBuffer())
                    {
                        // exit the do/while loop
                        break;
                    }
                }
                while (true);

                if (TotalBytesRead == 0)
                {
                    var socketException = new SocketException((int)SocketError.ConnectionRefused);
                    throw Fx.Exception.AsWarning(new IOException(socketException.Message, socketException));
                }
            }

            public HttpStatusCode StatusCode { get; private set; }

            public string StatusDescription { get; private set; }

            public WebHeaderCollection Headers { get; private set; }

            public override string ToString()
            {
                // return a string like "407 Proxy Auth Required"
                return (int)StatusCode + " " + StatusDescription;
            }

            /// <summary>
            /// Parse the bytes received so far.
            /// If possible:
            ///    -Parse the Status line
            ///    -Parse the HTTP Headers
            ///    -if HTTP Headers Content-Length is present do we have that much content received?
            /// If all the above succeed then this method returns true, otherwise false (need to receive more data from network stream).
            /// </summary>
            private bool TryParseBuffer()
            {
                if (_bodyStartIndex == 0)
                {
                    int firstSpace = IndexOfAsciiChar(Buffer, 0, TotalBytesRead, ' ');
                    if (firstSpace == -1)
                    {
                        return false;
                    }

                    ////HttpVersion = Encoding.ASCII.GetString(array, arraySegment.Offset, firstSpace - arraySegment.Offset);
                    int secondSpace = IndexOfAsciiChar(Buffer, firstSpace + 1, TotalBytesRead - (firstSpace + 1), ' ');
                    if (secondSpace == -1)
                    {
                        return false;
                    }

                    string statusCodeString = Encoding.ASCII.GetString(Buffer, firstSpace + 1, secondSpace - (firstSpace + 1));
                    StatusCode = (HttpStatusCode)int.Parse(statusCodeString, CultureInfo.InvariantCulture);
                    int endOfLine = IndexOfAsciiChars(Buffer, secondSpace + 1, TotalBytesRead - (secondSpace + 1), '\r', '\n');
                    if (endOfLine == -1)
                    {
                        return false;
                    }

                    StatusDescription = Encoding.ASCII.GetString(Buffer, secondSpace + 1, endOfLine - (secondSpace + 1));

                    // Now parse the headers
                    Headers = new WebHeaderCollection();
                    while (true)
                    {
                        int startCurrentLine = endOfLine + 2;
                        if (startCurrentLine >= TotalBytesRead)
                        {
                            return false;
                        }
                        else if (Buffer[startCurrentLine] == '\r' && Buffer[startCurrentLine + 1] == '\n')
                        {
                            // \r\n\r\n indicates the end of the HTTP headers.
                            _bodyStartIndex = startCurrentLine + 2;
                            break;
                        }

                        int separatorIndex = IndexOfAsciiChars(Buffer, startCurrentLine, TotalBytesRead - startCurrentLine, ':', ' ');
                        if (separatorIndex == -1)
                        {
                            return false;
                        }

                        string headerName = Encoding.ASCII.GetString(Buffer, startCurrentLine, separatorIndex - startCurrentLine);
                        endOfLine = IndexOfAsciiChars(Buffer, separatorIndex + 2, TotalBytesRead - (separatorIndex + 2), '\r', '\n');
                        if (endOfLine == -1)
                        {
                            return false;
                        }

                        string headerValue = Encoding.ASCII.GetString(Buffer, separatorIndex + 2, endOfLine - (separatorIndex + 2));
                        Headers.Add(headerName, headerValue);
                    }
                }

                // check to see if all the body bytes have been received.
                string contentLengthValue = Headers[HttpResponseHeader.ContentLength];
                if (!string.IsNullOrEmpty(contentLengthValue) && contentLengthValue != "0")
                {
                    int contentLength = int.Parse(contentLengthValue, CultureInfo.InvariantCulture);
                    if (contentLength > TotalBytesRead - _bodyStartIndex)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public static int IndexOfAsciiChar(byte[] array, int offset, int count, char asciiChar)
        {
            Fx.Assert(asciiChar <= byte.MaxValue, "asciiChar isn't valid ASCII!");
            Fx.Assert(offset + count <= array.Length, "offset + count > array.Length!");

            for (int i = offset; i < offset + count; i++)
            {
                if (array[i] == asciiChar)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Check if the given buffer contains the 2 specified ASCII characters (in sequence) without having to allocate or convert byte[] into string
        /// </summary>
        public static int IndexOfAsciiChars(byte[] array, int offset, int count, char asciiChar1, char asciiChar2)
        {
            Fx.Assert(asciiChar1 <= byte.MaxValue, "asciiChar1 isn't valid ASCII!");
            Fx.Assert(asciiChar2 <= byte.MaxValue, "asciiChar2 isn't valid ASCII!");
            Fx.Assert(offset + count <= array.Length, "offset + count > array.Length!");

            for (int i = offset; i < offset + count - 1; i++)
            {
                if (array[i] == asciiChar1 && array[i + 1] == asciiChar2)
                {
                    return i;
                }
            }

            return -1;
        }

        private static async Task<int> ReadFromStreamAsync(Stream stream, byte[] buffer)
        {
            return await ReadFromStreamAsync(stream, buffer, 0, buffer.Length).ConfigureAwait(false);
        }

        private static async Task<int> ReadFromStreamAsync(Stream stream, byte[] buffer, int offset, int size)
        {
            return await stream.ReadAsync(buffer, offset, size).ConfigureAwait(false);
        }

        private static async Task WriteToStreamAsync(Stream stream, byte[] buffer)
        {
            await WriteToStreamAsync(stream, buffer, 0, buffer.Length).ConfigureAwait(false);
        }

        private static async Task WriteToStreamAsync(Stream stream, byte[] buffer, int offset, int size)
        {
            await stream.WriteAsync(buffer, offset, size).ConfigureAwait(false);
        }
    }
}
