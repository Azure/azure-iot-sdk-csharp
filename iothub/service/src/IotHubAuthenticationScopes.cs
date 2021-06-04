// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// This class contains the most commonly used Azure Active Directory token audience scopes. They should be used as
    /// inputs when setting the authentication scopes in the client options classes.
    /// </summary>
    public static class IotHubAuthenticationScopes
    {
        /// <summary>
        /// The default value for all client options. This value should be used for any public or private cloud other than Azure US Government cloud.
        /// </summary>
        public static IReadOnlyList<string> DefaultAuthenticationScopes = new List<string> { "https://iothubs.azure.net/.default" };

        /// <summary>
        /// This value should be used for Azure US Government cloud.
        /// </summary>
        public static IReadOnlyList<string> AzureGovernmentAuthenticationScopes = new List<string> { "https://iothubs.azure.us/.default" };
    }
}
