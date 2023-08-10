// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Models
{
    /// <summary> The ArcEnabledDevice onboarding request metadata. </summary>
    public class ArcEnabledDeviceMetadata : ResourceMetadata
    {
        /// <summary> Initializes a new instance of ArcEnabledDeviceMetadata. </summary>
        /// <param name="publicKey"> The Arc for Servers public key. </param>
        public ArcEnabledDeviceMetadata(string publicKey = default)
        {
            PublicKey = publicKey;
            Kind = "ArcEnabledDeviceMetadata";
        }

        /// <summary> The Arc for Servers public key. </summary>
        [JsonProperty(PropertyName = "publicKey")]
        public string PublicKey { get; }
    }
}
