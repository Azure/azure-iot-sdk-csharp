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
        private readonly ClientConfiguration _clientConfiguration;

        internal ModuleAuthenticationWithSakRefresh(
            string deviceId,
            string moduleId,
            ClientConfiguration clientConfiguration,
            TimeSpan sasTokenTimeToLive = default,
            int sasTokenRenewalBuffer = default,
            bool disposeWithClient = true)
            : base(deviceId,
                  moduleId,
                  sasTokenTimeToLive,
                  sasTokenRenewalBuffer,
                  disposeWithClient)
        {
            _clientConfiguration = clientConfiguration ?? throw new ArgumentNullException(nameof(clientConfiguration));
        }

        ///<inheritdoc/>
        protected override Task<string> SafeCreateNewToken(string iotHub, TimeSpan suggestedTimeToLive)
        {
            var builder = new SharedAccessSignatureBuilder()
            {
                Key = _clientConfiguration.SharedAccessKey,
                TimeToLive = suggestedTimeToLive,
            };

            if (_clientConfiguration.SharedAccessKeyName == null)
            {
                builder.Target = "{0}/devices/{1}/modules/{2}".FormatInvariant(
                    iotHub,
                    WebUtility.UrlEncode(DeviceId),
                    WebUtility.UrlEncode(ModuleId));
            }
            else
            {
                builder.KeyName = _clientConfiguration.SharedAccessKeyName;
                builder.Target = _clientConfiguration.IotHubHostName;
            }

            return Task.FromResult(builder.ToSignature());
        }
    }
}
