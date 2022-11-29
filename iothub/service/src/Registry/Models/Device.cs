// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Azure;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains Device properties and their accessors.
    /// </summary>
    public class Device
    {
        /// <summary>
        /// For serialization purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Device()
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
        [JsonPropertyName("deviceId")]
        public string Id { get; internal set; }

        /// <summary>
        /// Device's Generation Id.
        /// </summary>
        /// <remarks>
        /// This value is used to distinguish devices with the same deviceId, when they have been deleted and re-created.
        /// </remarks>
        [JsonPropertyName("generationId")]
        public string GenerationId { get; internal set; }

        /// <summary>
        /// Device's ETag.
        /// </summary>
        [JsonPropertyName("etag")]
        public ETag ETag { get; set; }

        /// <summary>
        /// Device's connection state.
        /// </summary>
        [JsonPropertyName("connectionState")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ClientConnectionState ConnectionState { get; internal set; }

        /// <summary>
        /// Device's status.
        /// </summary>
        [JsonPropertyName("status")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ClientStatus Status { get; set; }

        /// <summary>
        /// Reason, if any, for the device to be in specified status.
        /// </summary>
        [JsonPropertyName("statusReason")]
        public string StatusReason { get; set; }

        /// <summary>
        /// Time when the connection state was last updated.
        /// </summary>
        [JsonPropertyName("connectionStateUpdatedTime")]
        public DateTimeOffset ConnectionStateUpdatedOnUtc { get; internal set; }

        /// <summary>
        /// Time when the status was last updated.
        /// </summary>
        [JsonPropertyName("statusUpdatedTime")]
        public DateTimeOffset StatusUpdatedOnUtc { get; internal set; }

        /// <summary>
        /// Time when the device was last active.
        /// </summary>
        [JsonPropertyName("lastActivityTime")]
        public DateTimeOffset LastActiveOnUtc { get; internal set; }

        /// <summary>
        /// Number of messages sent to the device from the cloud.
        /// </summary>
        [JsonPropertyName("cloudToDeviceMessageCount")]
        public int CloudToDeviceMessageCount { get; internal set; }

        /// <summary>
        /// Device's authentication mechanism.
        /// </summary>
        [JsonPropertyName("authentication")]
        public AuthenticationMechanism Authentication { get; set; }

        /// <summary>
        ///  Capabilities that are enabled one the device.
        /// </summary>
        [JsonPropertyName("capabilities")]
        public virtual ClientCapabilities Capabilities { get; set; }

        /// <summary>
        /// The scope of the device. For edge devices, this is auto-generated and immutable. For leaf devices, set this to create child/parent
        /// relationship.
        /// </summary>
        /// <remarks>
        /// For leaf devices, the value to set a parent edge device can be retrieved from the parent edge device's scope property.
        /// For more information, see <see href="https://docs.microsoft.com/azure/iot-edge/iot-edge-as-gateway?view=iotedge-2020-11#parent-and-child-relationships"/>.
        /// </remarks>
        [JsonPropertyName("deviceScope")]
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
        [JsonPropertyName("parentScopes")]
        public virtual IList<string> ParentScopes { get; internal set; } = new List<string>();
    }
}
