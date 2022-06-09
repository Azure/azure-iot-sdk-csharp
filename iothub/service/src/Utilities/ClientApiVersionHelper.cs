﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Holds the API version numbers required in data-plane calls to the service for <see cref="RegistryManager"/>,
    /// <see cref="ServiceClient"/>, and <see cref="JobClient"/>.
    /// </summary>
    internal class ClientApiVersionHelper
    {
        private const string ApiVersionQueryPrefix = "api-version=";
        private const string ApiVersionDefault = "2021-04-12";

        /// <summary>
        /// The default API version query string parameter to use for all data-plane service calls in <see cref="RegistryManager"/>,
        /// <see cref="ServiceClient"/>, and <see cref="JobClient"/>.
        /// </summary>
        public const string ApiVersionQueryString = ApiVersionQueryPrefix + ApiVersionDefault;

        // For DigitalTwinClient which has an autorest-generated protocol layer, the API version is specified at generation time.
        // To update that API version, follow the instructions at ./DigitalTwin/readme.md.
    }
}
