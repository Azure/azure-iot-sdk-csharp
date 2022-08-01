// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// General request options that are applicable, but optional, for invoke command APIs.
    /// </summary>
    public class DigitalTwinInvokeCommandRequestOptions
    {
        /// <summary>
        /// The serialized command payload.
        /// </summary>
        public string Payload { get; set; }

        /// <summary>
        /// The time (in seconds) that the service waits for the device to come online.
        /// The default is 0 seconds (which means the device must already be online) and the maximum is 300 seconds.
        /// </summary>
        public int? ConnectTimeoutInSeconds { get; set; }

        /// <summary>
        /// The time (in seconds) that the service waits for the method invocation to return a response.
        /// The default is 30 seconds, minimum is 5 seconds, and maximum is 300 seconds.
        /// </summary>
        public int? ResponseTimeoutInSeconds { get; set; }
    }
}
