// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// A class used as a model to deserialize one schema type of errors received from IoT hub.
    /// </summary>
    internal sealed class ErrorPayload1
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

    internal sealed class ResponseMessageWrapper
    {
        [SuppressMessage("Usage", "CA1507: Use nameof in place of string literal 'Message'",
            Justification = "This JsonProperty annotation depends on service-defined contract (name) and is independent of the property name selected by the SDK.")]
        [JsonPropertyName("Message")]
        internal string Message { get; set; }
    }
}
