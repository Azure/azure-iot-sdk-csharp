// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    internal class SdkUtils
    {
        private const string ApiVersion2019_03_31 = "2019-03-31";
        private const string ApiVersion2026_11_01 = "2026-11-01";
        private const string ApiVersion2026_11_02_Preview = "2026-11-02-preview";

        /// <summary>
        /// Builds the <c>api-version=...</c> query string for the given service API version.
        /// </summary>
        internal static string GetApiVersionQueryString(ServiceVersion serviceVersion)
        {
            string apiVersion;
            switch (serviceVersion)
            {
                case ServiceVersion.V2019_03_31:
                    apiVersion = ApiVersion2019_03_31;
                    break;

                case ServiceVersion.V2026_11_01:
                    apiVersion = ApiVersion2026_11_01;
                    break;

                case ServiceVersion.V2026_11_02_Preview:
                    apiVersion = ApiVersion2026_11_02_Preview;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(serviceVersion), serviceVersion, "Unknown service API version.");
            }

            return CustomHeaderConstants.ApiVersion + "=" + apiVersion;
        }
    }
}
