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
    /// These metadata are provided by the Service and contains information about the last
    /// updated date time, and version.
    /// </remarks>
    /// <example>
    /// For instance, the following is a valid twin state, represented as <c>initialTwin</c> in the rest API.
    /// <code>
    /// {
    ///     "initialTwin": {
    ///         "tags":{
    ///             "SpeedUnity":"MPH",
    ///         }
    ///         "properties":{
    ///             "desired": {
    ///                 "MaxSpeed":{
    ///                     "Value":500,
    ///                     "NewValue":300
    ///                 },
    ///                 "$metadata":{
    ///                     "$lastUpdated":"2017-09-21T02:07:44.238Z",
    ///                     "$lastUpdatedVersion":4,
    ///                     "MaxSpeed":{
    ///                         "$lastUpdated":"2017-09-21T02:07:44.238Z",
    ///                         "$lastUpdatedVersion":4,
    ///                         "Value":{
    ///                             "$lastUpdated":"2017-09-21T02:07:44.238Z",
    ///                             "$lastUpdatedVersion":4
    ///                         },
    ///                         "NewValue":{
    ///                             "$lastUpdated":"2017-09-21T02:07:44.238Z",
    ///                             "$lastUpdatedVersion":4
    ///                         }
    ///                     }
    ///                 },
    ///                 "$version":4
    ///             }
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public class ProvisioningTwinState
    {
        [JsonProperty(PropertyName = "properties", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private ProvisioningTwinDocument _properties;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <remarks>
        /// This constructor creates an instance of the TwinState with the provided twin collection tags and desired properties.
        /// </remarks>
        /// <example>
        /// When serialized, this class will looks like the following example:
        /// <code>
        /// {
        ///     "initialTwin": {
        ///         "tags":{
        ///             "SpeedUnity":"MPH",
        ///         }
        ///         "properties":{
        ///             "desired":{
        ///                 "MaxSpeed":{
        ///                     "Value":500,
        ///                     "NewValue":300
        ///                 },
        ///                 "$version":4
        ///             }
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <param name="tags">The twin collection with the initial tags state. It can be null.</param>
        /// <param name="desiredProperties">The twin collection with the initial desired properties. It can be null.</param>
        public ProvisioningTwinState(IDictionary<string, object> tags, ProvisioningTwinProperties desiredProperties)
        {
            Tags = tags;
            DesiredProperties = desiredProperties;
        }

        [JsonConstructor]
        private ProvisioningTwinState(IDictionary<string, object> tags, ProvisioningTwinDocument properties)
        {
            Tags = tags;
            DesiredProperties = properties?.Desired;
        }

        /// <summary>
        /// Getter and setter the for tags.
        /// </summary>
        [JsonProperty(PropertyName = "tags", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IDictionary<string, object> Tags { get; set; }

        /// <summary>
        /// Getter and setter the desired properties.
        /// </summary>
        [JsonIgnore]
        public ProvisioningTwinProperties DesiredProperties
        {
            get => _properties?.Desired;

            set => _properties = value == null
                ? null
                : new ProvisioningTwinDocument
                    {
                        Desired = value,
                        Reported = null,
                    };
        }

        /// <summary>
        /// Convert this object in a pretty print format.
        /// </summary>
        /// <returns>The string with the content of this class in a pretty print format.</returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
