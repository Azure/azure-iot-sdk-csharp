// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Contains HTTP transport-specific settings for a provisioning device client.
    /// </summary>
    public sealed class ProvisioningClientHttpSettings : ProvisioningClientTransportSettings
    {
        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        public ProvisioningClientHttpSettings()
        {
            Protocol = ProvisioningClientTransportProtocol.WebSocket;
        }

        internal ProvisioningClientHttpSettings Clone()
        {
            return new ProvisioningClientHttpSettings()
            {
                Proxy = Proxy,
                SslProtocols = SslProtocols,
                Protocol = Protocol,
            };
        }
    }
}
