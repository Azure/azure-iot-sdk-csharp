﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Options that allow configuration of the device or module client instance during initialization.
    /// </summary>
    public class ClientOptions
    {
        /// <summary>
        /// The digital twins model Id associated with the device or module client instance.
        /// This feature is currently supported only over MQTT.
        /// </summary>
        public string ModelId { get; set; }

        /// <summary>
        /// The transport settings to use for all file upload operations, regardless of what protocol the device
        /// client is configured with. All file upload operations take place over https. If no client certificates
        /// are configured to this object, then file upload operations will use the client certificates configured
        /// in the transport settings set for the non-file upload operations.
        /// </summary>
        public Http1TransportSettings FileUploadTransportSettings { get; set; } = new Http1TransportSettings();
    }
}
