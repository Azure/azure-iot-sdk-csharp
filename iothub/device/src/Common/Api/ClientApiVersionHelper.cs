// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    internal class ClientApiVersionHelper
    {
        internal const string ApiVersionQueryPrefix = "api-version=";
        internal const string ApiVersionLatest = "2019-10-01";

        public const string ApiVersionString = ApiVersionLatest;
        public const string ApiVersionQueryString = ApiVersionQueryPrefix + ApiVersionString;
    }
}
