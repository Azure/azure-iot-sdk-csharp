// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// A class used as a model to deserialize error response object received from IoT hub.
    /// </summary>
    internal sealed class IotHubExceptionResult
    {
        [SuppressMessage("Usage", "CA1507: Use nameof in place of string literal 'Message'",
            Justification = "This JsonProperty annotation depends on service-defined contract (name) and is independent of the property name selected by the SDK.")]
        [JsonProperty("Message")]
#pragma warning restore CA1507 // Use nameof in place of string
        internal ErrorPayload1 Message { get; set; }
    }

    /// <summary>
    /// A class used as a model to deserialize a different style of error response received from IoT hub.
    /// </summary>
    internal sealed class IotHubExceptionResult2
    {
        [SuppressMessage("Usage", "CA1507: Use nameof in place of string literal 'Message'",
            Justification = "This JsonProperty annotation depends on service-defined contract (name) and is independent of the property name selected by the SDK.")]
        [JsonProperty("Message")]
#pragma warning restore CA1507 // Use nameof in place of string
        internal string Message { get; set; }

    }
}
