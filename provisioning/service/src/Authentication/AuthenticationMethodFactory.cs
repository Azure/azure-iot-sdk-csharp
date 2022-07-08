// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Common.Service.Auth
{
    /// <summary>
    /// Creates an instance of an implementation of <see cref="IAuthenticationMethod"/> based on known authentication parameters.
    /// </summary>
    internal sealed class AuthenticationMethodFactory
    {
        internal static IAuthenticationMethod GetAuthenticationMethod(ServiceConnectionStringBuilder iotHubConnectionStringBuilder)
        {
            if (string.IsNullOrWhiteSpace(iotHubConnectionStringBuilder.SharedAccessKey))
            {
                return new ServiceAuthenticationWithSharedAccessPolicyToken(
                    iotHubConnectionStringBuilder.SharedAccessKeyName,
                    iotHubConnectionStringBuilder.SharedAccessSignature);
            }
            else if (string.IsNullOrWhiteSpace(iotHubConnectionStringBuilder.SharedAccessSignature))
            {
                return new ServiceAuthenticationWithSharedAccessPolicyKey(
                    iotHubConnectionStringBuilder.SharedAccessKeyName,
                    iotHubConnectionStringBuilder.SharedAccessKey);
            }

            throw new InvalidOperationException($"Unsupported Authentication Method {iotHubConnectionStringBuilder}");
        }
    }
}
