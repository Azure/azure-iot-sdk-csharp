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
        private readonly ClientConfiguration _clientConfiguration;

        public DeviceAuthenticationWithSakRefresh(
            string deviceId,
            ClientConfiguration clientConfiguration) : base(deviceId)
        {
            _clientConfiguration = clientConfiguration ?? throw new ArgumentNullException(nameof(clientConfiguration));
        }

        internal DeviceAuthenticationWithSakRefresh(
            string deviceId,
            ClientConfiguration clientConfiguration,
            TimeSpan sasTokenTimeToLive,
            int sasTokenRenewalBuffer,
            bool disposeWithClient)
            : base(deviceId, (int)sasTokenTimeToLive.TotalSeconds, sasTokenRenewalBuffer, disposeWithClient)
        {
            _clientConfiguration = clientConfiguration ?? throw new ArgumentNullException(nameof(clientConfiguration));
        }

        ///<inheritdoc/>
        protected override Task<string> SafeCreateNewToken(string iotHub, int suggestedTimeToLive)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, iotHub, suggestedTimeToLive, nameof(SafeCreateNewToken));

                var builder = new SharedAccessSignatureBuilder
                {
                    Key = _clientConfiguration.SharedAccessKey,
                    TimeToLive = TimeSpan.FromSeconds(suggestedTimeToLive),
                };

                if (_clientConfiguration.SharedAccessKeyName == null)
                {
                    builder.Target = "{0}/devices/{1}".FormatInvariant(
                        iotHub,
                        WebUtility.UrlEncode(DeviceId));
                }
                else
                {
                    builder.KeyName = _clientConfiguration.SharedAccessKeyName;
                    builder.Target = _clientConfiguration.IotHubHostName;
                }

                return Task.FromResult(builder.ToSignature());
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, iotHub, suggestedTimeToLive, nameof(SafeCreateNewToken));
            }
        }
    }
}
