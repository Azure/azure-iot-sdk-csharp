// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Represents the different collections of properties on a client twin.
    /// </summary>
    public class ProvisioningTwinDocument
    {
        /// <summary>
        /// Gets and sets the twin desired properties.
        /// </summary>
        [JsonPropertyName("desired")]
        public ProvisioningTwinProperties Desired { get; set; } = new();

        /// <summary>
        /// Gets and sets the twin reported properties.
        /// </summary>
        [JsonPropertyName("reported")]
        public ProvisioningTwinProperties Reported { get; set; } = new();
    }
}
