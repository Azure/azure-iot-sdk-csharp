// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Represents the response from IoT Hub containing issued certificates.
    /// </summary>
    public class CertificateSigningResponse
    {
        /// <summary>
        /// List of Base64-encoded certificates in the certificate chain.
        /// The first certificate is the issued device certificate, followed by intermediates.
        /// </summary>
        [JsonProperty("certificates")]
        public IList<string> Certificates { get; set; }

        /// <summary>
        /// Correlation ID for diagnostic and support purposes.
        /// Matches the correlationId from the 202 Accepted response.
        /// </summary>
        [JsonProperty("correlationId")]
        public string CorrelationId { get; set; }
    }
}
