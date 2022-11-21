// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// A class used as a model to deserialize error response object received from IoT hub.
    /// </summary>
    internal class IotHubExceptionResult
    {
        [JsonPropertyName("Message")]
        internal ResponseMessage Message { get; set; }
    }

    /// <summary>
    /// A class used as a model to deserialize a different style of error response received form IoT hub.
    /// </summary>
    internal class IotHubExceptionResult2
    {
        [JsonPropertyName("Message")]
        internal string Message { get; set; }

    }
}
