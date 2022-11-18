﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Represents twin properties.
    /// </summary>
    public class ClientTwinDocument
    {
        /// <summary>
        /// Gets and sets the twin desired properties.
        /// </summary>
        [JsonPropertyName("desired")]
        public ClientTwinProperties Desired { get; set; } = new();

        /// <summary>
        /// Gets and sets the twin reported properties.
        /// </summary>
        [JsonPropertyName("reported")]
        public ClientTwinProperties Reported { get; set; } = new();
    }
}
