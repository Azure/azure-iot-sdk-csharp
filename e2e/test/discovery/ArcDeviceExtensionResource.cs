// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.E2ETests.Discovery
{
    public class ArcDeviceExtensionResource
    {
        public class ArcDeviceExtensionResourceProperties
        {
            public string registrationId { get; set; }
            public string onboardingStatus { get; set; } = "Pending";
            public bool discoveryEnabled { get; set; } = true;
            public string provisioningPolicyResourceId { get; set; }

        }
        public ArcDeviceExtensionResourceProperties Properties { get; set; }
    }
}
