// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The device registry statistics that can be retrieved from IoT hub.
    /// </summary>
    public class RegistryStatistics
    {
        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        /// <remarks>
        /// This class can be inherited from and set by unit tests for mocking purposes.
        /// </remarks>
        protected internal RegistryStatistics()
        { }

        /// <summary>
        /// Gets or sets the count of all devices.
        /// </summary>
        [JsonProperty("totalDeviceCount")]
        public long TotalDeviceCount { get; protected internal set; }

        /// <summary>
        /// Gets or sets the count of all enabled devices.
        /// </summary>
        [JsonProperty("enabledDeviceCount")]
        public long EnabledDeviceCount { get; protected internal set; }

        /// <summary>
        /// Gets or sets the count of all disabled devices.
        /// </summary>
        [JsonProperty("disabledDeviceCount")]
        public long DisabledDeviceCount { get; protected internal set; }
    }
}
