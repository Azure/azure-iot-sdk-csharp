using System;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.LongHaul.Device
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
