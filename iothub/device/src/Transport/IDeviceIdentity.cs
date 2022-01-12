// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
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
        AmqpTransportSettings AmqpTransportSettings { get; }

        /// <summary>
        /// Device connection string details.
        /// </summary>
        IotHubConnectionString IotHubConnectionString { get; }

        /// <summary>
        /// Device details and information.
        /// </summary>
        ProductInfo ProductInfo { get; }

        /// <summary>
        /// Device configuration options at the time of initialization.
        /// </summary>
        ClientOptions Options { get; }

        /// <summary>
        /// Device authentication audience.
        /// </summary>
        string Audience { get; }

        /// <summary>
        /// Whether or not Device is part of a connection pooling.
        /// </summary>
        bool IsPooling();
    }
}
