// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Contains HTTP transport-specific settings for the device and module clients.
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
    }
}
