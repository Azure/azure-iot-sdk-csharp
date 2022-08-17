// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Creates an instance of an implementation of <see cref="IAuthenticationMethod"/> based on known authentication parameters.
    /// </summary>
    internal sealed class AuthenticationMethodFactory
    {
        internal static IAuthenticationMethod GetAuthenticationMethod(IotHubConnectionCredentials credentials)
        {
            if (credentials.SharedAccessKeyName != null)
            {
                return new DeviceAuthenticationWithSharedAccessPolicyKey(
                    credentials.DeviceId,
                    credentials.SharedAccessKeyName,
                    credentials.SharedAccessKey);
            }
            else if (credentials.SharedAccessKey != null)
            {
                return credentials.ModuleId == null
                    ? new DeviceAuthenticationWithRegistrySymmetricKey(
                        credentials.DeviceId,
                        credentials.SharedAccessKey)
                    : new ModuleAuthenticationWithRegistrySymmetricKey(
                        credentials.DeviceId,
                        credentials.ModuleId,
                        credentials.SharedAccessKey);
            }
            else if (credentials.SharedAccessSignature != null)
            {
                return credentials.ModuleId == null
                    ? new DeviceAuthenticationWithToken(
                        credentials.DeviceId,
                        credentials.SharedAccessSignature)
                    : new ModuleAuthenticationWithToken(
                        credentials.DeviceId,
                        credentials.ModuleId,
                        credentials.SharedAccessSignature);
            }
            else if (credentials.UsingX509Cert)
            {
                return new DeviceAuthenticationWithX509Certificate(credentials.DeviceId, credentials.Certificate);
            }

            throw new InvalidOperationException($"Unsupported authentication method in '{credentials}'.");
        }
    }
}
