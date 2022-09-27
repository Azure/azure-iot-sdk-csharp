// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Authentication;
using Tpm2Lib;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Authentication method that uses a shared access signature token and allows for token refresh.
    /// </summary>
    public sealed class DeviceAuthenticationWithTpm : DeviceAuthenticationWithTokenRefresh
    {
        private readonly AuthenticationProviderTpm _authProvider;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="deviceId">Device Identifier.</param>
        /// <param name="authenticationProvider">Device authentication provider settings for TPM hardware security modules.</param>
        /// <param name="suggestedTimeToLive">
        /// The suggested time to live value for the generated SAS tokens.
        /// The default value is 1 hour.
        /// </param>
        /// <param name="timeBufferPercentage">
        /// The time buffer before expiry when the token should be renewed, expressed as a percentage of the time to live.
        /// The default behavior is that the token will be renewed when it has 15% or less of its lifespan left.
        ///</param>
        public DeviceAuthenticationWithTpm(
            string deviceId,
            AuthenticationProviderTpm authenticationProvider,
            TimeSpan suggestedTimeToLive = default,
            int timeBufferPercentage = default)
            : base(deviceId, suggestedTimeToLive, timeBufferPercentage)
        {
            _authProvider = authenticationProvider ?? throw new ArgumentNullException(nameof(authenticationProvider));
        }

        ///<inheritdoc/>
        protected override Task<string> SafeCreateNewTokenAsync(string iotHub, TimeSpan suggestedTimeToLive)
        {
            var builder = new TpmSharedAccessSignatureBuilder(_authProvider)
            {
                TimeToLive = suggestedTimeToLive,
                Target = "{0}/devices/{1}".FormatInvariant(
                    iotHub,
                    WebUtility.UrlEncode(DeviceId)),
            };

            return Task.FromResult(builder.ToSignature());
        }

        private class TpmSharedAccessSignatureBuilder : SharedAccessSignatureBuilder
        {
            private readonly AuthenticationProviderTpm _authenticationProvider;

            public TpmSharedAccessSignatureBuilder(AuthenticationProviderTpm authenticationProvider)
            {
                _authenticationProvider = authenticationProvider;
            }

            protected override string Sign(string requestString, string key)
            {
                Debug.Assert(key == null);

                byte[] encodedBytes = Encoding.UTF8.GetBytes(requestString);
                byte[] hmac = Array.Empty<byte>();
                try
                {
                    hmac = _authenticationProvider.Sign(encodedBytes);
                }
                catch (Exception ex) when (ex is TssException || ex is TpmException)
                {
                    throw new IotHubClientException(ex.Message, false, ex);
                }

                return Convert.ToBase64String(hmac);
            }
        }
    }
}
