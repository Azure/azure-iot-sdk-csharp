// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Azure;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains IoTHub Module properties and their accessors.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "Cannot rename public facing types since they are considered behavior changes.")]
    public class Module
    {
        /// <summary>
        /// Creates a new instance of this class. For serialization purposes only.
        /// </summary>
        internal Module()
        {
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="deviceId">Device identifier</param>
        /// <param name="moduleId">Module identifier</param>
        public Module(string deviceId, string moduleId)
        {
            Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
            Argument.AssertNotNullOrWhiteSpace(moduleId, nameof(moduleId));

            Id = moduleId;
            DeviceId = deviceId;
        }

        /// <summary>
        /// Module Id.
        /// </summary>
        [JsonProperty("moduleId")]
        public string Id { get; internal set; }

        /// <summary>
        /// Device Id.
        /// </summary>
        [JsonProperty("deviceId")]
        public string DeviceId { get; internal set; }

        /// <summary>
        /// Modules's generation Id.
        /// </summary>
        /// <remarks>
        /// This value is used to distinguish devices with the same deviceId, when they have been deleted and re-created.
        /// </remarks>
        [JsonProperty("generationId")]
        public string GenerationId { get; internal set; }

        /// <summary>
        /// Module's ETag.
        /// </summary>
        [JsonProperty("etag")]
        // NewtonsoftJsonETagConverter is used here because otherwise the ETag isn't serialized properly.
        [JsonConverter(typeof(NewtonsoftJsonETagConverter))]
        public ETag ETag { get; set; }

        /// <summary>
        /// Modules's connection state.
        /// </summary>
        [JsonProperty("connectionState")]
        public ClientConnectionState ConnectionState { get; internal set; }

        /// <summary>
        /// Time when the connection state was last updated.
        /// </summary>
        [JsonProperty("connectionStateUpdatedTime")]
        public DateTimeOffset? ConnectionStateUpdatedOnUtc { get; internal set; }

        /// <summary>
        /// Time when the module was last active.
        /// </summary>
        [JsonProperty("lastActivityTime")]
        public DateTimeOffset? LastActiveOnUtc { get; internal set; }

        /// <summary>
        /// Number of messages sent to the module from the cloud.
        /// </summary>
        [JsonProperty("cloudToDeviceMessageCount")]
        public int CloudToDeviceMessageCount { get; internal set; }

        /// <summary>
        /// Device's authentication mechanism.
        /// </summary>
        [JsonProperty("authentication")]
        public AuthenticationMechanism Authentication { get; set; } = new();

        /// <summary>
        /// Represents the modules managed by owner.
        /// </summary>
        [JsonProperty("managedBy")]
        public string ManagedBy { get; set; }
    }
}
