// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Client.Extensions;

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
                return csBuilder.ModuleId != null
                    ? new ModuleAuthenticationWithToken(
                        csBuilder.DeviceId,
                        csBuilder.ModuleId,
                        csBuilder.SharedAccessSignature)
                    : (IAuthenticationMethod)new DeviceAuthenticationWithToken(
                        csBuilder.DeviceId,
                        csBuilder.SharedAccessSignature);
            }
            else if (csBuilder.UsingX509Cert)
            {
                return new DeviceAuthenticationWithX509Certificate(csBuilder.DeviceId, csBuilder.Certificate);
            }

            throw new InvalidOperationException($"Unsupported authentication method in '{csBuilder}'.");
        }

        /// <summary>
        /// Creates a <see cref="DeviceAuthenticationWithSharedAccessPolicyKey"/> instance based on the parameters.
        /// </summary>
        /// <param name="deviceId">Device Identifier.</param>
        /// <param name="policyName">Name of the shared access policy to use.</param>
        /// <param name="key">Key associated with the shared access policy.</param>
        /// <returns>A new instance of the <see cref="DeviceAuthenticationWithSharedAccessPolicyKey"/> class.</returns>
        public static IAuthenticationMethod CreateAuthenticationWithSharedAccessPolicyKey(string deviceId, string policyName, string key)
        {
            return new DeviceAuthenticationWithSharedAccessPolicyKey(deviceId, policyName, key);
        }

        /// <summary>
        /// Creates a <see cref="DeviceAuthenticationWithToken"/> instance based on the parameters.
        /// </summary>
        /// <param name="deviceId">Device Identifier.</param>
        /// <param name="token">Security token associated with the device.</param>
        /// <returns>A new instance of the <see cref="DeviceAuthenticationWithToken"/> class.</returns>
        public static IAuthenticationMethod CreateAuthenticationWithToken(string deviceId, string token)
        {
            return new DeviceAuthenticationWithToken(deviceId, token);
        }

        /// <summary>
        /// Creates a <see cref="ModuleAuthenticationWithToken"/> instance based on the parameters.
        /// </summary>
        /// <param name="deviceId">Device Identifier.</param>
        /// <param name="moduleId">Module Identifier.</param>
        /// <param name="token">Security token associated with the device.</param>
        /// <returns>A new instance of the <see cref="ModuleAuthenticationWithToken"/> class.</returns>
        public static IAuthenticationMethod CreateAuthenticationWithToken(string deviceId, string moduleId, string token)
        {
            return new ModuleAuthenticationWithToken(deviceId, moduleId, token);
        }

        /// <summary>
        /// Creates a <see cref="DeviceAuthenticationWithRegistrySymmetricKey"/> instance based on the parameters.
        /// </summary>
        /// <param name="deviceId">Device Identifier.</param>
        /// <param name="key">Key associated with the device in the device registry.</param>
        /// <returns>A new instance of the <see cref="DeviceAuthenticationWithRegistrySymmetricKey"/> class.</returns>
        public static IAuthenticationMethod CreateAuthenticationWithRegistrySymmetricKey(string deviceId, string key)
        {
            return new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, key);
        }

        /// <summary>
        /// Creates a <see cref="ModuleAuthenticationWithRegistrySymmetricKey"/> instance based on the parameters.
        /// </summary>
        /// <param name="deviceId">Device Identifier.</param>
        /// <param name="moduleId">Module Identifier.</param>
        /// <param name="key">Key associated with the module in the device registry.</param>
        /// <returns>A new instance of the <see cref="ModuleAuthenticationWithRegistrySymmetricKey"/> class.</returns>
        public static IAuthenticationMethod CreateAuthenticationWithRegistrySymmetricKey(string deviceId, string moduleId, string key)
        {
            return new ModuleAuthenticationWithRegistrySymmetricKey(deviceId, moduleId, key);
        }
    }
}
