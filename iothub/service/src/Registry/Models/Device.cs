// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Azure;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains Device properties and their accessors.
    /// </summary>
    public class Device
    {
        /// <summary>
        /// Creates a new instance of this class. For serialization purposes only.
        /// </summary>
        internal Device()
        {
        }

        /// <summary>
        /// Creates a new instance of device.
        /// </summary>
        /// <param name="id">Device Id</param>
        public Device(string id)
        {
            Argument.AssertNotNullOrWhiteSpace(id, nameof(id));

            Id = id;
        }

        /// <summary>
        /// Device Id.
        /// </summary>
        [JsonProperty(PropertyName = "deviceId")]
        public string Id { get; internal set; }

        /// <summary>
        /// Device's Generation Id.
        /// </summary>
        /// <remarks>
        /// This value is used to distinguish devices with the same deviceId, when they have been deleted and re-created.
        /// </remarks>
        [JsonProperty(PropertyName = "generationId")]
        public string GenerationId { get; internal set; }

        /// <summary>
        /// Device's ETag.
        /// </summary>
        [JsonProperty(PropertyName = "etag")]
        [JsonConverter(typeof(NewtonsoftJsonETagConverter))] // NewtonsoftJsonETagConverter is used here because otherwise the ETag isn't serialized properly
        public ETag ETag { get; set; }

        /// <summary>
        /// Device's connection state.
        /// </summary>
        [JsonProperty(PropertyName = "connectionState")]
        [JsonConverter(typeof(StringEnumConverter))]
        public DeviceConnectionState ConnectionState { get; internal set; }

        /// <summary>
        /// Device's status.
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        [JsonConverter(typeof(StringEnumConverter))]
        public DeviceStatus Status { get; set; }

        /// <summary>
        /// Reason, if any, for the device to be in specified status.
        /// </summary>
        [JsonProperty(PropertyName = "statusReason")]
        public string StatusReason { get; set; }

        /// <summary>
        /// Time when the connection state was last updated.
        /// </summary>
        [JsonProperty(PropertyName = "connectionStateUpdatedTime")]
        public DateTimeOffset ConnectionStateUpdatedOnUtc { get; internal set; }

        /// <summary>
        /// Time when the status was last updated.
        /// </summary>
        [JsonProperty(PropertyName = "statusUpdatedTime")]
        public DateTimeOffset StatusUpdatedOnUtc { get; internal set; }

        /// <summary>
        /// Time when the device was last active.
        /// </summary>
        [JsonProperty(PropertyName = "lastActivityTime")]
        public DateTimeOffset LastActiveOnUtc { get; internal set; }

        /// <summary>
        /// Number of messages sent to the device from the cloud.
        /// </summary>
        [JsonProperty(PropertyName = "cloudToDeviceMessageCount")]
        public int CloudToDeviceMessageCount { get; internal set; }

        /// <summary>
        /// Device's authentication mechanism.
        /// </summary>
        [JsonProperty(PropertyName = "authentication")]
        public AuthenticationMechanism Authentication { get; set; }

        /// <summary>
        ///  Capabilities that are enabled one the device.
        /// </summary>
        [JsonProperty(PropertyName = "capabilities", NullValueHandling = NullValueHandling.Ignore)]
        public virtual DeviceCapabilities Capabilities { get; set; }

        /// <summary>
        /// The scope of the device. For edge devices, this is auto-generated and immutable. For leaf devices, set this to create child/parent
        /// relationship.
        /// </summary>
        /// <remarks>
        /// For leaf devices, the value to set a parent edge device can be retrieved from the parent edge device's scope property.
        /// For more information, see <see href="https://docs.microsoft.com/azure/iot-edge/iot-edge-as-gateway?view=iotedge-2020-11#parent-and-child-relationships"/>.
        /// </remarks>
        [JsonProperty(PropertyName = "deviceScope", NullValueHandling = NullValueHandling.Ignore)]
        public virtual string Scope { get; set; }

        /// <summary>
        /// The scopes of the upper level edge devices if applicable.
        /// </summary>
        /// <remarks>
        /// For edge devices, the value to set a parent edge device can be retrieved from the parent edge device's scope property.
        ///
        /// For leaf devices, this could be set to the same value as scope or left for the service to copy over.
        ///
        /// For now, this list can only have 1 element in the collection.
        ///
        /// For more information, see <see href="https://docs.microsoft.com/azure/iot-edge/iot-edge-as-gateway?view=iotedge-2020-11#parent-and-child-relationships"/>.
        /// </remarks>
        [JsonProperty(PropertyName = "parentScopes", NullValueHandling = NullValueHandling.Ignore)]
        public virtual IList<string> ParentScopes { get; internal set; } = new List<string>();
    }
}
