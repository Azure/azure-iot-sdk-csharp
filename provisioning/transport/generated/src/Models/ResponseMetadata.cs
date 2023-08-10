// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Models
{
    /// <summary> The response metadata. </summary>
    public partial class ResponseMetadata
    {
        /// <summary> Initializes a new instance of ResponseMetadata. </summary>
        internal ResponseMetadata()
        {
        }

        /// <summary> Initializes a new instance of ResponseMetadata. </summary>
        /// <param name="kind"> Discriminator property for ResponseMetadata. </param>
        internal ResponseMetadata(string kind = default)
        {
            Kind = kind;
        }

        /// <summary> Discriminator property for ResponseMetadata. </summary>
        [JsonProperty(PropertyName = "kind")]
        internal string Kind { get; set; }
    }
}
