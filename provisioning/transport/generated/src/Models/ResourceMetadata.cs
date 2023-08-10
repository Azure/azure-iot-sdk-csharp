// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Models
{
    /// <summary>
    /// Resource Metadata object.
    /// </summary>
    public class ResourceMetadata
    {
        /// <summary>
        /// Initializes a new instance of the ResourceMetadata class.
        /// </summary>
        public ResourceMetadata(string kind = default)
        {
            Kind = kind;
        }

        /// <summary>
        /// Discriminator property for ResourceMetadata.
        /// </summary>
        [JsonProperty(PropertyName = "kind")]
        public string Kind { get; set; }
    }
}