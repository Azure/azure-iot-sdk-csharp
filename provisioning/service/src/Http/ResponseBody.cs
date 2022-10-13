using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// A class used as a model to deserialize response body object received from DPS.
    /// </summary>
    internal class ResponseBody
    {
        [JsonProperty("errorCode")]
        internal int ErrorCode { get; set; }

        [JsonProperty("code")]
        internal int Code
        {
            get => ErrorCode;
            set => ErrorCode = value;
        }

        [JsonProperty("trackingId")]
        internal string TrackingId { get; set; }

        [JsonProperty("message")]
        internal string Message { get; set; }

        [JsonProperty("timestampUtc")]
        internal string OccurredOnUtc { get; set; }
    }
}
