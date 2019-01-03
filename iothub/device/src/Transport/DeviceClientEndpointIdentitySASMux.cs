// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.IO;

    /// <summary>
    /// Represents DeviceClientEndpointIdentity which uses SAS authentication and multiplexing
    /// </summary>
    internal class DeviceClientEndpointIdentitySASMux : DeviceClientEndpointIdentity
    {
        internal DeviceClientEndpointIdentitySASMux(IotHubConnectionString iotHubConnectionString, AmqpTransportSettings amqpTransportSettings)
            : base(iotHubConnectionString, amqpTransportSettings)
        {
        }
    }
}