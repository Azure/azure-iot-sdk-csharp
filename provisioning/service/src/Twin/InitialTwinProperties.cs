﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// The "properties" section in a twin document that holds desired and reported properties.
    /// </summary>
    /// <remarks>
    /// For the purposes of setting the initial state of a twin, only the desired section can be provided.
    /// </remarks>
    public sealed class InitialTwinProperties
    {
        /// <summary>
        /// Gets and sets the twin desired properties.
        /// </summary>
        [JsonPropertyName("desired")]
        public InitialTwinPropertyCollection Desired { get; set; } = new();
    }
}