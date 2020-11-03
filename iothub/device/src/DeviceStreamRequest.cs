// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Represents a new incoming request for device streaming.
    /// </summary>
    public sealed class DeviceStreamRequest
    {
        /// <summary>
        /// Creates a new instance of DeviceStreamRequest.
        /// </summary>
        /// <param name="requestId">The Id of this request.</param>
        /// <param name="name">Name of the stream.</param>
        /// <param name="uri">URI to the IoT Hub streaming gateway.</param>
        /// <param name="authorizationToken">Authorization token used to connect to the gateway.</param>
        internal DeviceStreamRequest(string requestId, string name, Uri uri, string authorizationToken)
        {
            RequestId = requestId;
            Name = name;
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
            AuthorizationToken = authorizationToken;
        }

        /// <summary>
        /// The Id of this streaming request.
        /// </summary>
        public string RequestId { get; private set; }

        /// <summary>
        /// Name of the IoT Hub stream, created by the originating endpoint.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Uri for connecting to the IoT Hub streaming gateway.
        /// </summary>
        public Uri Uri { get; private set; }

        /// <summary>
        /// Authorization token provided by the streaming gateway for the client endpoint to connect to it.
        /// </summary>
        public string AuthorizationToken { get; private set; }
    }
}
