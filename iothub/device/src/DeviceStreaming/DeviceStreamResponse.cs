﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The data structure to represent the stream response that is used for interacting with Iot Hub.
    /// </summary>
    internal sealed class DeviceStreamResponse
    {
        /// <summary>
        /// Default constructor with no requestId and accept/reject outcome.
        /// </summary>
        internal DeviceStreamResponse(string requestId, bool isAccepted)
        {
            RequestId = requestId;
            IsAccepted = isAccepted;
        }

        /// <summary>
        /// Indicates wether the stream request is accepted (true) or rejected (false).
        /// </summary>
        internal bool IsAccepted { get; set; }

        /// <summary>
        /// The request Id for the transport layer.
        /// </summary>
        internal string RequestId { get; set; }
    }
}
