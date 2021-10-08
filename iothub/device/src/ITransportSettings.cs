﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Interface used to define various transport-specific settings for DeviceClient and ModuleClient.
    /// </summary>
    public interface ITransportSettings
    {
        /// <summary>
        /// Returns the transport type of the TransportSettings object.
        /// </summary>
        /// <returns>The TransportType</returns>
        TransportType GetTransportType();

        /// <summary>
        /// The time to wait for a receive operation.
        /// </summary>
        TimeSpan DefaultReceiveTimeout { get; }
    }
}
