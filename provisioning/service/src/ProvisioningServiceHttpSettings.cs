// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Security.Authentication;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// contains Http1 transport-specific settings for Service Client
    /// </summary>
    public sealed class ProvisioningServiceHttpSettings
    {
        /// <summary>
        /// Creates an instance of Http1TransportSettings
        /// </summary>
        public ProvisioningServiceHttpSettings()
        {
            Proxy = DefaultWebProxySettings.Instance;
        }

        /// <summary>
        /// Gets or sets proxy information for the request.
        /// </summary>
        public IWebProxy Proxy { get; set; }

        /// <summary>
        /// The version of TLS to use by default.
        /// </summary>
        /// <remarks>
        /// Defaults to "None", which means let the OS decide the proper TLS version (SChannel in Windows / OpenSSL in Linux).
        /// </remarks>
        public SslProtocols SslProtocols { get; set; } = SslProtocols.None;

        /// <summary>
        /// To enable certificate revocation check. Default to be false.
        /// </summary>
        public bool CertificateRevocationCheck { get; set; }
    }
}
