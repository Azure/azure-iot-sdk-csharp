// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    internal class SDKUtils
    {
        private const string ApiVersionProvisioning = "2021-10-01";
        public const string ApiVersionQueryString = CustomHeaderConstants.ApiVersion + "=" + ApiVersionProvisioning;
    }
}
