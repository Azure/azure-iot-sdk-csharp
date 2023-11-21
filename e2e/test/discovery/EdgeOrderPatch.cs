// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.E2ETests.Discovery
{
    public class EdgeOrderPatch
    {
        public class EdgeOrderPatchProperties
        {
            public class EdgeOrderPatchProvisioningDetails
            {
                public string ProvisioningArmId { get; set; }
                public string ProvisioningEndpoint { get; set; }
                public string SerialNumber { get; set; }
                public string ReadyToConnectArmId { get; set; }
            }
            public EdgeOrderPatchProvisioningDetails ProvisioningDetails { get; set; }
        }
        public EdgeOrderPatchProperties Properties { get; set; }
    }
}
