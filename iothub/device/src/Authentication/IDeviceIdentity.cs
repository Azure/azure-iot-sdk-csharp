// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Transport;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Interface for device configurations and common attributes.
    /// </summary>
    internal interface IDeviceIdentity
    {
        /// <summary>
        /// Authentication model for the device.
        /// </summary>
        AuthenticationModel AuthenticationModel { get; }

        /// <summary>
        /// AMQP transport layer settings of the device.
        /// </summary>
        IotHubClientAmqpSettings AmqpTransportSettings { get; }

        /// <summary>
        /// Device connection information details.
        /// </summary>
        IotHubConnectionInfo IotHubConnectionInfo { get; }

        /// <summary>
        /// SDK,.NET version, Operating system and environment information.
        /// </summary>
        ProductInfo ProductInfo { get; }

        /// <summary>
        /// Device configuration options at the time of initialization.
        /// </summary>
        IotHubClientOptions Options { get; }

        /// <summary>
        /// Device authentication audience.
        /// </summary>
        string Audience { get; }

        /// <summary>
        /// Whether or not the device is part of a connection pooling.
        /// </summary>
        bool IsPooling();
    }
}
