// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Used to describe the X509 attestation mechanism.
    /// </summary>
    public sealed class X509Attestation
    {
        /// <summary>
        /// Creates a new instance of <see cref="X509Attestation"/>
        /// </summary>
        public X509Attestation()
        {
        }

        /// <summary>
        /// Client certificates.
        /// </summary>
        [JsonProperty(PropertyName = "clientCertificates", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public X509Certificates ClientCertificates { get; set; }

        /// <summary>
        /// Signing certificates.
        /// </summary>
        [JsonProperty(PropertyName = "signingCertificates", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public X509Certificates SigningCertificates { get; set; }

        /// <summary>
        /// Certificates Authority references.
        /// </summary>
        [JsonProperty(PropertyName = "caReferences", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public X509CAReferences CAReferences { get; set; }
    }
}
