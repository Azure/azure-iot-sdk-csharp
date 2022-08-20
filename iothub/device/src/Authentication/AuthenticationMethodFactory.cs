// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Creates an instance of an implementation of <see cref="IAuthenticationMethod"/> based on known authentication parameters.
    /// </summary>
    public sealed class AuthenticationMethodFactory
    {
        internal static IAuthenticationMethod GetAuthenticationMethodFromConnectionString(IotHubConnectionCredentials iotHubConnectionCredentials)
        {
            if (iotHubConnectionCredentials.SharedAccessKeyName != null)
            {
                return new DeviceAuthenticationWithSharedAccessPolicyKey(
                    iotHubConnectionCredentials.DeviceId,
                    iotHubConnectionCredentials.SharedAccessKeyName,
                    iotHubConnectionCredentials.SharedAccessKey);
            }
            else if (iotHubConnectionCredentials.SharedAccessKey != null)
            {
                return iotHubConnectionCredentials.ModuleId == null
                    ? new DeviceAuthenticationWithRegistrySymmetricKey(
                        iotHubConnectionCredentials.DeviceId,
                        iotHubConnectionCredentials.SharedAccessKey)
                    : new ModuleAuthenticationWithRegistrySymmetricKey(
                        iotHubConnectionCredentials.DeviceId,
                        iotHubConnectionCredentials.ModuleId,
                        iotHubConnectionCredentials.SharedAccessKey);
            }
            else if (iotHubConnectionCredentials.SharedAccessSignature != null)
            {
                return iotHubConnectionCredentials.ModuleId == null
                    ? new DeviceAuthenticationWithToken(
                        iotHubConnectionCredentials.DeviceId,
                        iotHubConnectionCredentials.SharedAccessSignature)
                    : new ModuleAuthenticationWithToken(
                        iotHubConnectionCredentials.DeviceId,
                        iotHubConnectionCredentials.ModuleId,
                        iotHubConnectionCredentials.SharedAccessSignature);
            }

            throw new InvalidOperationException($"Unsupported authentication method in '{iotHubConnectionCredentials}'.");
        }
    }
}
