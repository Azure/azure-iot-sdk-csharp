// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Contains HTTP transport-specific settings for the device and module clients.
    /// </summary>
    public sealed class HttpTransportSettings : ITransportSettings
    {
        /// <inheritdoc/>
        public TransportProtocol Protocol { get; } = TransportProtocol.WebSocket;

        /// <inheritdoc/>
        public X509Certificate2 ClientCertificate { get; set; }

        /// <summary>
        /// The time to wait for a receive operation. The default value is 1 minute.
        /// </summary>
        /// <remarks>
        /// This property is currently unused.
        /// </remarks>
        public TimeSpan DefaultReceiveTimeout { get; } = TimeSpan.FromMinutes(1);


        /// <inheritdoc/>
        public IWebProxy Proxy { get; set; } = DefaultWebProxySettings.Instance;

        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString()
        {
            return $"{GetType().Name}/{Protocol}";
        }
    }
}
