﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.LongHaul.Module
{
    internal abstract class TelemetryBase
    {
        /// <summary>
        /// The date/time the event occurred, in UTC.
        /// </summary>
        [JsonPropertyName("eventDateTimeUtc")]
        public DateTime? EventDateTimeUtc { get; set; } = DateTime.UtcNow;
    }
}
