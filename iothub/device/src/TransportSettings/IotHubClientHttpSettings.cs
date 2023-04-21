// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Contains HTTP transport-specific settings for the device and module clients.
    /// </summary>
    public sealed class IotHubClientHttpSettings : IotHubClientTransportSettings
    {
        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        public IotHubClientHttpSettings()
        {
            Protocol = IotHubClientTransportProtocol.WebSocket;
        }

        /// <summary>
        /// The HTTP client to use for all HTTP operations.
        /// </summary>
        /// <remarks>
        /// If not provided, an HTTP client will be created for you based on the other settings provided.
        /// <para>
        /// This HTTP client instance will be disposed when the device/module client using it is disposed.
        /// </para>
        /// <para>
        /// If provided, all other HTTP-specific settings (such as proxy, SSL protocols, and CertificateRevocationCheck)
        /// on this class will be ignored and must be specified on the custom HttpClient instance.
        /// </para>
        /// </remarks>
        public HttpClient HttpClient { get; set; }

        /// <summary>
        /// Gets or sets a callback method to validate the server certificate.
        /// </summary>
        /// <remarks>
        /// This is used only for setting the certificate validator for module clients.
        /// </remarks>
        internal Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback { get; set; }

        internal override IotHubClientTransportSettings Clone()
        {
            return new IotHubClientHttpSettings()
            {
                Protocol = Protocol,
                Proxy = Proxy,
                SslProtocols = SslProtocols,
                CertificateRevocationCheck = CertificateRevocationCheck,
                ServerCertificateCustomValidationCallback = ServerCertificateCustomValidationCallback,
            };
        }
    }

    
}
