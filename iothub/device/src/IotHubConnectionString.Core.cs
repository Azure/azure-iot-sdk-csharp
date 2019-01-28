// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Net;
    using Microsoft.Azure.Amqp;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client.Extensions;
    using System.Diagnostics;
    using Microsoft.Azure.Devices.Shared;

    internal sealed partial class IotHubConnectionString : IAuthorizationProvider, ICbsTokenProvider
    {
        public AuthenticationWithTokenRefresh TokenRefresher
        {
            get;
            private set;
        }

        Task<string> IAuthorizationProvider.GetPasswordAsync()
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(IotHubConnectionString)}.{nameof(IAuthorizationProvider.GetPasswordAsync)}");

                Debug.Assert(this.TokenRefresher != null);

                if (!string.IsNullOrWhiteSpace(this.SharedAccessSignature))
                {
                    return Task.FromResult(this.SharedAccessSignature);
                }

                return this.TokenRefresher.GetTokenAsync(this.Audience);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(IotHubConnectionString)}.{nameof(IAuthorizationProvider.GetPasswordAsync)}");
            }
        }

        // Used by IotHubTokenRefresher.
        async Task<CbsToken> ICbsTokenProvider.GetTokenAsync(Uri namespaceAddress, string appliesTo, string[] requiredClaims)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, namespaceAddress, appliesTo, $"{nameof(IotHubConnectionString)}.{nameof(ICbsTokenProvider.GetTokenAsync)}");

                string tokenValue;
                DateTime expiresOn;

                if (!string.IsNullOrWhiteSpace(this.SharedAccessSignature))
                {
                    tokenValue = this.SharedAccessSignature;
                    expiresOn = DateTime.MaxValue;
                }
                else
                {
                    if (Logging.IsEnabled && (TokenRefresher == null)) Logging.Fail(this, $"Cannot create SAS Token: no provider.", nameof(ICbsTokenProvider.GetTokenAsync));
                    Debug.Assert(TokenRefresher != null);
                    tokenValue = await this.TokenRefresher.GetTokenAsync(this.Audience).ConfigureAwait(false);
                    expiresOn = this.TokenRefresher.ExpiresOn;
                }

                return new CbsToken(tokenValue, CbsConstants.IotHubSasTokenType, expiresOn);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, namespaceAddress, appliesTo, $"{nameof(IotHubConnectionString)}.{nameof(ICbsTokenProvider.GetTokenAsync)}");
            }
        }

        public Uri BuildLinkAddress(string path)
        {
            var builder = new UriBuilder(this.AmqpEndpoint)
            {
                Path = path,
            };

            return builder.Uri;
        }
    }
}
