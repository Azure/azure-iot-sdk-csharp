// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    internal class SDKUtils
    {
        // TODO - Since there are breaking changes on Attestation and other data contracts in 2019-01-15 version, can't update to the latest api version until all these changes applied on.
        private const string ApiVersionProvisioningPreview = "2019-02-15-preview";
        public const string ApiVersionQueryString = CustomHeaderConstants.ApiVersion + "=" + ApiVersionProvisioningPreview;
    }
}
