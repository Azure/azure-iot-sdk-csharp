﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// Base class of DeviceClientEndpointIdentity
    /// Stores the common attributes
    /// - connection string 
    /// - transport settings 
    /// </summary>
    internal abstract class DeviceClientEndpointIdentity
    {
        internal IotHubConnectionString iotHubConnectionString { get; }
        internal AmqpTransportSettings amqpTransportSettings { get; }

        internal ProductInfo productInfo { get; }

        internal DeviceClientEndpointIdentity(IotHubConnectionString iotHubConnectionString, AmqpTransportSettings amqpTransportSettings, ProductInfo productInfo)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(DeviceClientEndpointIdentity)}");

            this.iotHubConnectionString = iotHubConnectionString;
            this.amqpTransportSettings = amqpTransportSettings;
            this.productInfo = productInfo;

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(DeviceClientEndpointIdentity)}");
        }
    }
}