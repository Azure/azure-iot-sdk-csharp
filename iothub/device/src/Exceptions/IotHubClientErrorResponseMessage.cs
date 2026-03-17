// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A class used as a model to deserialize error response message object received from IoT hub.
    /// </summary>
    public sealed class IotHubClientErrorResponseMessage
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("errorCode")]
        public int ErrorCode { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("trackingId")]
        public string TrackingId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("timestampUtc")]
        public string OccurredOnUtc { get; set; }
    }
}
