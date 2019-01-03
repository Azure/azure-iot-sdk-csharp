// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        internal DeviceClientEndpointIdentity(IotHubConnectionString iotHubConnectionString, AmqpTransportSettings amqpTransportSettings)
        {
            this.iotHubConnectionString = iotHubConnectionString;
            this.amqpTransportSettings = amqpTransportSettings;
        }
    }
}