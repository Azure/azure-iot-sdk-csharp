// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Contains HTTP transport-specific settings for the device and module clients.
    /// </summary>
    public sealed class Http1TransportSettings : ITransportSettings
    {
        private static readonly TimeSpan s_defaultOperationTimeout = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        public Http1TransportSettings()
        {
            Proxy = DefaultWebProxySettings.Instance;
        }

        /// <summary>
        /// Gets the transport type for this settings class.
        /// </summary>
        /// <returns>HyperText Transfer Protocol transport type. <see cref="TransportType.Http1"/></returns>
        public TransportType GetTransportType()
        {
            return TransportType.Http1;
        }

        /// <summary>
        /// Device certificate used for authentication.
        /// </summary>
        public X509Certificate2 ClientCertificate { get; set; }

        /// <summary>
        /// The time to wait for a receive operation. The default value is 1 minute.
        /// </summary>
        /// <remarks>
        /// This property is currently unused.
        /// </remarks>
        public TimeSpan DefaultReceiveTimeout => s_defaultOperationTimeout;

        /// <inheritdoc/>
        public IWebProxy Proxy { get; set; }

        /// <summary>
        /// The HTTP client to use for all HTTP operations.
        /// </summary>
        /// <remarks>
        /// If not provided, an HTTP client will be created for you based on the other settings provided.
        /// <para>
        /// This HTTP client instance will be disposed when the device/module client using it is disposed.
        /// </para>
        /// <para>
        /// If provided, all other HTTP-specific settings (such as proxy, SSL protocols, and certificate revocation check)
        /// on this class will be ignored and must be specified on this HttpClient instance.
        /// </para>
        /// </remarks>
        public HttpClient HttpClient { get; set; }
    }
}
