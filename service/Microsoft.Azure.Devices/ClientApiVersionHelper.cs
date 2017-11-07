// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    class ClientApiVersionHelper
    {
        const string ApiVersionQueryPrefix = "api-version=";
        const string ApiVersionGA = "2016-02-03";
        const string ApiVersionApril2016 = "2016-04-30";
        const string ApiVersionDMPreview = "2016-04-25-preview";
        const string ApiVersionDMPreview2 = "2016-09-30-preview";
        const string ApiVersionDMGA = "2016-11-14";
        const string ApiVersionJune2017 = "2017-06-30";
        const string ApiVersionJuly2017 = "2017-07-11";
        const string ApiVersionOctober2017 = "2017-10-15";
#if ENABLE_MODULES_SDK
        const string ApiVersionEdgePublicPreview = "2017-11-08-preview";
        const string ApiVersionLatest = ApiVersionEdgePublicPreview;
#else
        const string ApiVersionLatest = ApiVersionOctober2017;
#endif

        public const string ApiVersionQueryString = ApiVersionQueryPrefix + ApiVersionLatest;
        public const string ApiVersionQueryStringGA = ApiVersionQueryPrefix + ApiVersionGA;
    }
}
