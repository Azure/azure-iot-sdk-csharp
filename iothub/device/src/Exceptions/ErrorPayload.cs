// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A class used as a model to deserialize one schema type of errors received from IoT hub.
    /// </summary>
    public sealed class ErrorPayload
    {
        /// <summary>
        /// The error code sent by IoT hub
        /// </summary>
        [JsonPropertyName("errorCode")]
        public dynamic ErrorCode { get; set; }

        /// <summary>
        /// The tracking Id associated with this error. Provide this when contacting customer support.
        /// </summary>
        [JsonPropertyName("trackingId")]
        public string TrackingId { get; set; }

        /// <summary>
        /// The human-readable error message sent by IoT hub.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; }

        /// <summary>
        /// The same json field as <see cref="Message"/>, but this allows for the json property name to be capitalized
        /// as some error messages sent from IoT hub are.
        /// </summary>
        [JsonPropertyName("Message")]
        public string AlternateMessage 
        {
            get => Message; 
            set => Message = value;
        }

        /// <summary>
        /// The datetime when the error occurred.
        /// </summary>
        [JsonPropertyName("timestampUtc")]
        public string OccurredOnUtc { get; set; }

        /// <summary>
        /// The enum of the error code sent by IoT hub.
        /// </summary>
        [JsonIgnore]
        public IotHubClientErrorCode IotHubClientErrorCode { get; set; }
    }
}
