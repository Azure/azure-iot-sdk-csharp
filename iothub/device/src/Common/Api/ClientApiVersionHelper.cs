// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    internal class ClientApiVersionHelper
    {
        internal const string ApiVersionQueryPrefix = "api-version=";
        internal const string ApiVersionLatest = "2019-10-01";

        // The preview API version has been added to enable support for plug and play features.
        // This will be removed once the plug and play service goes GA.
        internal const string ApiVersionPreview = "2020-05-31-preview";

        public const string ApiVersionString = ApiVersionLatest;
        public const string ApiVersionQueryStringLatest = ApiVersionQueryPrefix + ApiVersionString;

        public const string ApiVersionQueryStringPreview = ApiVersionQueryPrefix + ApiVersionPreview;
    }
}
