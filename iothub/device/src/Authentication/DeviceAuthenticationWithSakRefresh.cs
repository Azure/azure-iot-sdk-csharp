// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    // Implementing SAS Token refresh based on a SharedAccessKey (SAK).
    internal class DeviceAuthenticationWithSakRefresh : DeviceAuthenticationWithTokenRefresh
    {
        private readonly IotHubConnectionString _connectionString;

        public DeviceAuthenticationWithSakRefresh(
            string deviceId,
            IotHubConnectionString connectionString) : base(deviceId)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        internal DeviceAuthenticationWithSakRefresh(
            string deviceId,
            IotHubConnectionString connectionString,
            TimeSpan sasTokenTimeToLive,
            int sasTokenRenewalBuffer,
            bool disposeWithClient)
            : base(deviceId, (int)sasTokenTimeToLive.TotalSeconds, sasTokenRenewalBuffer, disposeWithClient)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        ///<inheritdoc/>
        protected override Task<string> SafeCreateNewToken(string iotHub, int suggestedTimeToLive)
        {
            try
            {
                if (Logging.IsEnabled)
                {
                    Logging.Enter(this, iotHub, suggestedTimeToLive, nameof(SafeCreateNewToken));
                }

                var builder = new SharedAccessSignatureBuilder
                {
                    Key = _connectionString.SharedAccessKey,
                    TimeToLive = TimeSpan.FromSeconds(suggestedTimeToLive),
                };

                if (_connectionString.SharedAccessKeyName == null)
                {
                    builder.Target = "{0}/devices/{1}".FormatInvariant(
                        iotHub,
                        WebUtility.UrlEncode(DeviceId));
                }
                else
                {
                    builder.KeyName = _connectionString.SharedAccessKeyName;
                    builder.Target = _connectionString.Audience;
                }

                return Task.FromResult(builder.ToSignature());
            }
            finally
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, iotHub, suggestedTimeToLive, nameof(SafeCreateNewToken));
                }
            }
        }
    }
}
