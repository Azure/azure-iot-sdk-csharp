// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Specifies the different connection states of a device.
    /// </summary>
    public enum DeviceConnectionState
    {
        /// <summary>
        /// Represents a device in the disconnected state.
        /// </summary>
        Disconnected = 0,

        /// <summary>
        /// Represents a device in the connected state.
        /// </summary>
        Connected = 1
    }
}
