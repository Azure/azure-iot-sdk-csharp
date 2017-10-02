// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Contains Certificate and Certifcate properties
    /// </summary>
    public class X509CertificateWithInfo
    {
        /// <summary>
        /// Certificate
        /// </summary>
        [JsonProperty(PropertyName = "certificate")]
        [JsonConverter(typeof(X509CertificateConverter))]
        public X509Certificate2 Certificate { get; set; }

        /// <summary>
        /// Certificate properties.
        /// </summary> 
        [JsonProperty(PropertyName = "info")]
        public X509CertificateInfo Info { get; set; }
    }
}
