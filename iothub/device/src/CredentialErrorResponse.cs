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
    }
    internal class CredentialErrorInfo
    {
        /// <summary>
        /// Correlation ID matching the operation.
        /// </summary>
        [JsonProperty("correlationId")]
        public string CorrelationId { get; set; }
        /// <summary>
        /// CMS-specific error name (e.g., "FailedToDecodeCsr").
        /// Only present for 400040 errors.
        /// </summary>
        [JsonProperty("credentialError")]
        public string CredentialError { get; set; }
        /// <summary>
        /// Human-readable error message from CMS.
        /// Only present for 400040 errors.
        /// </summary>
        [JsonProperty("credentialMessage")]
        public string CredentialMessage { get; set; }
        /// <summary>
        /// The active request ID (for 409005 errors).
        /// </summary>
        [JsonProperty("requestId")]
        public string RequestId { get; set; }
        /// <summary>
        /// Operation expiration time (for 409005 errors).
        /// </summary>
        [JsonProperty("operationExpires")]
        public DateTimeOffset? OperationExpires { get; set; }
    }
}
