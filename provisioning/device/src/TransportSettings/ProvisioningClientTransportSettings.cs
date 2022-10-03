// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Base class used to define various transport-specific settings for IoT hub device and module clients.
    /// </summary>
    public abstract class ProvisioningClientTransportSettings
    {
        /// <summary>
        /// The configured transport protocol.
        /// </summary>
        public ProvisioningClientTransportProtocol Protocol { get; protected set; }

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
        /// ProvisioningDeviceClient GetProvisioningDeviceClient()
        /// {
        ///     var proxy = new WebProxy("localhost", "8888");
        ///     var mqttSettings = new ProvisioningClientMqttSettings()
        ///     {
        ///         Proxy = proxy
        ///     };
        ///
        ///     var optionsWithProxy = new ProvisioningClientOptions(mqttSettings);
        ///
        ///     return new ProvisioningDeviceClient(
        ///         "global endpoint",
        ///         "your id scope",
        ///         securityProvider,
        ///         optionsWithProxy);
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
        /// A callback for remote certificate validation.
        /// </summary>
        /// <remarks>
        /// If incorrectly implemented, your device may fail to connect to IoT hub and/or be open to security vulnerabilities.
        /// <para>
        /// This feature is only applicable for HTTP, MQTT over TCP, MQTT over web socket, AMQP
        /// over TCP. AMQP web socket communication does not support this feature.
        /// </para>
        /// </remarks>
        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; } = DefaultRemoteCertificateValidation;

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

        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString()
        {
            return $"{GetType().Name}/{Protocol}";
        }
    }
}
