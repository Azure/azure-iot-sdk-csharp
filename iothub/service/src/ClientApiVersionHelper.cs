// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Holds the API version numbers required in data-plane calls to the service
    /// </summary>
    internal class ClientApiVersionHelper
    {
        private const string ApiVersionQueryPrefix = "api-version=";

        // The preview branch uses a GA api version because the preview features this SDK support are already in public preview and is available on certain Azure regions.
        // For more information on regional availability, check out this page: https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-device-streams-overview#regional-availability
        private const string ApiVersionDefault = "2020-09-30";

        /// <summary>
        /// The default API version to use for all data-plane service calls
        /// </summary>
        public const string ApiVersionQueryString = ApiVersionQueryPrefix + ApiVersionDefault;
    }
}
