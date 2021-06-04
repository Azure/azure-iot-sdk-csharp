// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Options that allow configuration of the JobClient instance during initialization.
    /// </summary>
    public class JobClientOptions
    {
        /// <summary>
        /// The authentication scopes to use when requesting access tokens from Azure Active Directory for authenticating with the
        /// IoT Hub.
        /// </summary>
        /// <remarks>
        /// This value defaults <see cref="IotHubAuthenticationScopes.DefaultAuthenticationScopes"/>, which is used for
        /// any public or private cloud other than Azure US Government cloud.
        /// For Azure US Government cloud users, this value must be set to <see cref="IotHubAuthenticationScopes.AzureGovernmentAuthenticationScopes"/>.
        /// </remarks>
        public IReadOnlyList<string> TokenCredentialAuthenticationScopes { get; set; } = IotHubAuthenticationScopes.DefaultAuthenticationScopes;
    }
}
