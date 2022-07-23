// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Contains HTTP transport-specific settings for the device and module clients.
    /// </summary>
    public sealed class HttpTransportSettings : ITransportSettings
    {
        private static readonly TimeSpan s_defaultOperationTimeout = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        public HttpTransportSettings()
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
    }
}
