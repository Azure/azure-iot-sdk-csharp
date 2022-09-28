// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// The policy for which a particular message will be sent. Note that Device Provisioning Service does not support QoS 2.
    /// </summary>
    public enum QualityOfService
    {
        /// <summary>
        /// The message will be sent once. It will not be resent under any circumstances.
        /// </summary>
        AtMostOnce = 0,

        /// <summary>
        /// The message will be sent once, but will be resent if the service fails to acknowledge the message.
        /// </summary>
        AtLeastOnce = 1,
    }
}
