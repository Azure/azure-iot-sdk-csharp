// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains service statistics that can be retrieved from IoT hub.
    /// </summary>
    public class ServiceStatistics
    {
        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        /// <remarks>
        /// This class can be inherited from and set by unit tests for mocking purposes.
        /// </remarks>
        protected internal ServiceStatistics()
        { }

        /// <summary>
        /// Number of devices connected to IoT hub.
        /// </summary>
        [JsonProperty("connectedDeviceCount")]
        public long ConnectedDeviceCount { get; protected internal set; }
    }
}
