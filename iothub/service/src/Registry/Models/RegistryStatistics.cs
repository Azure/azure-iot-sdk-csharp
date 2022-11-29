// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Text.Json.Serialization;

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
        [EditorBrowsable(EditorBrowsableState.Never)]
        public RegistryStatistics()
        { }

        /// <summary>
        /// Gets or sets the count of all devices.
        /// </summary>
        [JsonPropertyName("totalDeviceCount")]
        public long TotalDeviceCount { get; protected internal set; }

        /// <summary>
        /// Gets or sets the count of all enabled devices.
        /// </summary>
        [JsonPropertyName("enabledDeviceCount")]
        public long EnabledDeviceCount { get; protected internal set; }

        /// <summary>
        /// Gets or sets the count of all disabled devices.
        /// </summary>
        [JsonPropertyName("disabledDeviceCount")]
        public long DisabledDeviceCount { get; protected internal set; }
    }
}
