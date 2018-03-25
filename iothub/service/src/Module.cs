// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#if ENABLE_MODULES_SDK
namespace Microsoft.Azure.Devices
{
    using System;
    using Microsoft.Azure.Devices.Shared;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// 
    /// </summary>
    public class Module : IETagHolder
    {
        /// <summary>
        /// 
        /// </summary>
        public Module()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        public Module(string deviceId, string moduleId)
        {
            this.Id = moduleId;
            this.DeviceId = deviceId;
            this.ConnectionState = DeviceConnectionState.Disconnected;
            this.LastActivityTime = this.ConnectionStateUpdatedTime = DateTime.MinValue;
        }

        /// <summary>
        /// Module ID
        /// </summary>
        [JsonProperty(PropertyName = "moduleId")]
        public string Id { get; internal set; }

        /// <summary>
        /// Device ID
        /// </summary>
        [JsonProperty(PropertyName = "deviceId")]
        public string DeviceId { get; internal set; }

        /// <summary>
        /// Modules's Generation ID
        /// </summary>
        [JsonProperty(PropertyName = "generationId")]
        public string GenerationId { get; internal set; }

        /// <summary>
        /// Module's ETag
        /// </summary>
        [JsonProperty(PropertyName = "etag")]
        public string ETag
        {
            get;
            set;
        }

        /// <summary>
        /// Modules's ConnectionState
        /// </summary>
        [JsonProperty(PropertyName = "connectionState")]
        [JsonConverter(typeof(StringEnumConverter))]
        public DeviceConnectionState ConnectionState { get; set; }

        /// <summary>
        /// Time when the <see cref="ConnectionState"/> was last updated
        /// </summary>
        [JsonProperty(PropertyName = "connectionStateUpdatedTime")]
        public DateTime ConnectionStateUpdatedTime { get; set; }

        /// <summary>
        /// Time when the <see cref="Module"/> was last active
        /// </summary>
        [JsonProperty(PropertyName = "lastActivityTime")]
        public DateTime LastActivityTime { get; internal set; }

        /// <summary>
        /// Number of messages sent to the Module from the Cloud
        /// </summary>
        [JsonProperty(PropertyName = "cloudToDeviceMessageCount")]
        public int CloudToDeviceMessageCount { get; internal set; }

        /// <summary>
        /// Device's authentication mechanism
        /// </summary>
        [JsonProperty(PropertyName = "authentication")]
        public AuthenticationMechanism Authentication { get; set; }

        /// <summary>
        /// represents the modules managed by owner
        /// </summary>
        [JsonProperty(PropertyName = "managedBy")]
        public string ManagedBy { get; private set; }
    }
}
#endif