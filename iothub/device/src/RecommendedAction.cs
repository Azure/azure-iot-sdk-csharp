// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Recommended actions for users to take upon different ConnectionStatus and ConnectionStatusChangeReason
    /// </summary>
    public enum RecommendedAction
    {
        /// <summary>
        /// It's recommended to take no more actions as all operations on the device or module client will be carried out as normal.
        /// This is expected when <see cref="ConnectionStatus.Connected"/> and <see cref="ConnectionStatusChangeReason.ConnectionOk"/>.
        /// </summary>
        NoActionWhenNormal,

        /// <summary>
        /// It's recommended to not close or open the DeviceClient or ModuleClient instance as the client client is trying to recover
        /// from a disconnect due to a transient exception.
        /// This is expected when <see cref="ConnectionStatus.DisconnectedRetrying"/> and <see cref="ConnectionStatusChangeReason.CommunicationError"/>.
        /// </summary>
        NoActionWhenRetrying,

        /// <summary>
        /// If you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.
        /// This is expected when:
        /// <list type="bullet">
        /// <item><description>The client has been closed gracefully with <see cref="ConnectionStatus.Disabled"/> and <see cref="ConnectionStatusChangeReason.ClientClose"/>.
        /// </description></item>
        /// <item><description>The client has been disconnected because the retry policy expired with <see cref="ConnectionStatus.Disconnected"/> and <see cref="ConnectionStatusChangeReason.RetryExpired"/>.
        /// </description></item>
        /// <item><description>The client has been disconnected due to a non-retry-able exception with <see cref="ConnectionStatus.Disconnected"/> and <see cref="ConnectionStatusChangeReason.CommunicationError"/>.
        /// Inspect the exception for details.</description></item>
        /// </list>
        /// </summary>
        DisposeAndOpenIfWish,

        /// <summary>
        /// The supplied credentials are invalid. Use a valid one and run again.
        /// This is expected when <see cref="ConnectionStatus.Disconnected"/> and <see cref="ConnectionStatusChangeReason.BadCredential"/>.
        /// </summary>
        UseValidCredential,

        /// <summary>
        /// Fix the device status in the Iot hub and then create a new device client instance as the device has been deleted or marked as disabled (on your hub instance).
        /// This is expected when <see cref="ConnectionStatus.Disconnected"/> and <see cref="ConnectionStatusChangeReason.DeviceDisabled"/>.
        /// </summary>
        FixDeviceStatus,

        /// <summary>
        /// This combination of ConnectionStatus and ConnectionStatusChangeReason is not expected, contact the client library team with logs.
        /// </summary>
        ContactUs,
    }
}
