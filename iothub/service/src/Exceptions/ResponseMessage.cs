// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// A class used as a model to deserialize error response message object received from IoT hub.
    /// </summary>
    internal class ResponseMessage
    {
        [JsonPropertyName("errorCode")]
        internal string ErrorCode { get; set; }

        [JsonPropertyName("trackingId")]
        internal string TrackingId { get; set; }

        [JsonPropertyName("message")]
        internal string Message { get; set; }

        [JsonPropertyName("timestampUtc")]
        internal string OccurredOnUtc { get; set; }
    }

    internal class ResponseMessageWrapper
    {
        [JsonPropertyName("Message")]
        internal string Message { get; set; }
    }
}
