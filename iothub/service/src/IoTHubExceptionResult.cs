﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// A class used as a model to deserialize error response object received from IoT Hub
    /// </summary>
    internal class IoTHubExceptionResult
    {
        [JsonProperty("Message")]
        internal string Message { get; set; }

        [JsonProperty("ExceptionMessage")]
        internal string ExceptionMessage { get; set; }
    }
}
