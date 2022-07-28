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
        private readonly IotHubConnectionInfo _connInfo;

        public DeviceAuthenticationWithSakRefresh(
            string deviceId,
            IotHubConnectionInfo connectionInfo) : base(deviceId)
        {
            _connInfo = connectionInfo ?? throw new ArgumentNullException(nameof(connectionInfo));
        }

        internal DeviceAuthenticationWithSakRefresh(
            string deviceId,
            IotHubConnectionInfo connectionInfo,
            TimeSpan sasTokenTimeToLive,
            int sasTokenRenewalBuffer,
            bool disposeWithClient)
            : base(deviceId, (int)sasTokenTimeToLive.TotalSeconds, sasTokenRenewalBuffer, disposeWithClient)
        {
            _connInfo = connectionInfo ?? throw new ArgumentNullException(nameof(connectionInfo));
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
                    Key = _connInfo.SharedAccessKey,
                    TimeToLive = TimeSpan.FromSeconds(suggestedTimeToLive),
                };

                if (_connInfo.SharedAccessKeyName == null)
                {
                    builder.Target = "{0}/devices/{1}".FormatInvariant(
                        iotHub,
                        WebUtility.UrlEncode(DeviceId));
                }
                else
                {
                    builder.KeyName = _connInfo.SharedAccessKeyName;
                    builder.Target = _connInfo.Audience;
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
