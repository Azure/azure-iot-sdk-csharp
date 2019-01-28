// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
#pragma warning disable CA1717 // Only FlagsAttribute enums should have plural names - Reason: Not plural.
    /// <summary>
    /// Connection Status supported by DeviceClient
    /// </summary>
    public enum ConnectionStatus
#pragma warning restore CA1717 // Only FlagsAttribute enums should have plural names - Reason: Not plural.
    {
        /// <summary>
        /// The device or module is disconnected..
        /// </summary>
        Disconnected,

        /// <summary>
        /// The device or module is connected.
        /// </summary>
        Connected,

#pragma warning disable CA1707 // Identifiers should not contain underscores - Reason: public API cannot be changed.
        /// <summary>
        /// The device is attempting to reconnect.
        /// </summary>
        Disconnected_Retrying,
#pragma warning restore CA1707 // Identifiers should not contain underscores - Reason: public API cannot be changed.

        /// <summary>
        /// The device connection was closed.
        /// </summary>
        Disabled,
    }
}
