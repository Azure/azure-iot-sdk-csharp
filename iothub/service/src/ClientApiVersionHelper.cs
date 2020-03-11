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
        private const string ApiVersionGA = "2016-02-03";
        private const string ApiVersionDefault = "2019-03-30";
        private const string ApiVersionLimitedAvailability = "2020-03-13";

        // For import/export devices jobs, a new parameter is available in a
        // new api-version, which is only available in a few initial regions.
        // Control access via an environment variable. If a user wishes to try it out,
        // they can set "EnabledStorageIdentity" to "1". Otherwise, the SDK will still
        // default to the latest, broadly-supported api-version used in this SDK.
        internal static bool IsStorageIdentityEnabled => StringComparer.OrdinalIgnoreCase.Equals("1", Environment.GetEnvironmentVariable("EnableStorageIdentity"));

        /// <summary>
        /// The default API version to use for all data-plane service calls
        /// </summary>
        public const string ApiVersionQueryStringDefault = ApiVersionQueryPrefix + ApiVersionDefault;

        public const string ApiVersionQueryStringLimitedAvailability = ApiVersionQueryPrefix + ApiVersionLimitedAvailability;

        public const string ApiVersionQueryStringGA = ApiVersionQueryPrefix + ApiVersionGA;
    }
}
