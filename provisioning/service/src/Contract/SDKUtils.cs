// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    internal class SdkUtils
    {
        // This package is pinned to the preview Device Provisioning Service data-plane API version
        // 2026-11-02-preview. Every request path issues this api-version; there is no runtime selection.
        private const string ApiVersionProvisioning = "2026-11-02-preview";
        public const string ApiVersionQueryString = CustomHeaderConstants.ApiVersion + "=" + ApiVersionProvisioning;
    }
}
