// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Used to describe the X509 primary and secondary certificate
    /// </summary>
    public class X509Certificates
    {
        /// <summary>
        /// Primary certificate.
        /// </summary>
        [JsonProperty(PropertyName = "primary")]
        public X509CertificateWithInfo Primary { get; set; }

        /// <summary>
        /// Secondary certificate.
        /// </summary>
        [JsonProperty(PropertyName = "secondary")]
        public X509CertificateWithInfo Secondary { get; set; }
    }
}
