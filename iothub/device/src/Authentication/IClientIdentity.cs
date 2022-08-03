// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Transport;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Interface for device configurations and common attributes.
    /// </summary>
    internal interface IClientIdentity
    {
        /// <summary>
        /// Authentication model for the device.
        /// </summary>
        AuthenticationModel AuthenticationModel { get; }

        /// <summary>
        /// Client configuration options at the time of initialization.
        /// </summary>
        IotHubClientOptions ClientOptions { get; }

        /// <summary>
        /// Client authentication audience.
        /// </summary>
        string AmqpCbsAudience { get; }

        /// <summary>
        /// Whether or not the device is part of a connection pooling.
        /// </summary>
        bool IsPooling();
    }
}
