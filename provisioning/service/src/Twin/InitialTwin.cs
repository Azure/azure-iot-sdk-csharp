// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single twin initial state.
    /// </summary>
    /// <remarks>
    /// Each entity in the collections can contain a associated <see cref="InitialTwinMetadata"/>.
    /// <para>
    /// These metadata are provided by the service and contains information about the last
    /// updated date time, and version.
    /// </para>
    /// </remarks>
    public class InitialTwin
    {
        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        public InitialTwin()
        {
        }

        /// <summary>
        /// Getter and setter the for tags.
        /// </summary>
        [JsonPropertyName("tags")]
        public JsonDictionary Tags { get; set; } = new();

        /// <summary>
        /// Getter and setter the desired properties.
        /// </summary>
        [JsonPropertyName("properties")]
        public InitialTwinPropertyCollection DesiredProperties { get; set; }
    }
}
