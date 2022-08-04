// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Optional parameters to execute a direct method with.
    /// </summary>
    public class DirectMethodRequestOptions
    {
        /// <summary>
        /// The timeout (in seconds) before the direct method request will fail if the device fails to respond to the request.
        /// This timeout may happen if the target device is slow in handling the direct method.
        /// </summary>
        public int? ResponseTimeout { get; set; }

        /// <summary>
        /// The timeout (in seconds) before the direct method request will fail if the request takes too long to reach the device.
        /// This timeout may happen if the target device is not connected to the cloud or if the cloud fails to deliver
        /// the request to the target device in time. If this value is set to 0 seconds, then the target device must be online
        /// when this direct method request is made.
        /// </summary>
        public int? ConnectionTimeout { get; set; }

        /// <summary>
        /// The json payload for the request.
        /// </summary>
        public string Payload { get; set; }
    }
}
