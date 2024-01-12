// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.E2ETests.Discovery
{
    public class EdgeOrderPatch
    {
        public class EdgeOrderPatchProperties
        {
            public class EdgeOrderPatchOrderItemDetails
            {
                public class EdgeOrderPatchProductDetails
                {
                    public class EdgeOrderPatchProvisioningDetails
                    {
                        public string ProvisioningArmId { get; set; }
                        public string ProvisioningEndpoint { get; set; }
                        public string SerialNumber { get; set; }
                        public string ReadyToConnectArmId { get; set; }
                    }
                    [JsonProperty(PropertyName = "parentProvisioningDetails")]
                    public EdgeOrderPatchProvisioningDetails ProvisioningDetails { get; set; }
                }
                [JsonProperty(PropertyName = "productDetails")]
                public EdgeOrderPatchProductDetails ProductDetails { get; set; }
            }
            [JsonProperty(PropertyName = "orderItemDetails")]
            public EdgeOrderPatchOrderItemDetails OrderItemDetails { get; set; }
        }
        [JsonProperty(PropertyName = "properties")]
        public EdgeOrderPatchProperties Properties { get; set; }
    }

    public class DiscoveryDevicePatch
    {
        public class DiscoveryDeviceTpmDetails
        {
            [JsonProperty(PropertyName = "endorsementKey")]
            public string EndorsementKey { get; set; }
            [JsonProperty(PropertyName = "signingKey")]
            public string SigningKey { get; set; }
        }
        [JsonProperty(PropertyName = "tpmDetails")]
        public DiscoveryDeviceTpmDetails TpmDetails { get; set; }
        [JsonProperty(PropertyName = "edgeProvisioningEndpoint")]
        public string EdgeProvisioningEndpoint { get; set; }
        [JsonProperty(PropertyName = "registrationId")]
        public string RegistrationId { get; set; }
    }
}
