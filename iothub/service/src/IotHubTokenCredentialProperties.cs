// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using System.Threading;

#if !NET451

using Azure.Core;

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
        private const string _tokenType = "jwt";
        private readonly TokenCredential _credential;
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

        public override string GetAuthorizationHeader()
        {
#if NET451
            throw new InvalidOperationException($"TokenCredential is not supported on NET451");

#else
            AccessToken token = _credential.GetToken(new TokenRequestContext(), new CancellationToken());
            return $"Bearer {token.Token}";

#endif
        }

#pragma warning disable CS1998 // Disabled as we need to throw exception for NET 451.

        public async override Task<CbsToken> GetTokenAsync(Uri namespaceAddress, string appliesTo, string[] requiredClaims)
        {
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#if NET451
            throw new InvalidOperationException($"TokenCredential is not supported on NET451");

#else
            AccessToken token = await _credential.GetTokenAsync(new TokenRequestContext(), new CancellationToken()).ConfigureAwait(false);
            return new CbsToken(
                token.Token,
                _tokenType,
                token.ExpiresOn.UtcDateTime);
#endif
        }
    }
}
