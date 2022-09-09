// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Specifies the different connection statuses of a device.
    /// </summary>
    public enum DeviceConnectionStatus
    {
        /// <summary>
        /// Represents a device in the Disconnected status.
        /// </summary>
        Disconnected = 0,

        /// <summary>
        /// Represents a device in the Connected status.
        /// </summary>
        Connected = 1
    }
}
