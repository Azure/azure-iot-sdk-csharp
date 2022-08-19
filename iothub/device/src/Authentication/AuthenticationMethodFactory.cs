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
                return csBuilder.ModuleId == null
                    ? new DeviceAuthenticationWithRegistrySymmetricKey(
                        csBuilder.DeviceId,
                        csBuilder.SharedAccessKey)
                    : new ModuleAuthenticationWithRegistrySymmetricKey(
                        csBuilder.DeviceId,
                        csBuilder.ModuleId,
                        csBuilder.SharedAccessKey);
            }
            else if (csBuilder.SharedAccessSignature != null)
            {
                return csBuilder.ModuleId == null
                    ? new DeviceAuthenticationWithToken(
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
