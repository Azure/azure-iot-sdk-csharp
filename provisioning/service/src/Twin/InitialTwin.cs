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
    /// Each entity in the collections can contain a associated <see cref="InitialTwinMetadata"/>.
    /// <para>
    /// These metadata are provided by the service and contains information about the last
    /// updated date time, and version.
    /// </para>
    /// </remarks>
    public class InitialTwin
    {
        [JsonProperty("properties", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private InitialTwinProperties _properties;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        public InitialTwin()
        {
        }

        [JsonConstructor]
#pragma warning disable IDE0051 // Used for deserialization
        private InitialTwin(IDictionary<string, object> tags, InitialTwinProperties properties)
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
