// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Authentication method that uses a shared access signature token and allows for token refresh. 
    /// </summary>
    public sealed class DeviceAuthenticationWithTpm : DeviceAuthenticationWithTokenRefresh
    {
        private SecurityClientHsmTpm _securityClient;

        public DeviceAuthenticationWithTpm(
            string deviceId, 
            SecurityClientHsmTpm securityClient) : base(deviceId)
        {
            _securityClient = securityClient ?? throw new ArgumentNullException(nameof(securityClient));
        }

        public DeviceAuthenticationWithTpm(
            string deviceId,
            SecurityClientHsmTpm securityClient,
            int suggestedTimeToLiveSeconds,
            int timeBufferPercentage) : base(deviceId, suggestedTimeToLiveSeconds, timeBufferPercentage)
        {
            _securityClient = securityClient ?? throw new ArgumentNullException(nameof(securityClient));
        }

        protected override Task<string> SafeCreateNewToken(string iotHub, int suggestedTimeToLiveSeconds)
        {
            var builder = new TpmSharedAccessSignatureBuilder(_securityClient)
            {
                TimeToLive = TimeSpan.FromSeconds(suggestedTimeToLiveSeconds),
                Target = "{0}/devices/{1}".FormatInvariant(
                    iotHub,
                    WebUtility.UrlEncode(DeviceId)),
            };

            return Task.FromResult(builder.ToSignature());
        }

        private class TpmSharedAccessSignatureBuilder : SharedAccessSignatureBuilder
        {
            private SecurityClientHsmTpm _securityClient;

            public TpmSharedAccessSignatureBuilder(SecurityClientHsmTpm securityClient)
            {
                _securityClient = securityClient;
            }

            protected override string Sign(string requestString, string key)
            {
                Debug.Assert(key == null);

                byte[] encodedBytes = Encoding.UTF8.GetBytes(requestString);
                byte[] hmac = _securityClient.Sign(encodedBytes);
                return Convert.ToBase64String(hmac);
            }
        }
    }
}
