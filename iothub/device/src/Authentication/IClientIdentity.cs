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
        /// Authentication method associated with this client that uses a shared access signature token
        /// and allows for token refresh.
        /// </summary>
        AuthenticationWithTokenRefresh TokenRefresher { get; }

        /// <summary>
        /// The IoT hub this client connects to.
        /// </summary>
        string IotHubName { get; }

        /// <summary>
        /// The device ID associated with this client.
        /// </summary>
        string DeviceId { get; }

        /// <summary>
        /// The module ID associated with this client.
        /// </summary>
        string ModuleId { get; }

        /// <summary>
        /// The host service that this client connects to.
        /// This can either be the IoT hub name or a gateway service name.
        /// </summary>
        string HostName { get; }

        // TODO (abmisr): Consolidate with AmqpCbsAudience
        /// <summary>
        /// This currently points to the hostname.
        /// This needs to store the formatted audience (hostname/devices/deviceId)
        /// </summary>
        string Audience { get; }

        /// <summary>
        /// The shared access key name used to connect the device to the IoT hub service.
        /// </summary>
        string SharedAccessKeyName { get; }

        /// <summary>
        /// The shared access key used to connect to the IoT hub service.
        /// </summary>
        string SharedAccessKey { get; }

        /// <summary>
        /// The shared access signature used to connect to the IoT hub service.
        /// </summary>
        string SharedAccessSignature { get; }

        /// <summary>
        /// Indocates if the client is connecting to IoT hub service through a gateway service.
        /// </summary>
        bool IsUsingGateway { get; }

        /// <summary>
        /// Authentication model for the device.
        /// </summary>
        AuthenticationModel AuthenticationModel { get; }

        /// <summary>
        /// Client configuration options at the time of initialization.
        /// </summary>
        IotHubClientOptions ClientOptions { get; }

        // TODO (abmisr): Consolidate with Audience
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
