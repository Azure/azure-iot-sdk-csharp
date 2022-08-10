// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The data structure represent the method response that is used for interacting with IoT hub.
    /// </summary>
    internal sealed class MethodResponseInternal
    {
        /// <summary>
        /// Initiailizes this class with the specified data.
        /// </summary>
        /// <param name="requestId">the method request id corresponding to this respond.</param>
        /// <param name="status">the status code of the method call.</param>
        /// <param name="payload">The method response payload.</param>
        internal MethodResponseInternal(
            string requestId, int status, byte[] payload = null)
        {
            RequestId = requestId;
            Status = status;
            Payload = payload;
        }

        /// <summary>
        /// the request Id for the transport layer
        /// </summary>
        internal string RequestId { get; }

        /// <summary>
        /// contains the response of the device client application method handler.
        /// </summary>
        internal int Status { get; }

        /// <summary>
        /// The method response payload.
        /// </summary>
        internal byte[] Payload { get; }
    }
}
