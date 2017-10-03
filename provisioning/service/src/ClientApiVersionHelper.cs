// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    class ClientApiVersionHelper
    {
        const string ApiVersionProvisioningPreview = "2017-08-31-preview";

        public const string ApiVersionQueryPrefix = "api-version=";
        public const string ApiVersionQueryString = ApiVersionQueryPrefix + ApiVersionProvisioningPreview;

    }
}
