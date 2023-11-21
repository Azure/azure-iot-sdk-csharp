// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.E2ETests.Discovery
{
    public class AzureBootstrapResource : AzureResource
    {
        public class BootstrapResourceProperties
        {
            public string SiteResourceId { get; set; }
            public int MaximumNumberOfDevicesToOnboard { get; set; }
            public string TokenExpiryDate { get; set; }
        }

        public class BootstrapResourceIdentity
        {
            public string Type { get; set; }
        }

        public BootstrapResourceProperties Properties { get; set; }

        public BootstrapResourceIdentity Identity { get; set; }
    }
}
