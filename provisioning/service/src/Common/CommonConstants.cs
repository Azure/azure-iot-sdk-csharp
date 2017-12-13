// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Common
{
    internal static class CommonConstants
    {
        // TODO: move these to ConfigProvider
        public const string MediaTypeForDeviceManagementApis = "application/json";
        public const string ContentTypeCharset = "charset=utf8";
        public const string ContentType = MediaTypeForDeviceManagementApis + "; " + ContentTypeCharset;

        // Custom HTTP headers
        public const string IotHubErrorCode = "IotHubErrorCode";

    }
}
