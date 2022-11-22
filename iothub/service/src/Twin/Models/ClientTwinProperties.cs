// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Represents twin properties.
    /// </summary>
    public sealed class ClientTwinProperties
    {
        /// <summary>
        /// Gets and sets the twin desired properties.
        /// </summary>
        [JsonPropertyName("desired")]
        public ClientTwinPropertyCollection Desired { get; set; } = new();

        /// <summary>
        /// Gets and sets the twin reported properties.
        /// </summary>
        [JsonPropertyName("reported")]
        public ClientTwinPropertyCollection Reported { get; set; } = new();
    }
}
