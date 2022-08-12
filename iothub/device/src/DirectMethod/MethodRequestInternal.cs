// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The data structure represent the method request coming from the IoT hub.
    /// </summary>
    internal sealed class MethodRequestInternal
    {
        /// <summary>
        /// Initializes this class with the specified data.
        /// </summary>
        internal MethodRequestInternal(string name, string requestId, byte[] payload = null)
        {
            Name = name;
            RequestId = requestId;
            Payload = payload;
        }

        /// <summary>
        /// The name of the direct method.
        /// </summary>
        internal string Name { get; }

        /// <summary>
        /// The request Id for the transport layer.
        /// </summary>
        internal string RequestId { get; }

        /// <summary>
        /// The method request payload.
        /// </summary>
        internal byte[] Payload { get; private set; }
    }
}
