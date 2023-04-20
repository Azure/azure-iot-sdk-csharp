// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.LongHaul.Device
{
    internal abstract class TelemetryBase
    {
        /// <summary>
        /// The date/time the event occurred, in UTC.
        /// </summary>
        [JsonProperty("eventDateTimeUtc")]
        [JsonPropertyName("eventDateTimeUtc")]
        public DateTime? EventDateTimeUtc { get; set; } = DateTime.UtcNow;
    }
}
