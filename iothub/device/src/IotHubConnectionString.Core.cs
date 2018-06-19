// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Net;
    using Microsoft.Azure.Amqp;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client.Extensions;

    internal sealed partial class IotHubConnectionString : IAuthorizationProvider, ICbsTokenProvider
    {
        public AuthenticationWithTokenRefresh TokenRefresher
        {
            get;
            private set;
        }

        Task<string> IAuthorizationProvider.GetPasswordAsync()
        {
            if (!string.IsNullOrWhiteSpace(this.SharedAccessSignature))
            {
                return Task.FromResult(this.SharedAccessSignature);
            }

            return this.TokenRefresher.GetTokenAsync(this.Audience);
        }

        // Used by IotHubTokenRefresher.
        async Task<CbsToken> ICbsTokenProvider.GetTokenAsync(Uri namespaceAddress, string appliesTo, string[] requiredClaims)
        {
            string tokenValue;
            DateTime expiresOn;

            if (!string.IsNullOrWhiteSpace(this.SharedAccessSignature))
            {
                tokenValue = this.SharedAccessSignature;
                expiresOn = DateTime.MaxValue;
            }
            else
            {
                tokenValue = await this.TokenRefresher.GetTokenAsync(this.Audience).ConfigureAwait(false);
                expiresOn = this.TokenRefresher.ExpiresOn;
            }

            return new CbsToken(tokenValue, CbsConstants.IotHubSasTokenType, expiresOn);
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
