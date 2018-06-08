// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Microsoft.Azure.Devices.Shared;

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
    ///     <code>initialTwin</code> in the rest API.
    /// <code>
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
    /// </code>
    /// </example>
    /// <seealso cref="!:https://docs.microsoft.com/en-us/rest/api/iot-dps/deviceenrollment">Device Enrollment</seealso>
    /// <seealso cref="!:https://docs.microsoft.com/en-us/rest/api/iot-dps/deviceenrollmentgroup">Device Enrollment Group</seealso>
    /// <seealso cref="!:https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-device-twins">Understand and use device twins in IoT Hub</seealso>
    /// <seealso cref="!:https://docs.microsoft.com/en-us/rest/api/iothub/devicetwinapi">Device Twin Api</seealso>
    public class TwinState
    {
        /// <summary>
        /// Getter and setter the <see cref="TwinState"/> tags.
        /// </summary>
        [JsonProperty(PropertyName = "tags", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public TwinCollection Tags { get; set; }

        [JsonProperty(PropertyName = "properties", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private TwinProperties _properties;

        /// <summary>
        /// Getter and setter the <see cref="TwinState"/> properties.
        /// </summary>
        [JsonIgnore]
        public TwinCollection DesiredProperties
        {
            get
            {
                /* SRS_TWIN_STATE_21_002: [If the _properties is null, the get.DesiredProperties shall return null.] */
                if (_properties == null)
                {
                    return null;
                }

                /* SRS_TWIN_STATE_21_003: [The get.DesiredProperties shall return the content of _properties.Desired.] */
                return _properties.Desired;
            }

            set
            {
                /* SRS_TWIN_STATE_21_004: [If the value is null, the set.DesiredProperties shall set _properties as null.] */
                if (value == null)
                {
                    _properties = null;
                }
                else
                {
                    /* SRS_TWIN_STATE_21_005: [The set.DesiredProperties shall convert the provided value in a 
                                                TwinPropertyes.Desired and store it as _properties.] */
                    _properties = new TwinProperties()
                    {
                        Desired = value,
                        Reported = null,
                    };
                }
            }
        }

        /// <summary>
        /// CONSTRUCTOR
        /// </summary>
        /// <remarks>
        /// This constructor creates an instance of the TwinState with the provided <see cref="TwinCollection"/>
        ///     tags and desired properties.
        /// </remarks>
        /// <example>
        /// When serialized, this class will looks like the following example:
        /// <code>
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
        /// </code>
        /// </example>
        /// <param name="tags">the <see cref="TwinCollection"/> with the initial tags state. It can be <code>null</code>.</param>
        /// <param name="desiredProperties">the <see cref="TwinCollection"/> with the initial desired properties. It can be <code>null</code>.</param>
        public TwinState(TwinCollection tags, TwinCollection desiredProperties)
        {
            /* SRS_TWIN_STATE_21_001: [The constructor shall store the provided tags and desiredProperties.] */
            Tags = tags;
            DesiredProperties = desiredProperties;
        }

        [JsonConstructor]
        private TwinState(TwinCollection tags, TwinProperties properties)
        {
            Tags = tags;
            if(properties == null)
            {
                DesiredProperties = null;
            }
            else
            {
                DesiredProperties = properties.Desired;
            }
        }

        /// <summary>
        /// Convert this object in a pretty print format.
        /// </summary>
        /// <returns>The <code>string</code> with the content of this class in a pretty print format.</returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
