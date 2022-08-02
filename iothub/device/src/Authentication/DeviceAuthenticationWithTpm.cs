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
        /// Initializes a new instance of this class with default
        /// time to live of 1 hour and default buffer percentage value of 15.
        /// </summary>
        /// <remarks>
        /// This constructor will create an authentication method instance that will be disposed when its
        /// associated device client instance is disposed. To reuse the authentication method instance across
        /// multiple client instance lifetimes,
        /// use the <see cref="DeviceAuthenticationWithTpm(string, AuthenticationProviderTpm, int, int, bool)"/>
        /// constructor and set <c>disposeWithClient</c> to <c>false</c>.
        /// </remarks>
        /// <param name="deviceId">Device Identifier.</param>
        /// <param name="authenticationProvider">Device authentication provider settings for TPM hardware security modules.</param>
        public DeviceAuthenticationWithTpm(
            string deviceId,
            AuthenticationProviderTpm authenticationProvider)
            : base(deviceId)
        {
            _authProvider = authenticationProvider ?? throw new ArgumentNullException(nameof(authenticationProvider));
        }

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="deviceId">Device Identifier.</param>
        /// <param name="authenticationProvider">Device authentication provider settings for TPM hardware security modules.</param>
        /// <param name="suggestedTimeToLiveSeconds">Token time to live suggested value.</param>
        /// <param name="timeBufferPercentage">Time buffer before expiry when the token should be renewed expressed as percentage of
        /// the time to live. EX: If you want a SAS token to live for 85% of life before proactive renewal, this value should be 15.</param>
        public DeviceAuthenticationWithTpm(
            string deviceId,
            AuthenticationProviderTpm authenticationProvider,
            int suggestedTimeToLiveSeconds,
            int timeBufferPercentage)
            : this(deviceId, authenticationProvider, suggestedTimeToLiveSeconds, timeBufferPercentage, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="deviceId">Device Identifier.</param>
        /// <param name="authenticationProvider">Device authentication provider settings for TPM hardware security modules.</param>
        /// <param name="suggestedTimeToLiveSeconds">Token time to live suggested value.</param>
        /// <param name="timeBufferPercentage">Time buffer before expiry when the token should be renewed expressed as percentage of
        /// the time to live. EX: If you want a SAS token to live for 85% of life before proactive renewal, this value should be 15.</param>
        /// <param name="disposeWithClient "><c>true</c> if the authentication method should be disposed of by the client
        /// when the client using this instance is itself disposed; <c>false</c> if you intend to reuse the authentication method.</param>
        public DeviceAuthenticationWithTpm(
            string deviceId,
            AuthenticationProviderTpm authenticationProvider,
            int suggestedTimeToLiveSeconds,
            int timeBufferPercentage,
            bool disposeWithClient)
            : base(deviceId, suggestedTimeToLiveSeconds, timeBufferPercentage, disposeWithClient)
        {
            _authProvider = authenticationProvider ?? throw new ArgumentNullException(nameof(authenticationProvider));
        }

        ///<inheritdoc/>
        protected override Task<string> SafeCreateNewToken(string audience, int suggestedTimeToLiveSeconds)
        {
            var builder = new TpmSharedAccessSignatureBuilder(_authProvider)
            {
                TimeToLive = TimeSpan.FromSeconds(suggestedTimeToLiveSeconds),
                Target = audience,
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
