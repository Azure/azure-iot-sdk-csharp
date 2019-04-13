// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.E2ETests
{
    public static partial class Configuration
    {
        public static class Oms
        {
            public static string AadTenant => GetValue("OMS_AAD_TENANT");

            public static string AadAppId => GetValue("OMS_AAD_APP_ID");

            public static string AadAppKey => GetValue("OMS_AAD_APP_KEY");

            public static string WorkspacedId => GetValue("OMS_WORKSPACE_ID");
        }
    }
}
