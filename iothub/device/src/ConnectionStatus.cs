// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
#pragma warning disable CA1717 // Only FlagsAttribute enums should have plural names - Reason: Not plural.
    /// <summary>
    /// Connection status supported by the client.
    /// </summary>
    public enum ConnectionStatus
#pragma warning restore CA1717 // Only FlagsAttribute enums should have plural names - Reason: Not plural.
    {
        /// <summary>
        /// The device or module is disconnected.
        /// <para>Inspect the associated <see cref="ConnectionStatusChangeReason"/> returned (and exception thrown, if any), and take appropriate action.</para>
        /// </summary>
        Disconnected,

        /// <summary>
        /// The device or module is connected.
        /// <para>The client is connected, and ready to be used.</para>
        /// </summary>
        Connected,

        /// <summary>
        /// The device or module is attempting to reconnect.
        /// <para>The client is attempting to recover the connection. Do NOT close or open the client instance when it is retrying.</para>
        /// </summary>
        Disconnected_Retrying,

        /// <summary>
        /// The device connection was closed.
        /// <para>If you want to perform more operations on the device client, you should <see cref="IotHubDeviceClient.Dispose()"/> and then re-initialize the client.</para>
        /// </summary>
        Disabled,
    }
}
