// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Represents a new incoming request for Device Streaming.
    /// </summary>
    public sealed class DeviceStreamRequest
    {
        /// <summary>
        /// Creates a new instance of StreamRequest
        /// </summary>
        /// <param name="requestId">ID of this request.</param>
        /// <param name="name">Name of the stream</param>
        /// <param name="url">Url to the Device Streaming gateway</param>
        /// <param name="authorizationToken">Authorization token used to connect to the gateway</param>
        public DeviceStreamRequest(String requestId, string name, Uri url, string authorizationToken)
        {
            RequestId = requestId;
            Name = name;
            Url = url;
            AuthorizationToken = authorizationToken;
        }

        /// <summary>
        /// ID of this streaming request.
        /// </summary>
        public string RequestId { get; private set; }

        /// <summary>
        /// Name of the IoT Hub Stream as created by the originating endpoint.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Url for connecting to the IoT Hub streaming gateway.
        /// </summary>
        public Uri Url { get; private set; }

        /// <summary>
        /// Authorization token provided by the Streaming gateway for the client endpoint to connect to it.
        /// </summary>
        public string AuthorizationToken { get; private set; }
    }
}
