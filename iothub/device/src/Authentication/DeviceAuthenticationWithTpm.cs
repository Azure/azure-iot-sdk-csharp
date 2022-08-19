// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Authentication;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Authentication method that uses a shared access signature token and allows for token refresh.
    /// </summary>
    public sealed class DeviceAuthenticationWithTpm : DeviceAuthenticationWithTokenRefresh
    {
        private readonly AuthenticationProviderTpm _authProvider;

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// This constructor will create an authentication method instance that will be disposed when its
        /// associated device client instance is disposed. To reuse the authentication method instance across multiple client instance lifetimes
        /// set <paramref name="disposeWithClient"/> to <c>false</c>.
        /// <param name="deviceId">Device Identifier.</param>
        /// <param name="authenticationProvider">Device authentication provider settings for TPM hardware security modules.</param>
        /// <param name="suggestedTimeToLiveSeconds">
        /// The suggested time to live value for the generated SAS tokens.
        /// The default value is 1 hour.
        /// </param>
        /// <param name="timeBufferPercentage">
        /// The time buffer before expiry when the token should be renewed, expressed as a percentage of the time to live.
        /// The default behavior is that the token will be renewed when it has 15% or less of its lifespan left.
        ///</param>
        ///<param name="disposeWithClient ">
        ///<c>true</c> if the authentication method should be disposed of by the client
        /// when the client using this instance is itself disposed; <c>false</c> if you intend to reuse the authentication method.
        /// Defaults to <c>true</c>.</param>
        public DeviceAuthenticationWithTpm(
            string deviceId,
            AuthenticationProviderTpm authenticationProvider,
            int suggestedTimeToLiveSeconds = default,
            int timeBufferPercentage = default,
            bool disposeWithClient = true)
            : base(deviceId, suggestedTimeToLiveSeconds, timeBufferPercentage, disposeWithClient)
        {
            _authProvider = authenticationProvider ?? throw new ArgumentNullException(nameof(authenticationProvider));
        }

        ///<inheritdoc/>
        protected override Task<string> SafeCreateNewToken(string iotHub, int suggestedTimeToLiveSeconds)
        {
            var builder = new TpmSharedAccessSignatureBuilder(_authProvider)
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
            private readonly AuthenticationProviderTpm _authenticationProvider;

            public TpmSharedAccessSignatureBuilder(AuthenticationProviderTpm authenticationProvider)
            {
                _authenticationProvider = authenticationProvider;
            }

            protected override string Sign(string requestString, string key)
            {
                Debug.Assert(key == null);

                byte[] encodedBytes = Encoding.UTF8.GetBytes(requestString);
                byte[] hmac = _authenticationProvider.Sign(encodedBytes);
                return Convert.ToBase64String(hmac);
            }
        }
    }
}
