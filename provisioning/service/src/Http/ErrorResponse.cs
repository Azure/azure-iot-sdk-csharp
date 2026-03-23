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
        /// The error code sent by the Device Provisioning Service.
        /// </summary>
        [JsonPropertyName("errorCode")]
        public int ErrorCode { get; set; }

        /// <summary>
        /// The error code sent by the Device Provisioning Service
        /// </summary>
        /// <remarks>
        /// This field is a duplicate of <see cref="ErrorCode"/> for serialization purposes.
        /// </remarks>
        [JsonPropertyName("code")]
        public int Code
        {
            get => ErrorCode;
            set => ErrorCode = value;
        }

        /// <summary>
        /// The tracking Id to include in any communications with customer support.
        /// </summary>
        [JsonPropertyName("trackingId")]
        public string TrackingId { get; set; }

        /// <summary>
        /// The human-readable error message
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; }

        /// <summary>
        /// The date when the error occurred.
        /// </summary>
        [JsonPropertyName("timestampUtc")]
        public string OccurredOnUtc { get; set; }
    }
}
