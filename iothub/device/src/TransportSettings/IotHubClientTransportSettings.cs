// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Base class used to define various transport-specific settings for IoT hub device and module clients.
    /// </summary>
    public abstract class IotHubClientTransportSettings
    {
        /// <summary>
        /// The configured transport protocol.
        /// </summary>
        public TransportProtocol Protocol { get; protected set; }

        /// <summary>
        /// The client certificate to use for authenticating.
        /// </summary>
        public X509Certificate2 ClientCertificate { get; set; }

        /// <summary>
        /// The time to wait for a receive operation.
        /// </summary>
        public TimeSpan DefaultReceiveTimeout { get; protected set; }

        /// <summary>
        /// The web proxy that will be used to connect to IoT hub using a web socket connection for AMQP, MQTT, or when using the
        /// HTTP protocol.
        /// </summary>
        /// <value>
        /// An instance of a class that implements <see cref="IWebProxy"/>.
        /// </value>
        /// <remarks>
        /// This setting will only be used when the client connects over web sockets or HTTPS.
        /// </remarks>
        /// <example>
        /// To set a proxy you must instantiate an instance of the <see cref="WebProxy"/> class--or any class that derives from
        /// <see cref="IWebProxy"/>. The snippet below shows a method that returns a device using a proxy that connects to localhost
        /// on port 8888.
        /// <c>
        /// static DeviceClient GetClientWithProxy()
        /// {
        ///     try
        ///     {
        ///         var proxyHost = "localhost";
        ///         var proxyPort = 8888;
        ///         // Specify the WebProxy to be used for the web socket connection
        ///         var transportSettings = new AmqpTransportSettings(Microsoft.Azure.Devices.Client.TransportType.Amqp_WebSocket_Only)
        ///         {
        ///             Proxy = new WebProxy(proxyHost, proxyPort)
        ///         };
        ///         return DeviceClient.CreateFromConnectionString("a connection string", new TransportSettings[] { transportSettings });
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

        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString()
        {
            return $"{GetType().Name}/{Protocol}";
        }
    }
}
