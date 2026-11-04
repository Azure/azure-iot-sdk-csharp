// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    internal class SdkUtils
    {
        // This client targets the stable GA API only; older and preview API versions are not selectable.
        private const string ApiVersionProvisioning = "2026-11-01";
        public const string ApiVersionQueryString = CustomHeaderConstants.ApiVersion + "=" + ApiVersionProvisioning;
    }
}
