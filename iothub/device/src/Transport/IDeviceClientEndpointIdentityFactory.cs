// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// Factory interface to create DeviceClientEndpointIdentity objects for Amqp transport layer
    /// </summary>
    internal interface IDeviceClientEndpointIdentityFactory
    {
        DeviceIdentity Create(IotHubConnectionString iotHubConnectionString, AmqpTransportSettings amqpTransportSettings, ProductInfo productInfo);
    }
}
