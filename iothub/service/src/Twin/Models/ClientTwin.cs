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
    /// Properties of a device or module stored at the service.
    /// </summary>
    public class ClientTwin
    {
        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        public ClientTwin() { }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="deviceId">The unique Id of the device to which the twin belongs.</param>
        public ClientTwin(string deviceId)
        {
            DeviceId = deviceId;
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="twinProperties">Properties of the twin.</param>
        public ClientTwin(ClientTwinDocument twinProperties)
        {
            Properties = twinProperties ?? new();
        }

        /// <summary>
        /// Gets and sets the twin Id.
        /// </summary>
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; }

        /// <summary>
        /// The DTDL model Id of the device or module.
        /// </summary>
        /// <remarks>
        /// The value will be null for a non-plug-and-play device.
        /// The value will be null for a plug-and-play device until the device connects and registers with the model Id.
        /// </remarks>
        [JsonPropertyName("modelId")]
        public string ModelId { get; set; }

        /// <summary>
        /// Gets and sets the twin module Id.
        /// </summary>
        [JsonPropertyName("moduleId")]
        public string ModuleId { get; set; }

        /// <summary>
        /// Gets and sets the twin tags.
        /// </summary>
        [JsonPropertyName("tags")]
        public IDictionary<string, object> Tags { get; protected internal set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets and sets the twin properties.
        /// </summary>
        [JsonPropertyName("properties")]
        public ClientTwinDocument Properties { get; set; } = new();

        /// <summary>
        /// Gets the twin configuration properties.
        /// </summary>
        /// <remarks>
        /// Configuration properties are read only.
        /// </remarks>
        [JsonPropertyName("configurations")]
        public IDictionary<string, ConfigurationInfo> Configurations { get; internal set; } = new Dictionary<string, ConfigurationInfo>();

        /// <summary>
        /// Gets the twin capabilities.
        /// </summary>
        /// <remarks>
        /// Twin capabilities are read only.
        /// </remarks>
        [JsonPropertyName("capabilities")]
        public ClientCapabilities Capabilities { get; internal set; }

        /// <summary>
        /// Twin's ETag.
        /// </summary>
        [JsonPropertyName("etag")]
        public ETag ETag { get; set; }

        /// <summary>
        /// Twin's version.
        /// </summary>
        [DefaultValue(null)]
        [JsonPropertyName("version")]
        public long? Version { get; set; }

        /// <summary>
        /// Gets the corresponding device's status.
        /// </summary>
        [JsonPropertyName("status")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ClientStatus? Status { get; internal set; }

        /// <summary>
        /// Reason, if any, for the corresponding device to be in specified status.
        /// </summary>
        [DefaultValue(null)]
        [JsonPropertyName("statusReason")]
        public string StatusReason { get; internal set; }

        /// <summary>
        /// Time when the corresponding device's status was last updated.
        /// </summary>
        [DefaultValue(null)]
        [JsonPropertyName("statusUpdatedTime")]
        public DateTimeOffset? StatusUpdatedOnUtc { get; internal set; }

        /// <summary>
        /// Corresponding device's connection state.
        /// </summary>
        [DefaultValue(null)]
        [JsonPropertyName("connectionState")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ClientConnectionState? ConnectionState { get; internal set; }

        /// <summary>
        /// Time when the corresponding device was last active.
        /// </summary>
        [DefaultValue(null)]
        [JsonPropertyName("lastActivityTime")]
        public DateTimeOffset? LastActiveOnUtc { get; internal set; }

        /// <summary>
        /// Number of messages sent to the corresponding device from the cloud.
        /// </summary>
        [DefaultValue(null)]
        [JsonPropertyName("cloudtoDeviceMessageCount")]
        public int? CloudToDeviceMessageCount { get; internal set; }

        /// <summary>
        /// Corresponding device's authentication type.
        /// </summary>
        [JsonPropertyName("authenticationType")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ClientAuthenticationType? AuthenticationType { get; internal set; }

        /// <summary>
        /// Corresponding device's X509 thumbprint.
        /// </summary>
        [DefaultValue(null)]
        [JsonPropertyName("x509Thumbprint")]
        public X509Thumbprint X509Thumbprint { get; internal set; }

        /// <summary>
        /// The scope of the device. Auto-generated and immutable for edge devices and modifiable in leaf devices to create child/parent relationship.
        /// </summary>
        /// <remarks>
        /// For more information, see <see href="https://docs.microsoft.com/azure/iot-edge/iot-edge-as-gateway?view=iotedge-2020-11#parent-and-child-relationships"/>.
        /// </remarks>
        [DefaultValue(null)]
        [JsonPropertyName("deviceScope")]
        public string DeviceScope { get; internal set; }

        /// <summary>
        /// The scopes of the upper level edge devices if applicable. Only available for edge devices.
        /// </summary>
        /// <remarks>
        /// For more information, see <see href="https://docs.microsoft.com/azure/iot-edge/iot-edge-as-gateway?view=iotedge-2020-11#parent-and-child-relationships"/>.
        /// </remarks>
        [DefaultValue(null)]
        [JsonPropertyName("parentScopes")]
        public virtual IReadOnlyList<string> ParentScopes { get; internal set; } = new List<string>();
    }
}
