// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Status of handling a message.
    /// </summary>
    public enum MessageResponse
    {
        /// <summary>
        /// No acknowledgment of receipt will be sent.
        /// </summary>
        None,

        /// <summary>
        /// Event will be completed, removing it from the queue.
        /// </summary>
        Completed,

        /// <summary>
        /// Event will be abandoned.
        /// </summary>
        Abandoned,
    };
}
