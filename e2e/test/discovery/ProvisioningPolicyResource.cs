// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.E2ETests.Discovery
{
    public class ProvisioningPolicyResource : AzureResource
    {
        public class ProvisioningPolicyProperties 
        {
            public class ProvisioningPolicyPropertyAuth
            {
                [JsonProperty(PropertyName = "type")]
                public string Type { get; set; }
            }
            [JsonProperty(PropertyName = "bootstrapAuthentication")]
            public ProvisioningPolicyPropertyAuth BootstrapAuthentication { get; set; }
            public class ProvisioningPolicyResourceDetails
            {
                [JsonProperty(PropertyName = "resourceType")]
                public string ResourceType { get; set; }
            }
            [JsonProperty(PropertyName = "resourceDetails")]
            public ProvisioningPolicyResourceDetails ResourceDetails { get; set; }
        }
        public ProvisioningPolicyProperties Properties { get; set; }
    }
}
