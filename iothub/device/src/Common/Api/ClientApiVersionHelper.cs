// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    internal class ClientApiVersionHelper
    {
        internal const string ApiVersionQueryPrefix = "api-version=";

        // The preview branch uses a GA api version because Device Streaming is already in public preview and is available on certain Azure regions.
        // For more information on regional availability, check out this page: https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-device-streams-overview#regional-availability
        internal const string ApiVersionLatest = "2020-09-30";

        public const string ApiVersionString = ApiVersionLatest;
        public const string ApiVersionQueryStringLatest = ApiVersionQueryPrefix + ApiVersionString;
    }
}
