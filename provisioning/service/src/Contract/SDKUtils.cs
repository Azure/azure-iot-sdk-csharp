// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    internal class SDKUtils
    {
        private const string ApiVersionProvisioningPreview = "2017-11-15";
        public const string ApiVersionQueryString = CustomHeaderConstants.ApiVersion + "=" + ApiVersionProvisioningPreview;
    }
}
