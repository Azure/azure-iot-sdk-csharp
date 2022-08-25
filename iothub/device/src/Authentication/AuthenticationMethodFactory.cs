// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Creates an instance of an implementation of <c>IAuthenticationMethod</c> based on known authentication parameters.
    /// </summary>
    internal sealed class AuthenticationMethodFactory
    {
        internal static IAuthenticationMethod GetAuthenticationMethodFromConnectionString(IotHubConnectionString iotHubConnectionString)
        {
            if (iotHubConnectionString.SharedAccessKeyName != null)
            {
                return new DeviceAuthenticationWithSharedAccessPolicyKey(
                    iotHubConnectionString.DeviceId,
                    iotHubConnectionString.SharedAccessKeyName,
                    iotHubConnectionString.SharedAccessKey);
            }
            else if (iotHubConnectionString.SharedAccessKey != null)
            {
                return iotHubConnectionString.ModuleId == null
                    ? new DeviceAuthenticationWithRegistrySymmetricKey(
                        iotHubConnectionString.DeviceId,
                        iotHubConnectionString.SharedAccessKey)
                    : new ModuleAuthenticationWithRegistrySymmetricKey(
                        iotHubConnectionString.DeviceId,
                        iotHubConnectionString.ModuleId,
                        iotHubConnectionString.SharedAccessKey);
            }
            else if (iotHubConnectionString.SharedAccessSignature != null)
            {
                return iotHubConnectionString.ModuleId == null
                    ? new DeviceAuthenticationWithToken(
                        iotHubConnectionString.DeviceId,
                        iotHubConnectionString.SharedAccessSignature)
                    : new ModuleAuthenticationWithToken(
                        iotHubConnectionString.DeviceId,
                        iotHubConnectionString.ModuleId,
                        iotHubConnectionString.SharedAccessSignature);
            }

            throw new ArgumentException($"Unsupported authentication method in '{iotHubConnectionString}'.");
        }
    }
}
