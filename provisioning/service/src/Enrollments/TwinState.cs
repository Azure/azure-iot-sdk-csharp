// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Twin initial state for the Device Provisioning Service.
    /// </summary>
    /// <remarks>
    /// The TwinState can contain one <see cref="TwinCollection"/> of <b>Tags</b>, and one
    ///     <see cref="TwinCollection"/> of <b>properties.desired</b>.
    ///
    /// Each entity in the collections can contain a associated <see cref="Metadata"/>.
    ///
    /// These metadata are provided by the Service and contains information about the last
    ///     updated date time, and version.
    /// </remarks>
    /// <example>
    /// For instance, the following is a valid TwinState, represented as
    ///     <c>initialTwin</c> in the rest API.
    /// <c>
    /// {
    ///     "initialTwin": {
    ///         "tags":{
    ///             "SpeedUnity":"MPH",
    ///             "$metadata":{
    ///                 "$lastUpdated":"2017-09-21T02:07:44.238Z",
    ///                 "$lastUpdatedVersion":4,
    ///                 "SpeedUnity":{
    ///                     "$lastUpdated":"2017-09-21T02:07:44.238Z",
    ///                     "$lastUpdatedVersion":4
    ///                 }
    ///             },
    ///             "$version":4
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
    /// </c>
    /// </example>
    public class TwinState
    {
        [JsonProperty(PropertyName = "properties", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private TwinProperties _properties;

        /// <summary>
        /// Creates an instance of TwinState.
        /// </summary>
        /// <remarks>
        /// This constructor creates an instance of the TwinState with the provided <see cref="TwinCollection"/>
        /// tags and desired properties.
        /// </remarks>
        /// <example>
        /// When serialized, this class will looks like the following example:
        /// <c>
        /// {
        ///     "initialTwin": {
        ///         "tags":{
        ///             "SpeedUnity":"MPH",
        ///             "$version":4
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
        /// </c>
        /// </example>
        /// <param name="tags">the <see cref="TwinCollection"/> with the initial tags state. It can be <c>null</c>.</param>
        /// <param name="desiredProperties">the <see cref="TwinCollection"/> with the initial desired properties. It can be <c>null</c>.</param>
        public TwinState(TwinCollection tags, TwinCollection desiredProperties)
        {
            Tags = tags;
            DesiredProperties = desiredProperties;
        }

        [JsonConstructor]
        private TwinState(TwinCollection tags, TwinProperties properties)
        {
            Tags = tags;
            if (properties == null)
            {
                DesiredProperties = null;
            }
            else
            {
                DesiredProperties = properties.Desired;
            }
        }

        /// <summary>
        /// Getter and setter the <see cref="TwinState"/> tags.
        /// </summary>
        [JsonProperty(PropertyName = "tags", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public TwinCollection Tags { get; set; }

        /// <summary>
        /// Getter and setter the <see cref="TwinState"/> properties.
        /// </summary>
        [JsonIgnore]
        public TwinCollection DesiredProperties
        {
            get => _properties?.Desired;

            set
            {
                if (value == null)
                {
                    _properties = null;
                }
                else
                {
                    _properties = new TwinProperties
                    {
                        Desired = value,
                        Reported = null,
                    };
                }
            }
        }

        /// <summary>
        /// Convert this object in a pretty print format.
        /// </summary>
        /// <returns>The <c>string</c> with the content of this class in a pretty print format.</returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
