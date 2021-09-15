// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.E2ETests
{
    public static partial class TestConfiguration
    {
        public static class AzureSecurityCenterForIoTLogAnalytics
        {
            // The Azure Active directory tenant (The subscription tenant)
            public static string AadTenant => GetValue("MSFT_TENANT_ID");

            // The Azure active directory used for authentication against log analytics
            public static string AadAppId => GetValue("LA_AAD_APP_ID");

            // The certificate used for authentication as a client secret
            public static string AadAppCertificate => GetValue("LA_AAD_APP_CERT_BASE64");

            // The log analytics workspace that is registered to ASC for IoT Security solution
            public static string WorkspacedId => GetValue("LA_WORKSPACE_ID");
        }
    }
}
