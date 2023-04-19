﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System;

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
