﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        internal IotHubClientHttpSettings Clone()
        {
            return new IotHubClientHttpSettings()
            {
                Protocol = Protocol,
                Proxy = Proxy,
                SslProtocols = SslProtocols,
                CertificateRevocationCheck = CertificateRevocationCheck,
            };
        }
    }

    
}
