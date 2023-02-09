// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// A class used as a model to deserialize one schema type of errors received from IoT hub.
    /// </summary>
    internal sealed class ErrorPayload1
    {
        [JsonProperty("errorCode")]
        internal string ErrorCode { get; set; }

        [JsonProperty("trackingId")]
        internal string TrackingId { get; set; }

        [JsonProperty("message")]
        internal string Message { get; set; }

        [JsonProperty("timestampUtc")]
        internal string OccurredOnUtc { get; set; }
    }

    internal sealed class ResponseMessageWrapper
    {
        [JsonProperty("Message")]
        internal string Message { get; set; }
    }
}
