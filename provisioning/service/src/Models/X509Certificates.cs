// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single device provisioning service X509 primary and secondary certificates.
    /// </summary>
    /// <remarks>
    /// This class creates a representation of an X509 certificate. It can receive primary and secondary
    /// certificate, but only the primary is mandatory.
    /// </remarks>
    public class X509Certificates
    {
        /// <summary>
        /// Primary certificate.
        /// </summary>
        [JsonPropertyName("primary")]
        public X509CertificateWithInfo Primary { get; set; }

        /// <summary>
        /// Secondary certificate.
        /// </summary>
        [JsonPropertyName("secondary")]
        public X509CertificateWithInfo Secondary { get; set; }
    }
}
