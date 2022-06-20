// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Provisioning error details.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1812", Justification = "Used by the JSon parser.")]
    public class ProvisioningErrorDetails
    {
        /// <summary>
        /// Error code.
        /// </summary>
        [JsonProperty(PropertyName = "errorCode")]
        public int ErrorCode { get; set; }

        /// <summary>
        /// Correlation Id.
        /// </summary>
        [JsonProperty(PropertyName = "trackingId")]
        public string TrackingId { get; set; }

        /// <summary>
        /// Error message.
        /// </summary>
        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        /// <summary>
        /// Additional information.
        /// </summary>
        [JsonProperty(PropertyName = "info", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> Info { get; private set; } = new Dictionary<string, string>();

        /// <summary>
        /// Time stamp (in UTC).
        /// </summary>
        public string TimestampUtc { get; set; }

        /// <summary>
        /// Create the error message with the saved error code, tracking Id, and timestamp.
        /// </summary>
        /// <param name="message">A formatted error message.</param>
        public string CreateMessage(string message)
        {
            var sb = new StringBuilder();
            sb.AppendLine(message);
            sb.AppendLine($"Service Error: {ErrorCode} - {Message} (TrackingID: {TrackingId} Time: {TimestampUtc})");

            if (Info != null)
            {
                foreach (string key in Info.Keys)
                {
                    sb.AppendLine($"\t{key}: {Info[key]}");
                }
            }

            return sb.ToString();
        }
    }
}
