// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The configurable options for <see cref="IotHubServiceClient"/> instances.
    /// </summary>
    public class IotHubServiceClientOptions
    {
        private static readonly TimeSpan s_defaultIdleTimeout = TimeSpan.FromMinutes(2);

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
        /// static ServiceClient GetServiceClient()
        /// {
        ///     try
        ///     {
        ///         var proxyHost = "localhost";
        ///         var proxyPort = 8888;
        ///         var transportSettings = new HttpTransportSettings
        ///         {
        ///             Proxy = new WebProxy(proxyHost, proxyPort)
        ///         };
        ///         // Specify the WebProxy to be used for the HTTP connection
        ///         var serviceClient = new ServiceClient("a connection string", transportSettings);
        ///         return serviceClient;
        ///     }
        ///     catch (Exception)
        ///     {
        ///         Console.WriteLine("Error creating client.");
        ///         throw;
        ///     }
        /// }
        /// </c>
        /// </example>
        public IWebProxy Proxy { get; set; }

        /// <summary>
        /// The HTTP client to use for all HTTP operations. If provided, all other settings will be ignored. If not provided,
        /// an HTTP client will be created for you based on the other provided settings.
        /// </summary>
        public HttpClient HttpClient { get; set; }

        /// <summary>
        /// The configured transport protocol.
        /// </summary>
        /// <remarks>
        /// Only used for AMQP. Can only be used for <see cref="MessagingClient"/> and <see cref="MessageFeedbackProcessorClient"/> and <see cref="FileUploadNotificationProcessorClient"/>.
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
        /// The default value is 2 minutes.
        /// </summary>
        /// <remarks>
        /// Only used for AMQP. Can only be used for <see cref="MessagingClient"/> and <see cref="MessageFeedbackProcessorClient"/> and <see cref="FileUploadNotificationProcessorClient"/>.
        /// The client will consider the connection as disconnected if the keep alive ping fails.
        /// Setting a very low idle timeout value can cause aggressive reconnects, and might not give the
        /// client enough time to establish a connection before disconnecting and reconnecting.
        /// </remarks>
        public TimeSpan AmqpConnectionKeepAlive { get; set; } = s_defaultIdleTimeout;

        /// <summary>
        /// A keep-alive for the transport layer in sending ping/pong control frames when using web sockets.
        /// </summary>
        /// <remarks>
        /// Only used for AMQP. Can only be used for <see cref="MessagingClient"/> and <see cref="MessageFeedbackProcessorClient"/> and <see cref="FileUploadNotificationProcessorClient"/>.
        /// </remarks>
        /// <seealso href="https://docs.microsoft.com/dotnet/api/system.net.websockets.clientwebsocketoptions.keepaliveinterval"/>
        public TimeSpan? AmqpWebSocketKeepAlive { get; set; }
    }
}
