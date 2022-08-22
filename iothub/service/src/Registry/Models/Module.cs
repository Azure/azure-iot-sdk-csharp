// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
        /// Creates a new instance of <see cref="Module"/>.
        /// </summary>
        /// <param name="deviceId">Device identifier</param>
        /// <param name="moduleId">Module identifier</param>
        public Module(string deviceId, string moduleId)
        {
            Argument.RequireNotNullOrEmpty(deviceId, nameof(deviceId));
            Argument.RequireNotNullOrEmpty(moduleId, nameof(moduleId));

            Id = moduleId;
            DeviceId = deviceId;
        }

        /// <summary>
        /// Module Id.
        /// </summary>
        [JsonProperty(PropertyName = "moduleId")]
        public string Id { get; internal set; }

        /// <summary>
        /// Device Id.
        /// </summary>
        [JsonProperty(PropertyName = "deviceId")]
        public string DeviceId { get; internal set; }

        /// <summary>
        /// Modules's Generation Id.
        /// </summary>
        [JsonProperty(PropertyName = "generationId")]
        public string GenerationId { get; internal set; }

        /// <summary>
        /// Module's ETag.
        /// </summary>
        [JsonProperty(PropertyName = "etag")]
        public string ETag { get; set; }

        /// <summary>
        /// Modules's ConnectionState.
        /// </summary>
        [JsonProperty(PropertyName = "connectionState")]
        [JsonConverter(typeof(StringEnumConverter))]
        public DeviceConnectionState ConnectionState { get; internal set; }

        /// <summary>
        /// Time when the <see cref="ConnectionState"/> was last updated.
        /// </summary>
        [JsonProperty(PropertyName = "connectionStateUpdatedTime")]
        public DateTime ConnectionStateUpdatedTime { get; internal set; }

        /// <summary>
        /// Time when the <see cref="Module"/> was last active.
        /// </summary>
        [JsonProperty(PropertyName = "lastActivityTime")]
        public DateTime LastActivityTime { get; internal set; }

        /// <summary>
        /// Number of messages sent to the <see cref="Module"/> from the cloud.
        /// </summary>
        [JsonProperty(PropertyName = "cloudToDeviceMessageCount")]
        public int CloudToDeviceMessageCount { get; internal set; }

        /// <summary>
        /// Device's authentication mechanism.
        /// </summary>
        [JsonProperty(PropertyName = "authentication")]
        public AuthenticationMechanism Authentication { get; set; }

        /// <summary>
        /// represents the modules managed by owner.
        /// </summary>
        [JsonProperty(PropertyName = "managedBy")]
        public string ManagedBy { get; set; }
    }
}
