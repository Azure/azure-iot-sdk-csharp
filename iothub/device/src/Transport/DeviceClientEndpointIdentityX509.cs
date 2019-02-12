// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.IO;
    using Microsoft.Azure.Devices.Shared;

    /// <summary>
    /// Represents DeviceClientEndpointIdentity which uses X509 authentication and single connection (no multiplexing)
    /// </summary>
    internal class DeviceClientEndpointIdentityX509 : DeviceClientEndpointIdentity
    {
        internal DeviceClientEndpointIdentityX509(IotHubConnectionString iotHubConnectionString, AmqpTransportSettings amqpTransportSettings, ProductInfo productInfo)
            : base (iotHubConnectionString, amqpTransportSettings, productInfo)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(DeviceClientEndpointIdentityX509)}");
        }
    }
}