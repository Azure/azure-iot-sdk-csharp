// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
        /// Error code.
        /// </summary>
        [JsonProperty(PropertyName = "errorCode")]
        public int ErrorCode { get; set; }

        /// <summary>
        /// Correlation ID.
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
#pragma warning disable CA2227 // Collection properties should be read only
        public Dictionary<string, string> Info { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Time stamp (in UTC).
        /// </summary>
        public DateTime TimestampUtc { get; set; }
    }
}
