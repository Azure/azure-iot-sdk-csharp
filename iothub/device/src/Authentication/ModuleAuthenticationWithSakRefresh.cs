// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client
{
    // Implementing SAS Token refresh based on a SharedAccessKey (SAK).
    internal class ModuleAuthenticationWithSakRefresh : ModuleAuthenticationWithTokenRefresh
    {
        private readonly string _sharedAccessKey;
        private readonly string _sharedAccessKeyName;

        internal ModuleAuthenticationWithSakRefresh(
            string deviceId,
            string moduleId,
            string sharedAccessKey,
            string sharedAccessKeyName = default,
            TimeSpan sasTokenTimeToLive = default,
            int sasTokenRenewalBuffer = default)
            : base(
                deviceId,
                moduleId,
                sasTokenTimeToLive,
                sasTokenRenewalBuffer)
        {
            _sharedAccessKey = sharedAccessKey ?? throw new ArgumentNullException(nameof(sharedAccessKey));
            _sharedAccessKeyName = sharedAccessKeyName;
        }

        ///<inheritdoc/>
        protected override Task<string> SafeCreateNewTokenAsync(string iotHub, TimeSpan suggestedTimeToLive)
        {
            var builder = new SharedAccessSignatureBuilder()
            {
                Key = _sharedAccessKey,
                TimeToLive = suggestedTimeToLive,
            };

            if (_sharedAccessKeyName == null)
            {
                builder.Target = "{0}/devices/{1}/modules/{2}".FormatInvariant(
                    iotHub,
                    WebUtility.UrlEncode(DeviceId),
                    WebUtility.UrlEncode(ModuleId));
            }
            else
            {
                builder.KeyName = _sharedAccessKeyName;
                builder.Target = iotHub;
            }

            return Task.FromResult(builder.ToSignature());
        }
    }
}
