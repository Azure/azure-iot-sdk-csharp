// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// The different configurations available for setting a value of MessageId on an IoT Hub Message.
    /// </summary>
    public enum SdkAssignsMessageId
    {
        /// <summary>
        /// MessageId is set only by the user.
        /// </summary>
        Never,

        /// <summary>
        /// If MessageId is not set by the user, the client library will set it to a random GUID before sending the message.
        /// </summary>
        WhenUnset,
    }
}
