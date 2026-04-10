// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    /// <summary>
    /// The state of the MQTT transport
    /// </summary>
    [Flags]
#pragma warning disable CA1714 // Flags enums should have plural names
    public enum TransportState
#pragma warning restore CA1714 // Flags enums should have plural names
    {
        /// <summary>
        /// Transport is not initialized
        /// </summary>
        NotInitialized = 1,

        /// <summary>
        /// Transport is opening
        /// </summary>
        Opening = 2,

        /// <summary>
        /// Transport has opened
        /// </summary>
        Open = 4,

        /// <summary>
        /// Transport is subscribing
        /// </summary>
        Subscribing = Open | 8,

        /// <summary>
        /// Transport is receiving
        /// </summary>
        Receiving = Open | 16,

        /// <summary>
        /// Transport is closed
        /// </summary>
        Closed = 32,

        /// <summary>
        /// Transport is in an error state
        /// </summary>
        Error = 64,
    }
}
