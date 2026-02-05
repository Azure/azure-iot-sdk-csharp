// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Newtonsoft.Json;
namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Represents the 202 Accepted response from IoT Hub.
    /// Internal use only - used to track operation expiration.
    /// </summary>
    internal class CertificateAcceptedResponse
    {
        /// <summary>
        /// Correlation ID for diagnostic and support purposes.
        /// </summary>
        [JsonProperty("correlationId")]
        public string CorrelationId { get; set; }
        /// <summary>
        /// Time when the operation expires and will be discarded if not completed.
        /// Default is approximately 12 hours from acceptance.
        /// </summary>
        [JsonProperty("operationExpires")]
        public DateTimeOffset OperationExpires { get; set; }
    }
}
