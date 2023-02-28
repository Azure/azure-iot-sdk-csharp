// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The properties required for authentication to IoT hub using a token credential.
    /// </summary>
    internal sealed class IotHubTokenCredentialProperties : IotHubConnectionProperties
    {
        private const string TokenType = "Bearer";
        private static readonly string[] s_iotHubAadTokenScopes = new string[] { "https://iothubs.azure.net/.default" };

        private readonly TokenCredential _credential;
        private readonly object _tokenLock = new();
        private AccessToken? _cachedAccessToken;

        // Creates an instance of this class. Provided for unit testing purposes only.
        protected internal IotHubTokenCredentialProperties(string hostName, TokenCredential credential, AccessToken? accessToken) : base(hostName)
        {
            _credential = credential;
            _cachedAccessToken = accessToken;
        }

        public IotHubTokenCredentialProperties(string hostName, TokenCredential credential) : base(hostName)
        {
            _credential = credential;
        }

        // The HTTP protocol uses this method to get the bearer token for authentication.
        public override string GetAuthorizationHeader()
        {
            lock (_tokenLock)
            {
                // A new token is generated if it is the first time or the cached token is close to expiry.
                if (!_cachedAccessToken.HasValue
                    || TokenHelper.IsCloseToExpiry(_cachedAccessToken.Value.ExpiresOn))
                {
                    _cachedAccessToken = _credential.GetToken(
                        new TokenRequestContext(s_iotHubAadTokenScopes),
                        new CancellationToken());
                }
            }

            return $"{TokenType} {_cachedAccessToken.Value.Token}";
        }

        // The AMQP protocol uses this method to get a CBS token for authentication.
        public async override Task<CbsToken> GetTokenAsync(Uri namespaceAddress, string appliesTo, string[] requiredClaims)
        {
            AccessToken token = await _credential
                .GetTokenAsync(new TokenRequestContext(s_iotHubAadTokenScopes), CancellationToken.None)
                .ConfigureAwait(false);

            return new CbsToken(
               $"{TokenType} {token.Token}",
                TokenType,
                token.ExpiresOn.UtcDateTime);
        }
    }
}
