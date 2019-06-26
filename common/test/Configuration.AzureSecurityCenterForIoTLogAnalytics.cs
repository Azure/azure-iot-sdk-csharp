// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.E2ETests
{
    public static partial class Configuration
    {
        public static class AzureSecurityCenterForIoTLogAnalytics
        {
            public static string AadTenant => GetValue("LA_AAD_TENANT");

            public static string AadAppId => GetValue("LA_AAD_APP_ID");

            public static string AadAppCertificate => GetValue("LA_AAD_APP_CERT_BASE64");

            public static string WorkspacedId => GetValue("LA_WORKSPACE_ID");
        }
    }
}
