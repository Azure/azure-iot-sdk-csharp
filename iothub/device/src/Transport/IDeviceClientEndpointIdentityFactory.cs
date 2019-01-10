// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.IO;

    /// <summary>
    /// Factory interface to create DeviceClientEndpointIdentity objects for Amqp transport layer
    /// </summary>
    interface IDeviceClientEndpointIdentityFactory
    {
        DeviceClientEndpointIdentity Create(IotHubConnectionString iotHubConnectionString, AmqpTransportSettings amqpTransportSettings, ProductInfo productInfo);
    }
}