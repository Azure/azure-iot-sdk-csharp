// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Represents the Cloud To Device Stream Initation Result.
    /// </summary>
    public class DeviceStreamResponse
    {
        /// <summary>
        /// Name of the stream.
        /// </summary>
        public string StreamName { get; private set; }

        /// <summary>
        /// Indicates if the stream request was accepted by the target endpoint.
        /// </summary>
        public bool IsAccepted { get; private set; }

        /// <summary>
        /// Authorization token provided by the Streaming gateway for the client endpoint to connect to it.
        /// </summary>
        public string AuthorizationToken { get; private set; }

        /// <summary>
        /// Uri for connecting to the IoT Hub streaming gateway.
        /// </summary>
        public Uri Url { get; private set; }

        /// <summary>
        /// Result for a Cloud-to-Device streaming request.
        /// </summary>
        /// <param name="streamName">Name of the Device Stream</param>
        /// <param name="isAccepted">Indicates if the remote endpoint has accepted to stream</param>
        /// <param name="authorizationToken">Authorization token provided by the Streaming gateway for the client endpoint to connect to it.</param>
        /// <param name="streamingGatewayUri">Uri for connecting to the IoT Hub streaming gateway.</param>
        public DeviceStreamResponse(string streamName, bool isAccepted, string authorizationToken, Uri streamingGatewayUri)
        {
            StreamName = streamName;
            IsAccepted = isAccepted;
            AuthorizationToken = authorizationToken;
            Url = streamingGatewayUri;
        }
    }
}
