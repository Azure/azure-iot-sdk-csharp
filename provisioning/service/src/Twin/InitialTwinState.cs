// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single twin initial state.
    /// </summary>
    /// <remarks>
    /// Each entity in the collections can contain a associated <see cref="ProvisioningTwinMetadata"/>.
    ///
    /// These metadata are provided by the service and contains information about the last
    /// updated date time, and version.
    /// </remarks>
    public class InitialTwinState
    {
        [JsonProperty("properties", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private InitialTwinProperties _properties;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <remarks>
        /// This constructor creates an instance of the TwinState with the provided twin collection tags and desired properties.
        /// </remarks>
        /// <param name="tags">The twin collection with the initial tags state. It can be null.</param>
        /// <param name="desiredProperties">The twin collection with the initial desired properties. It can be null.</param>
        public InitialTwinState(IDictionary<string, object> tags, InitialTwinPropertyCollection desiredProperties)
        {
            Tags = tags;
            DesiredProperties = desiredProperties;
        }

        [JsonConstructor]
#pragma warning disable IDE0051 // Used for deserialization
        private InitialTwinState(IDictionary<string, object> tags, InitialTwinProperties properties)
#pragma warning restore IDE0051
        {
            Tags = tags;
            DesiredProperties = properties?.Desired;
        }

        /// <summary>
        /// Getter and setter the for tags.
        /// </summary>
        [JsonProperty("tags", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IDictionary<string, object> Tags { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Getter and setter the desired properties.
        /// </summary>
        [JsonIgnore]
        public InitialTwinPropertyCollection DesiredProperties
        {
            get => _properties?.Desired;

            set => _properties = value == null
                ? null
                : new InitialTwinProperties
                    {
                        Desired = value,
                    };
        }
    }
}
