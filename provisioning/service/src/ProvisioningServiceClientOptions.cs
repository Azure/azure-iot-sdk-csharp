// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Options that allow configuration of the provisioning service client instance during initialization.
    /// </summary>
    public class ProvisioningServiceClientOptions
    {
        /// <summary>
        /// Gets or sets proxy information for the client to use.
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
        /// To enable certificate revocation check.
        /// </summary>
        /// <remarks>
        /// Defaults to false.
        /// </remarks>
        public bool CertificateRevocationCheck { get; set; }

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
    }
}
