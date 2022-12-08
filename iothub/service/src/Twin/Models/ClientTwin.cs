// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Azure;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Properties of a device or module stored on the service.
    /// </summary>
    public class ClientTwin
    {
        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        public ClientTwin() { }

        /// <summary>
        /// Creates an instance of this class with the specified device Id.
        /// </summary>
        /// <param name="deviceId">The unique Id of the device to which the twin belongs.</param>
        public ClientTwin(string deviceId)
        {
            DeviceId = deviceId;
        }

        /// <summary>
        /// Gets and sets the twin Id.
        /// </summary>
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        /// <summary>
        /// The DTDL model Id of the device or module.
        /// </summary>
        /// <remarks>
        /// The value will be null for a non-plug-and-play device.
        /// The value will be null for a plug-and-play device until the device connects and registers with the model Id.
        /// </remarks>
        [JsonProperty("modelId")]
        public string ModelId { get; set; }

        /// <summary>
        /// Gets and sets the twin module Id.
        /// </summary>
        [JsonProperty("moduleId")]
        public string ModuleId { get; set; }

        /// <summary>
        /// Gets and sets the twin tags.
        /// </summary>
        [JsonProperty("tags")]
        public IDictionary<string, object> Tags { get; protected internal set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets and sets the twin properties.
        /// </summary>
        [JsonProperty("properties")]
        public ClientTwinDocument Properties { get; set; } = new();

        /// <summary>
        /// Gets the twin configuration properties.
        /// </summary>
        /// <remarks>
        /// Configuration properties are read only.
        /// </remarks>
        [JsonProperty("configurations")]
        public IDictionary<string, ConfigurationInfo> Configurations { get; internal set; } = new Dictionary<string, ConfigurationInfo>();

        /// <summary>
        /// Gets the twin capabilities.
        /// </summary>
        /// <remarks>
        /// Twin capabilities are read only.
        /// </remarks>
        [JsonProperty("capabilities")]
        public ClientCapabilities Capabilities { get; internal set; } = new();

        /// <summary>
        /// Twin's ETag.
        /// </summary>
        [JsonProperty("etag")]
        // NewtonsoftJsonETagConverter is used here because otherwise the ETag isn't serialized properly.
        [JsonConverter(typeof(NewtonsoftJsonETagConverter))]
        public ETag ETag { get; set; }

        /// <summary>
        /// Device's ETag.
        /// </summary>
        [JsonProperty("deviceEtag")]
        // NewtonsoftJsonETagConverter is used here because otherwise the ETag isn't serialized properly.
        [JsonConverter(typeof(NewtonsoftJsonETagConverter))]
        public ETag DeviceETag { get; set; }

        /// <summary>
        /// Twin's version.
        /// </summary>
        public long? Version { get; set; }

        /// <summary>
        /// Gets the corresponding device's status.
        /// </summary>
        [JsonProperty("status")]
        public ClientStatus? Status { get; internal set; }

        /// <summary>
        /// Reason, if any, for the corresponding device to be in specified status.
        /// </summary>
        [JsonProperty("statusReason")]
        public string StatusReason { get; internal set; }

        /// <summary>
        /// Time when the corresponding device's status was last updated.
        /// </summary>
        [JsonProperty("statusUpdateTime")]
        public DateTimeOffset? StatusUpdatedOnUtc { get; internal set; }

        /// <summary>
        /// Corresponding device's connection state.
        /// </summary>
        [JsonProperty("connectionState")]
        public ClientConnectionState? ConnectionState { get; internal set; }

        /// <summary>
        /// Time when the corresponding device was last active.
        /// </summary>
        [JsonProperty("lastActivityTime")]
        public DateTimeOffset? LastActiveOnUtc { get; internal set; }

        /// <summary>
        /// Number of messages sent to the corresponding device from the cloud.
        /// </summary>
        [JsonProperty("cloudToDeviceMessageCount")]
        public int? CloudToDeviceMessageCount { get; internal set; }

        /// <summary>
        /// Corresponding device's authentication type.
        /// </summary>
        [JsonProperty("authenticationType")]
        public ClientAuthenticationType? AuthenticationType { get; internal set; }

        /// <summary>
        /// Corresponding device's X509 thumbprint.
        /// </summary>
        [JsonProperty("x509Thumbprint")]
        public X509Thumbprint X509Thumbprint { get; internal set; }

        /// <summary>
        /// The scope of the device. Auto-generated and immutable for edge devices and modifiable in leaf devices to create child/parent relationship.
        /// </summary>
        /// <remarks>
        /// For more information, see <see href="https://docs.microsoft.com/azure/iot-edge/iot-edge-as-gateway?view=iotedge-2020-11#parent-and-child-relationships"/>.
        /// </remarks>
        [JsonProperty("deviceScope")]
        public string DeviceScope { get; internal set; }

        /// <summary>
        /// The scopes of the upper level edge devices if applicable. Only available for edge devices.
        /// </summary>
        /// <remarks>
        /// For more information, see <see href="https://docs.microsoft.com/azure/iot-edge/iot-edge-as-gateway?view=iotedge-2020-11#parent-and-child-relationships"/>.
        /// </remarks>
        [JsonProperty("parentScopes")]
        public virtual IReadOnlyList<string> ParentScopes { get; internal set; } = new List<string>();
    }
}
