// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices.Common.Data
{
    /// <summary>
    /// Shared access policy permissions of IoT hub.
    /// For more information, see <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-security#iot-hub-permissions"/>.
    /// </summary>
    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AccessRights
    {
        /// <summary>
        /// Grants read access to the identity registry.
        /// Identity registry stores information about the devices and modules permitted to connect to the IoT hub.
        /// For more information, see <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-identity-registry"/>.
        /// </summary>
        RegistryRead = 1,

        /// <summary>
        /// Grants read and write access to the identity registry.
        /// Identity registry stores information about the devices and modules permitted to connect to the IoT hub.
        /// For more information, see <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-identity-registry"/>.
        /// </summary>
        RegistryWrite = RegistryRead | 2,

        /// <summary>
        /// Grants access to service facing communication and monitoring endpoints.
        /// It grants permission to receive device-to-cloud messages, send cloud-to-device messages, retrieve delivery acknowledgments for sent messages
        /// and file uploads, retrieve desired and reported properties on device twins, update tags and desired properties on device twins,
        /// and run queries.
        /// </summary>
        ServiceConnect = 4,

        /// <summary>
        /// Grants access to device facing endpoints.
        /// It grants permission to send device-to-cloud messages, receive cloud-to-device messages, perform file upload from a device, receive device twin
        /// desired property notifications and update device twin reported properties.
        /// </summary>
        DeviceConnect = 8
    }
}
