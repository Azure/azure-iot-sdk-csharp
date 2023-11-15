// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.E2ETests
{
    public static partial class TestConfiguration
    {
        public static partial class Discovery
        {
            public static string AzureBearerToken => GetValue("AZURE_ACCESS_TOKEN");

            public static string SubscriptionId => GetValue("DISCOVERY_AZURE_SUBSCRIPTION");

            public static string ResourceGroup1 => GetValue("DISCOVERY_AZURE_RG1");

            public static string ResourceGroup2 => GetValue("DISCOVERY_AZURE_RG2");

            public static string ResourceOwner => GetValue("DISCOVERY_RESOURCE_OWNER", "resource owner");

            public static string RegistrationId => GetValue("DISCOVERY_DEVICE_REGISTRATION_ID", "device");

            // resource names
            // if specified, this resource will be used instead of making a new one

            public static string BootstrapResourceName => GetValue("DISCOVERY_BOOTSTRAP_RESOURCE_NAME", "");

            public static string ProvisioningResourceName => GetValue("DISCOVERY_PROVISIONING_RESOURCE_NAME", "");

            public static string ProvisioningPolicyResourceName => GetValue("DISCOVERY_PROVISIONING_POLICY_RESOURCE_NAME", "");

            // discovery endpoint

            public static string GlobalDeviceEndpoint =>
                GetValue("DPS_GLOBALDISCOVERYENDPOINT", "sta1.eastus.device.discovery.edgeprov-dev.azure.net");
        }
    }
}
