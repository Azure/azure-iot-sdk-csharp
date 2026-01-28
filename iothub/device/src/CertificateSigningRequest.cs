// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Represents a certificate signing request to be sent to IoT Hub.
    /// </summary>
    public class CertificateSigningRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateSigningRequest"/> class.
        /// </summary>
        /// <param name="id">The device ID the certificate will be issued for. Must match the currently authenticated device ID.</param>
        /// <param name="csrData">The Base64-encoded PKCS#10 CSR without PEM headers/footers or newlines.</param>
        public CertificateSigningRequest(string id, string csrData)
        {
            Id = id;
            CertificateSigningRequestData = csrData;
        }
        
        /// <summary>
        /// Required. The device ID the certificate will be issued for.
        /// Must match the currently authenticated device ID.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Required. The Base64-encoded PKCS#10 CSR without PEM headers/footers or newlines.
        /// </summary>
        [JsonProperty("csr")]
        public string CertificateSigningRequestData { get; set; }

        /// <summary>
        /// Optional. Request ID to replace, or "*" to replace any active request.
        /// Use when:
        /// - The CSR is known to be different from a previous incomplete request
        /// - Client received 409005 and doesn't know if CSR has changed (e.g., storage failure)
        /// Default: null (will fail with 409005 if an active operation exists)
        /// </summary>
        [JsonProperty("replace", NullValueHandling = NullValueHandling.Ignore)]
        public string? Replace { get; set; }
    }
}
