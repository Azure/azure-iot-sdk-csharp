// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Newtonsoft.Json;
namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Represents an error response from credential management operations.
    /// </summary>
    internal class CredentialErrorResponse
    {
        [JsonProperty("errorCode")]
        public int ErrorCode { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("trackingId")]
        public string TrackingId { get; set; }

        [JsonProperty("timestampUtc")]
        public DateTimeOffset TimestampUtc { get; set; }

        [JsonProperty("info")]
        public CredentialErrorInfo Info { get; set; }

        [JsonProperty("retryAfter")]
        public int? RetryAfterSeconds { get; set; }

        internal class CredentialErrorInfo
        {
            [JsonProperty("correlationId")]
            public string CorrelationId { get; set; }

            [JsonProperty("credentialError")]
            public string CredentialError { get; set; }

            [JsonProperty("credentialMessage")]
            public string CredentialMessage { get; set; }

            [JsonProperty("requestId")]
            public string RequestId { get; set; }

            [JsonProperty("operationExpires")]
            public DateTimeOffset? OperationExpires { get; set; }
        }
    }
}
