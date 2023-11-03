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

            public static string GlobalDeviceEndpoint =>
                GetValue("DPS_GLOBALDISCOVERYENDPOINT", "sta1.eastus.device.discovery.edgeprov-dev.azure.net");

            public static string ConnectionStringInvalidServiceCertificate => GetValue("DISCOVERY_CONNECTION_STRING_INVALIDCERT", string.Empty);

            public static string GlobalDeviceEndpointInvalidServiceCertificate => GetValue("DPS_GLOBALDEVICEENDPOINT_INVALIDCERT", string.Empty);
        }
    }
}
