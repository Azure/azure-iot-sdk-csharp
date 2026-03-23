using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// A class used as a model to deserialize response body object received from DPS in error cases.
    /// </summary>
    public sealed class ErrorResponse
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("errorCode")]
        public int ErrorCode { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("code")]
        public int Code
        {
            get => ErrorCode;
            set => ErrorCode = value;
        }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("trackingId")]
        public string TrackingId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("timestampUtc")]
        public string OccurredOnUtc { get; set; }
    }
}
