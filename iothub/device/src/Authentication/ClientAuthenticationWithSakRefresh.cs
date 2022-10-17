﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client
{
    // Implementing SAS Token refresh based on a SharedAccessKey (SAK).
    internal class ClientAuthenticationWithSakRefresh : ClientAuthenticationWithTokenRefresh
    {
        private readonly string _sharedAccessKey;
        private readonly string _sharedAccessKeyName;

        internal ClientAuthenticationWithSakRefresh(
            string sharedAccessKey,
            string deviceId,
            string moduleId = default,
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
            if (Logging.IsEnabled)
                Logging.Enter(this, iotHub, suggestedTimeToLive, nameof(SafeCreateNewTokenAsync));

            var builder = new SharedAccessSignatureBuilder
            {
                Key = _sharedAccessKey,
                TimeToLive = suggestedTimeToLive,
            };

            if (_sharedAccessKeyName == null)
            {
                builder.Target = ModuleId == default
                    ? "{0}/devices/{1}".FormatInvariant(
                        iotHub,
                        WebUtility.UrlEncode(DeviceId))
                    : "{0}/devices/{1}/modules/{2}".FormatInvariant(
                        iotHub,
                        WebUtility.UrlEncode(DeviceId),
                        WebUtility.UrlEncode(ModuleId));
            }
            else
            {
                builder.KeyName = _sharedAccessKeyName;
                builder.Target = iotHub;
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, iotHub, suggestedTimeToLive, nameof(SafeCreateNewTokenAsync));

            return Task.FromResult(builder.ToSignature());
        }
    }
}
