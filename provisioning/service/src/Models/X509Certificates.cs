// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;

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
        internal X509Certificates(X509Certificate2 primary, X509Certificate2 secondary = null)
        {
            Argument.AssertNotNull(primary, nameof(primary));
            Primary = new X509CertificateWithInfo(primary);

            Secondary = secondary == null
                ? null
                : new X509CertificateWithInfo(secondary);
        }

        internal X509Certificates(string primary, string secondary = null)
        {
            Argument.AssertNotNullOrWhiteSpace(primary, nameof(primary));
            Primary = new X509CertificateWithInfo(primary);

            Secondary = secondary == null
                ? null
                : new X509CertificateWithInfo(secondary);
        }

        [JsonConstructor]
#pragma warning disable IDE0051 // Used for deserialization
        private X509Certificates(X509CertificateWithInfo primary, X509CertificateWithInfo secondary = null)
#pragma warning restore IDE0051
        {
            Argument.AssertNotNull(primary, nameof(primary));
            Primary = primary;
            Secondary = secondary;
        }

        /// <summary>
        /// Primary certificate.
        /// </summary>
        [JsonProperty("primary")]
        public X509CertificateWithInfo Primary { get; private set; }

        /// <summary>
        /// Secondary certificate.
        /// </summary>
        [JsonProperty("secondary", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public X509CertificateWithInfo Secondary { get; private set; }
    }
}
