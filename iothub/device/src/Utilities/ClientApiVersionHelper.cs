// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    internal static class ClientApiVersionHelper
    {
        internal const string ApiVersionQueryPrefix = "api-version=";
        internal const string ApiVersionLatest = "2020-09-30";

        public const string ApiVersionString = ApiVersionLatest;
        public const string ApiVersionQueryStringLatest = ApiVersionQueryPrefix + ApiVersionString;
    }
}
