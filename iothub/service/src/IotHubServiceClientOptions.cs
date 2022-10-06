// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The configurable options for <see cref="IotHubServiceClient"/> instances.
    /// </summary>
    public class IotHubServiceClientOptions
    {
        /// <summary>
        /// The web proxy that will be used to connect to IoT hub when using the HTTP protocol.
        /// </summary>
        /// <remarks>
        /// If you wish to bypass OS-specified proxy settings, set this to <see cref="GlobalProxySelection.GetEmptyWebProxy()"/>.
        /// </remarks>
        /// <seealso href="https://docs.microsoft.com/dotnet/api/system.net.http.httpclienthandler.proxy?view=net-6.0"/>
        /// <example>
        /// To set a proxy you must instantiate an instance of the <see cref="WebProxy"/> class--or any class that derives from <see cref="IWebProxy"/>.
        /// The snippet below shows a method that returns a device using a proxy that connects to localhost on port 8888.
        /// <c>
        /// IotHubServiceClient GetServiceClient()
        /// {
        ///     var proxy = new WebProxy("localhost", "8888");
        ///     var options = new IotHubServiceClientOptions
        ///     {
        ///         Protocol = IotHubTransportProtocol.WebSocket,
        ///         // Specify the WebProxy to be used for the HTTP and web socket connections.
        ///         Proxy = proxy,
        ///         // Using the default HttpClient here, so the proxy for HTTP operations will be set for me.
        ///     };
        ///     return new IotHubServiceClient("a connection string", options);
        /// }
        /// </c>
        /// </example>
        public IWebProxy Proxy { get; set; }

        /// <summary>
        /// The HTTP client to use for all HTTP operations.
        /// </summary>
        /// <remarks>
        /// If not provided, an HTTP client will be created for you based on the other settings provided.
        /// <para>
        /// If provided, all other HTTP-specific settings (that is <see cref="Proxy"/>, <see cref="SslProtocols"/>, and <see cref="CertificateRevocationCheck"/>)
        /// on this class will be ignored and must be specified on the custom HttpClient instance.
        /// </para>
        /// </remarks>
        public HttpClient HttpClient { get; set; }

        /// <summary>
        /// The configured transport protocol.
        /// </summary>
        /// <remarks>
        /// Only used for communications over AMQP, used in <see cref="MessagesClient"/>, <see cref="MessageFeedbackProcessorClient"/>,
        /// and <see cref="FileUploadNotificationProcessorClient"/>.
        /// </remarks>
        public IotHubTransportProtocol Protocol { get; set; } = IotHubTransportProtocol.Tcp;

        /// <summary>
        /// The version of TLS to use by default.
        /// </summary>
        /// <remarks>
        /// Defaults to "None", which means let the OS decide the proper TLS version (SChannel in Windows / OpenSSL in Linux).
        /// </remarks>
        public SslProtocols SslProtocols { get; set; } = SslProtocols.None;

        /// <summary>
        /// To enable certificate revocation check.
        /// </summary>
        /// <remarks>
        /// Defaults to false.
        /// </remarks>
        public bool CertificateRevocationCheck { get; set; }

        /// <summary>
        /// The configuration for setting <see cref="Message.MessageId"/> for every message sent by the service client instance.
        /// </summary>
        /// <remarks>
        /// The default behavior is that <see cref="Message.MessageId"/> is set only by the user.
        /// </remarks>
        public SdkAssignsMessageId SdkAssignsMessageId { get; set; } = SdkAssignsMessageId.Never;

        /// <summary>
        /// Specify client-side heartbeat interval.
        /// The interval, that the client establishes with the service, for sending keep alive pings.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default value is 2 minutes.
        /// </para>
        /// <para>
        /// Only used for AMQP. Can only be used for <see cref="MessagesClient"/>, <see cref="MessageFeedbackProcessorClient"/> and <see cref="FileUploadNotificationProcessorClient"/>.
        /// The client will consider the connection as disconnected if the keep alive ping fails.
        /// Setting a very low idle timeout value can cause aggressive reconnects, and might not give the
        /// client enough time to establish a connection before disconnecting and reconnecting.
        /// </para>
        /// </remarks>
        public TimeSpan AmqpConnectionKeepAlive { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// A callback for remote certificate validation.
        /// </summary>
        /// <remarks>
        /// If incorrectly implemented, your device may fail to connect to IoT hub and/or be open to security vulnerabilities.
        /// <para>
        /// This feature is only applicable for HTTP and AMQP over TCP. AMQP web socket communication
        /// does not support this feature. For users who want this support over AMQP websocket, you
        /// must instead provide a <see cref="ClientWebSocket"/> instance with the desired callback
        /// and other websocket options (e.g. proxy, keep-alive etc.) set.
        /// </para>
        /// </remarks>
        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; } = DefaultRemoteCertificateValidation;

        /// <summary>
        /// A keep-alive for the transport layer in sending ping/pong control frames when using web sockets.
        /// </summary>
        /// <remarks>
        /// Only used for communications over AMQP, used in <see cref="MessagesClient"/>, <see cref="MessageFeedbackProcessorClient"/>,
        /// and <see cref="FileUploadNotificationProcessorClient"/>.
        /// </remarks>
        /// <seealso href="https://docs.microsoft.com/dotnet/api/system.net.websockets.clientwebsocketoptions.keepaliveinterval"/>
        public TimeSpan? WebSocketKeepAlive { get; set; }

        /// <summary>
        /// An instance of client web socket to be used when transport protocol is set to web socket.
        /// </summary>
        /// <remarks>
        /// If not provided, an instance will be created from provided websocket options (e.g. proxy, keep-alive etc.)
        /// <para>
        /// Only used for communications over AMQP, used in <see cref="MessagesClient"/>, <see cref="MessageFeedbackProcessorClient"/>,
        /// and <see cref="FileUploadNotificationProcessorClient"/>.
        /// </para>
        /// <para>
        /// By default, this client web socket will be disposed when you dispose the service client.
        /// However, you can opt out of this behavior by setting the <see cref="DisposeClientWebSocket"/> flag.
        /// </para>
        /// </remarks>
        public ClientWebSocket ClientWebSocket { get; set; }

        /// <summary>
        /// If true, the provided <see cref="ClientWebSocket"/> will be disposed for you when you
        /// dispose this client. If false, the provided <see cref="ClientWebSocket"/> will not be disposed
        /// when you dispose this client.
        /// </summary>
        /// <remarks>
        /// By default, this is true.
        /// </remarks>
        public bool DisposeClientWebSocket { get; set; } = true;

        internal IotHubServiceClientOptions Clone()
        {
            return new IotHubServiceClientOptions
            {
                Proxy = Proxy,
                HttpClient = HttpClient,
                Protocol = Protocol,
                SslProtocols = SslProtocols,
                CertificateRevocationCheck = CertificateRevocationCheck,
                SdkAssignsMessageId = SdkAssignsMessageId,
                AmqpConnectionKeepAlive = AmqpConnectionKeepAlive,
                RemoteCertificateValidationCallback = RemoteCertificateValidationCallback,
                WebSocketKeepAlive = WebSocketKeepAlive,
                ClientWebSocket = ClientWebSocket,
                DisposeClientWebSocket = DisposeClientWebSocket,
            };
        }

        // The default remote certificate validation callback. It returns false if any SSL level exceptions occurred
        // during the handshake.
        private static bool DefaultRemoteCertificateValidation(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return sslPolicyErrors == SslPolicyErrors.None;
        }
    }
}
