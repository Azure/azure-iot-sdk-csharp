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
    /// Represents DeviceClientEndpointIdentity which uses IoTHub SAS authentication
    /// </summary>
    internal class DeviceClientEndpointIdentityIoTHubSas : DeviceClientEndpointIdentity
    {
        internal DeviceClientEndpointIdentityIoTHubSas(IotHubConnectionString iotHubConnectionString, AmqpTransportSettings amqpTransportSettings, ProductInfo productInfo)
            : base (iotHubConnectionString, amqpTransportSettings, productInfo)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(DeviceClientEndpointIdentityIoTHubSas)}");
        }
    }
}