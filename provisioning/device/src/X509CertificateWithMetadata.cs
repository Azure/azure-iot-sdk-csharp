// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// The certificate in PEM format and its associated metadata.
    /// </summary>
    public class X509CertificateWithMetadata
    {
        /// <summary>
        /// The certificate in PEM format.
        /// </summary>
        [JsonProperty(PropertyName = "certificate", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Certificate { get; internal set; }

        /// <summary>
        /// The certificate metadata.
        /// </summary>
        [JsonProperty(PropertyName = "metadata", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public X509CertificateMetadata Metadata { get; internal set; }
    }
}
