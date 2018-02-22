// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    class ClientApiVersionHelper
    {
        const string ApiVersionQueryPrefix = "api-version=";
        const string ApiVersionNov2016 = "2016-11-14";
        const string ApiVersionJune2017 = "2017-06-30";
        public const string ApiVersionString = ApiVersionJune2017;

        public const string ApiVersionQueryString = ApiVersionQueryPrefix + ApiVersionString;
    }
}