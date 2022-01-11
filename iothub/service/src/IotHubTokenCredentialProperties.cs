// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;

#if !NET451

using System.Threading;
using Azure.Core;
using Microsoft.Azure.Devices.Common;

#endif

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The properties required for authentication to IoT hub using a token credential.
    /// </summary>
    internal class IotHubTokenCrendentialProperties
        : IotHubConnectionProperties
    {
#if !NET451
        private const string TokenType = "Bearer";
        private readonly TokenCredential _credential;
        private readonly object _tokenLock = new object();
        private AccessToken? _cachedAccessToken;
#endif

#if NET451

        public IotHubTokenCrendentialProperties()
        {
            throw new InvalidOperationException("TokenCredential is not supported on NET451");
        }
#else

        public IotHubTokenCrendentialProperties(string hostName, TokenCredential credential) : base(hostName)
        {
            _credential = credential;
        }

#endif

        // The HTTP protocol uses this method to get the bearer token for authentication.
        public override string GetAuthorizationHeader()
        {
#if NET451
            throw new InvalidOperationException($"TokenCredential is not supported on NET451");

#else
            lock (_tokenLock)
            {
                // A new token is generated if it is the first time or the cached token is close to expiry.
                if (!_cachedAccessToken.HasValue
                    || TokenHelper.IsCloseToExpiry(_cachedAccessToken.Value.ExpiresOn))
                {
                    _cachedAccessToken = _credential.GetToken(
                        new TokenRequestContext(CommonConstants.IotHubAadTokenScopes),
                        new CancellationToken());
                }
            }

            return $"{TokenType} {_cachedAccessToken.Value.Token}";
#endif
        }

#pragma warning disable CS1998 // Disabled as we need to throw exception for NET 451.

        // The AMQP protocol uses this method to get a CBS token for authentication.
        public async override Task<CbsToken> GetTokenAsync(Uri namespaceAddress, string appliesTo, string[] requiredClaims)
        {
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#if NET451
            throw new InvalidOperationException($"TokenCredential is not supported on NET451");

#else
            AccessToken token = await _credential.GetTokenAsync(
                new TokenRequestContext(CommonConstants.IotHubAadTokenScopes),
                new CancellationToken()).ConfigureAwait(false);
            return new CbsToken(
               $"{TokenType} {token.Token}",
                TokenType,
                token.ExpiresOn.UtcDateTime);
#endif
        }
    }
}
