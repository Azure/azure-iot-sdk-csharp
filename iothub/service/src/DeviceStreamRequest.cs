// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Defines the parameters for sending a cloud-to-device Device Stream request.
    /// </summary>
    public class DeviceStreamRequest
    {
        private static TimeSpan _defaultDeviceStreamingTimeout = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Creates a new instance of DeviceStreamRequest
        /// </summary>
        /// <param name="streamName">Name of the Device Stream</param>
        public DeviceStreamRequest(string streamName)
        {
            if (string.IsNullOrWhiteSpace(streamName))
            {
                throw new ArgumentNullException(nameof(streamName));
            }

            this.StreamName = streamName;
            this.ResponseTimeout = _defaultDeviceStreamingTimeout;
            this.ConnectionTimeout = _defaultDeviceStreamingTimeout;
        }

        /// <summary>
        /// Name of the Device Stream
        /// </summary>
        public string StreamName { get; private set; }

        /// <summary>
        /// Maximum timeout for the Device Stream response to be received.
        /// </summary>
        public TimeSpan ResponseTimeout { get; private set; }

        /// <summary>
        /// Maximum timeout for the clients to connect to the streaming gateway.
        /// </summary>
        public TimeSpan ConnectionTimeout { get; private set; }
    }
}
