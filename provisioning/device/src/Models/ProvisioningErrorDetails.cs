// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Provisioning error details.
    /// </summary>
    public class ProvisioningErrorDetails
    {
        /// <summary>
        /// The error code that caused the provisioning failure.
        /// </summary>
        [JsonProperty("errorCode")]
        public int ErrorCode { get; set; }

        /// <summary>
        /// A unique Id to share with the service team when seeking help with an error.
        /// </summary>
        [JsonProperty("trackingId")]
        public string TrackingId { get; set; }

        /// <summary>
        /// Error message.
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// Additional information.
        /// </summary>
        [JsonProperty("info", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> Info { get; internal set; } = new();
    }
}
