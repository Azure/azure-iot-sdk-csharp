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
    /// The IoT hub credentials required for authenticating using AAD.
    /// </summary>
    internal class IotHubTokenCredential : IotHubCredential
    {
#if !NET451

        private TokenCredential _credential;

        public IotHubTokenCredential(string hostName, TokenCredential credential) : base(hostName)
        {
            _credential = credential;
        }

#endif

        public override string GetAuthorizationHeader()
        {
#if !NET451
            return _credential.GetToken(new TokenRequestContext(), new CancellationToken()).Token;
#else
            throw new InvalidOperationException($"{nameof(GetAuthorizationHeader)} is not supported on NET451");

#endif
        }

#pragma warning disable CS1998 // Disabled as we need to throw exception for NET 451.

        public async override Task<CbsToken> GetTokenAsync(Uri namespaceAddress, string appliesTo, string[] requiredClaims)
        {
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#if !NET451
            AccessToken token = await _credential.GetTokenAsync(new TokenRequestContext(), new CancellationToken()).ConfigureAwait(false);
            return new CbsToken(
                token.Token,
                CbsConstants.IotHubSasTokenType,
                DateTime.UtcNow.Add(_defaultTokenTimeToLive));
#else
            throw new InvalidOperationException($"{nameof(GetTokenAsync)} is not supported on NET451");
#endif
        }
    }
}
