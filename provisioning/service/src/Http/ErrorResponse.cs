using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// A class used as a model to deserialize response body object received from DPS in error cases.
    /// </summary>
    internal sealed class ErrorResponse
    {
        [JsonPropertyName("errorCode")]
        internal int ErrorCode { get; set; }

        [JsonPropertyName("code")]
        internal int Code
        {
            get => ErrorCode;
            set => ErrorCode = value;
        }

        [JsonPropertyName("trackingId")]
        internal string TrackingId { get; set; }

        [JsonPropertyName("message")]
        internal string Message { get; set; }

        [JsonPropertyName("timestampUtc")]
        internal string OccurredOnUtc { get; set; }
    }
}
