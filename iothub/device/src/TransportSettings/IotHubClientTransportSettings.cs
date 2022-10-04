// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Net;
using System.Security.Authentication;

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
        public IotHubClientTransportProtocol Protocol { get; protected set; }

        /// <summary>
        /// The web proxy that will be used to connect to IoT hub using a web socket connection for AMQP, MQTT, or when using the
        /// HTTP protocol.
        /// </summary>
        /// <remarks>
        /// If you wish to bypass OS-specified proxy settings, set this to <see cref="GlobalProxySelection.GetEmptyWebProxy()"/>.
        /// </remarks>
        /// <seealso href="https://docs.microsoft.com/dotnet/api/system.net.http.httpclienthandler.proxy?view=net-6.0"/>
        /// <example>
        /// To set a proxy you must instantiate an instance of the <see cref="WebProxy"/> class--or any class that derives from <see cref="IWebProxy"/>.
        /// The snippet below shows a method that returns a device using a proxy that connects to localhost on port 8888.
        /// <c>
        /// IotHubDeviceClient GetDeviceClient()
        /// {
        ///     var proxy = new WebProxy("localhost", "8888");
        ///     var mqttSettings = new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket)
        ///     {
        ///         // Specify the WebProxy to be used for the connection
        ///         Proxy = proxy,
        ///     };
        ///     var fileUploadSettings = new IotHubClientHttpSettings
        ///     {
        ///         // Also configure the proxy for file uploads.
        ///         Proxy = proxy,
        ///     };
        ///     var options = new IotHubClientOptions(mqttSettings)
        ///     {
        ///         FileUploadTransportSettings = fileUploadSettings,
        ///     };
        ///     return new IotHubDeviceClient("a connection string", options);
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

        internal abstract IotHubClientTransportSettings Clone();
    }
}
