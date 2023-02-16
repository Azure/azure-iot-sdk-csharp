// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
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
        [SuppressMessage("Usage", "CA1507: Use nameof in place of string literal 'Message'",
            Justification = "This JsonProperty annotation depends on service-defined contract (name) and is independent of the property name selected by the SDK.")]
        [JsonProperty("Message")]
#pragma warning restore CA1507 // Use nameof in place of string
        internal string Message { get; set; }
    }
}
