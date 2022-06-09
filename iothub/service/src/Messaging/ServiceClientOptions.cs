// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Options that allow configuration of the service client instance during initialization.
    /// </summary>
    public class ServiceClientOptions
    {
        /// <summary>
        /// The configuration for setting <see cref="Message.MessageId"/> for every message sent by the service client instance.
        /// The default behavior is that <see cref="Message.MessageId"/> is set only by the user.
        /// </summary>
        public SdkAssignsMessageId SdkAssignsMessageId { get; set; } = SdkAssignsMessageId.Never;
    }
}
