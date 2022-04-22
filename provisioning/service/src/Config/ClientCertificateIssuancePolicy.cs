// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// The issuance policy for client certificates.
    /// </summary>
    public class ClientCertificateIssuancePolicy
    {
        /// <summary>
        /// The CA name that can receice certificate signing requests from DPS on behalf of a device and issue it with an operational client certificate.
        /// </summary>
        [JsonProperty(PropertyName = "certificateAuthorityName", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string CertificateAuthorityName { get; set; }
    }
}
