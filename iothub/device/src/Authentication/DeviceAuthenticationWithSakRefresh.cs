// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client
{
    // Implementing SAS Token refresh based on a SharedAccessKey (SAK).
    internal class DeviceAuthenticationWithSakRefresh : DeviceAuthenticationWithTokenRefresh
    {
        private readonly string _sharedAccessKey;
        private readonly string _sharedAccessKeyName;

        internal DeviceAuthenticationWithSakRefresh(
            string deviceId,
            string sharedAccessKey,
            string sharedAccessKeyName = default,
            TimeSpan sasTokenTimeToLive = default,
            int sasTokenRenewalBuffer = default,
            bool disposeWithClient = true)
            : base(
                  deviceId,
                  sasTokenTimeToLive,
                  sasTokenRenewalBuffer,
                  disposeWithClient)
        {
            _sharedAccessKey = sharedAccessKey ?? throw new ArgumentNullException(nameof(sharedAccessKey));
            _sharedAccessKeyName = sharedAccessKeyName;
        }

        ///<inheritdoc/>
        protected override Task<string> SafeCreateNewTokenAsync(string iotHub, TimeSpan suggestedTimeToLive)
        {
            try
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
                    builder.Target = "{0}/devices/{1}".FormatInvariant(
                        iotHub,
                        WebUtility.UrlEncode(DeviceId));
                }
                else
                {
                    builder.KeyName = _sharedAccessKeyName;
                    builder.Target = iotHub;
                }

                return Task.FromResult(builder.ToSignature());
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, iotHub, suggestedTimeToLive, nameof(SafeCreateNewTokenAsync));
            }
        }
    }
}
