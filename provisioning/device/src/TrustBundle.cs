// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// A collection of trusted root or intermediate certificates associated with one or more device enrollments.
    /// </summary>
    public class TrustBundle
    {
        /// <summary>
        /// The certificates in the trust bundle.
        /// </summary>
        [JsonProperty(PropertyName = "certificates", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<X509CertificateWithMetadata> Certificates { get; internal set; }

        /// <summary>
        /// The trust bundle ID.
        /// A case-insensitive string (up to 128 characters long) of alphanumeric characters plus certain special characters : . _ -. No special characters allowed at start or end.
        /// </summary>
        [JsonProperty(PropertyName = "id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Id { get; internal set; }

        /// <summary>
        /// The DateTime this resource was created in UTC.
        /// </summary>
        [JsonProperty(PropertyName = "createdDateTime", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime CreatedDateTime { get; internal set; }

        /// <summary>
        /// The DateTime this resource was last updated in UTC.
        /// </summary>
        [JsonProperty(PropertyName = "lastModifiedDateTime", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime LastModifiedDateTime { get; internal set; }

        /// <summary>
        /// The ETag of the trust bundle.
        /// </summary>
        [JsonProperty(PropertyName = "etag", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Etag { get; internal set; }
    }
}
