// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Recommended actions for device applications to take upon different connection change events.
    /// </summary>
    public enum RecommendedAction
    {
        /// <summary>
        /// The default recommended action. This is being used for the initialization of ConnectionInfo only.
        /// </summary>
        DefaultAction,

        /// <summary>
        /// It's recommended to perform operations normally on your device client as it is successfully connected to the IoT hub.
        /// </summary>
        PerformNormally,

        /// <summary>
        /// It's recommended to not perform any operations on the client when:
        /// <list type="bullet">
        /// <item><description>The client has been closed gracefully.</description></item>
        /// <item><description>The client is trying to recover from a retry-able exception.</description></item>
        /// <item><description>The client has been disconnected due to non-retry-able exceptions with <see cref="ConnectionStatusChangeReason.BadCredential"/> or <see cref="ConnectionStatusChangeReason.DeviceDisabled"/>.
        /// Inspect the exception for details.</description></item>
        /// </list>
        /// </summary>
        NotDoAnything,

        /// <summary>
        /// It's recommended to re-initialize (dispose and open) the device client when the client has been disconnected due to non-retry-able exceptions with
        /// <see cref="ConnectionStatusChangeReason.RetryExpired"/> or <see cref="ConnectionStatusChangeReason.CommunicationError"/>.
        /// </summary>
        ReinitializeClient,

        /// <summary>
        /// The combination of ConnectionStatus and ConnectionStatusChangeReason is not expected, contact the client library team with logs.
        /// </summary>
        ContactUs,
    }
}
