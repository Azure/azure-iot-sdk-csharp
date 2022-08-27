﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The API version used in all service requests.
    /// </summary>
    internal static class ClientApiVersionHelper
    {
        private const string ApiVersionQueryPrefix = "api-version=";
        private const string ApiVersionDefault = "2021-04-12";

        /// <summary>
        /// The API version used in all service requests.
        /// </summary>
        public const string ApiVersionQueryString = ApiVersionQueryPrefix + ApiVersionDefault;
    }
}