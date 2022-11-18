// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Device registration.
    /// </summary>
    internal class DeviceRegistration
    {
        /// <summary>
        /// Creates an instance of the DeviceRegistration class.
        /// </summary>
        public DeviceRegistration(JRaw payload = default)
        {
            Payload = payload;
        }

        /// <summary>
        /// Gets or set the custom content payload.
        /// </summary>
        [JsonPropertyName("payload")]
        public JRaw Payload { get; internal set; }
    }
}
