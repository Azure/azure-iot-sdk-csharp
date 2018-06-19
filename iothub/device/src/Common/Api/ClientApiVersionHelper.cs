// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    class ClientApiVersionHelper
    {
        const string ApiVersionQueryPrefix = "api-version=";
        const string ApiVersionNov2016 = "2016-11-14";
        const string ApiVersionJune2017 = "2017-06-30";

#if ENABLE_MODULES_SDK
        const string ApiVersionEdgePublicPreview = "2017-11-08-preview";
        const string ApiVersionLatest = ApiVersionEdgePublicPreview;
#else
        const string ApiVersionLatest = ApiVersionJune2017;
#endif

        public const string ApiVersionString = ApiVersionLatest;

        public const string ApiVersionQueryString = ApiVersionQueryPrefix + ApiVersionString;
    }
}