// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Azure;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Twin Representation.
    /// </summary>
    [JsonConverter(typeof(TwinJsonConverter))]
    public class Twin
    {
        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        public Twin()
        {
            Tags = new TwinCollection();
            Properties = new TwinProperties();
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="deviceId">Device Id</param>
        public Twin(string deviceId) : this()
        {
            DeviceId = deviceId;
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="twinProperties"></param>
        public Twin(TwinProperties twinProperties)
        {
            Tags = new TwinCollection();
            Properties = twinProperties;
        }

        /// <summary>
        /// Gets and sets the twin device Id.
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// The DTDL model Id of the device.
        /// </summary>
        /// <remarks>
        /// The value will be null for a non-pnp device.
        /// The value will be null for a pnp device until the device connects and registers with the model Id.
        /// </remarks>
        public string ModelId { get; set; }

        /// <summary>
        /// Gets and sets the twin Module Id.
        /// </summary>
        public string ModuleId { get; set; }

        /// <summary>
        /// Gets and sets the  twin tags.
        /// </summary>
        public TwinCollection Tags { get; set; }

        /// <summary>
        /// Gets and sets the twin properties.
        /// </summary>
        public TwinProperties Properties { get; set; }

        /// <summary>
        /// Gets the twin configuration properties. These are read only
        /// </summary>
        public IDictionary<string, ConfigurationInfo> Configurations { get; internal set; }

        /// <summary>
        /// Gets the  twin capabilities. These are read only.
        /// </summary>
        public DeviceCapabilities Capabilities { get; set; }

        /// <summary>
        /// Twin's ETag.
        /// </summary>
        [JsonProperty(PropertyName = "etag", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [JsonConverter(typeof(NewtonsoftJsonETagConverter))] // NewtonsoftJsonETagConverter is used here because otherwise the ETag isn't serialized properly
        public ETag ETag { get; set; }

        /// <summary>
        /// Twin's Version.
        /// </summary>
        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public long? Version { get; set; }

        /// <summary>
        /// Gets the corresponding Device's Status.
        /// </summary>
        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public DeviceStatus? Status { get; internal set; }

        /// <summary>
        /// Reason, if any, for the corresponding device to be in specified status.
        /// </summary>
        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string StatusReason { get; internal set; }

        /// <summary>
        /// Time when the corresponding device's status was last updated
        /// </summary>
        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public DateTime? StatusUpdatedOnUtc { get; internal set; }

        /// <summary>
        /// Corresponding device's connection state.
        /// </summary>
        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [JsonConverter(typeof(StringEnumConverter))]
        public DeviceConnectionState? ConnectionState { get; internal set; }

        /// <summary>
        /// Time when the corresponding device was last active.
        /// </summary>
        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public DateTime? LastActiveOnUtc { get; internal set; }

        /// <summary>
        /// Number of messages sent to the corresponding device from the cloud.
        /// </summary>
        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int? CloudToDeviceMessageCount { get; internal set; }

        /// <summary>
        /// Corresponding device's authentication type.
        /// </summary>
        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public AuthenticationType? AuthenticationType { get; internal set; }

        /// <summary>
        /// Corresponding device's X509 thumbprint.
        /// </summary>
        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public X509Thumbprint X509Thumbprint { get; internal set; }

        /// <summary>
        /// The scope of the device. Auto-generated and immutable for edge devices and modifiable in leaf devices to create child/parent relationship.
        /// </summary>
        /// <remarks>
        /// For more information, see <see href="https://docs.microsoft.com/azure/iot-edge/iot-edge-as-gateway?view=iotedge-2020-11#parent-and-child-relationships"/>.
        /// </remarks>
        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string DeviceScope { get; internal set; }

        /// <summary>
        /// The scopes of the upper level edge devices if applicable. Only available for edge devices.
        /// </summary>
        /// <remarks>
        /// For more information, see <see href="https://docs.microsoft.com/azure/iot-edge/iot-edge-as-gateway?view=iotedge-2020-11#parent-and-child-relationships"/>.
        /// </remarks>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public virtual IReadOnlyList<string> ParentScopes { get; internal set; } = new List<string>();

        /// <summary>
        /// Gets the twin as a JSON string.
        /// </summary>
        /// <param name="formatting">Formatting for the output JSON string.</param>
        /// <returns>JSON string</returns>
        public string ToJson(Formatting formatting = Formatting.None)
        {
            return JsonConvert.SerializeObject(this, formatting);
        }
    }
}
