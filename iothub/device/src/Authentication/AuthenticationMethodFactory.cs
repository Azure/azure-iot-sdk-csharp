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
            if (credentials.IotHubConnectionString.SharedAccessKeyName != null)
            {
                return new DeviceAuthenticationWithSharedAccessPolicyKey(
                    credentials.IotHubConnectionString.DeviceId,
                    credentials.IotHubConnectionString.SharedAccessKeyName,
                    credentials.IotHubConnectionString.SharedAccessKey);
            }
            else if (credentials.IotHubConnectionString.SharedAccessKey != null)
            {
                return credentials.IotHubConnectionString.ModuleId == null
                    ? new DeviceAuthenticationWithRegistrySymmetricKey(
                        credentials.IotHubConnectionString.DeviceId,
                        credentials.IotHubConnectionString.SharedAccessKey)
                    : new ModuleAuthenticationWithRegistrySymmetricKey(
                        credentials.IotHubConnectionString.DeviceId,
                        credentials.IotHubConnectionString.ModuleId,
                        credentials.IotHubConnectionString.SharedAccessKey);
            }
            else if (credentials.IotHubConnectionString.SharedAccessSignature != null)
            {
                return credentials.IotHubConnectionString.ModuleId == null
                    ? new DeviceAuthenticationWithToken(
                        credentials.IotHubConnectionString.DeviceId,
                        credentials.IotHubConnectionString.SharedAccessSignature)
                    : new ModuleAuthenticationWithToken(
                        credentials.IotHubConnectionString.DeviceId,
                        credentials.IotHubConnectionString.ModuleId,
                        credentials.IotHubConnectionString.SharedAccessSignature);
            }
            else if (credentials.UsingX509Cert)
            {
                return new DeviceAuthenticationWithX509Certificate(
                    credentials.IotHubConnectionString.DeviceId,
                    credentials.Certificate);
            }

            throw new InvalidOperationException($"Unsupported authentication method in '{credentials}'.");
        }

        internal static IAuthenticationMethod GetAuthenticationMethod(IotHubConnectionStringBuilder csBuilder)
        {
            if (csBuilder.SharedAccessKeyName != null)
            {
                return new DeviceAuthenticationWithSharedAccessPolicyKey(
                    csBuilder.DeviceId,
                    csBuilder.SharedAccessKeyName,
                    csBuilder.SharedAccessKey);
            }
            else if (csBuilder.SharedAccessKey != null)
            {
                if (csBuilder.ModuleId != null)
                {
                    return new ModuleAuthenticationWithRegistrySymmetricKey(
                        csBuilder.DeviceId,
                        csBuilder.ModuleId,
                        csBuilder.SharedAccessKey);
                }
                else
                {
                    return new DeviceAuthenticationWithRegistrySymmetricKey(
                        csBuilder.DeviceId,
                        csBuilder.SharedAccessKey);
                }
            }
            else if (csBuilder.SharedAccessSignature != null)
            {
                return csBuilder.ModuleId == null
                    ? (IAuthenticationMethod)new DeviceAuthenticationWithToken(
                        csBuilder.DeviceId,
                        csBuilder.SharedAccessSignature)
                    : new ModuleAuthenticationWithToken(
                        csBuilder.DeviceId,
                        csBuilder.ModuleId,
                        csBuilder.SharedAccessSignature);
            }
            else if (csBuilder.UsingX509Cert)
            {
                return new DeviceAuthenticationWithX509Certificate(csBuilder.DeviceId, csBuilder.Certificate);
            }

            throw new InvalidOperationException($"Unsupported authentication method in '{csBuilder}'.");
        }
    }
}
