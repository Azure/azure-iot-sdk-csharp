// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains service statistics that can be retrieved from IoT hub.
    /// </summary>
    public class ServiceStatistics
    {
        /// <summary>
        /// For deserialization and unit testing purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ServiceStatistics()
        { }

        /// <summary>
        /// Number of devices connected to IoT hub.
        /// </summary>
        [JsonPropertyName("connectedDeviceCount")]
        public long ConnectedDeviceCount { get; protected internal set; }
    }
}
