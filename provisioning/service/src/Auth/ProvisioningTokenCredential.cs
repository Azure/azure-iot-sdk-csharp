// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Threading;
using Azure.Core;
using Microsoft.Azure.Devices.Common.Service.Auth;

namespace Microsoft.Azure.Devices.Provisioning.Service.Auth
{
    internal class ProvisioningTokenCredential : IAuthorizationHeaderProvider
    {
#if !NET451
        private const string _tokenType = "Bearer";
        private readonly TokenCredential _credential;
        private readonly object _tokenLock = new object();
        private AccessToken? _cachedAccessToken;
#endif

#if NET451
        public ProvisioningTokenCredential()
        {
            throw new InvalidOperationException("TokenCredential is not supported on NET451");
        }
#else
        public ProvisioningTokenCredential(TokenCredential credential)
        {
            _credential = credential;
        }
#endif

        // The HTTP protocol uses this method to get the bearer token for authentication.
        public string GetAuthorizationHeader()
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
                        new TokenRequestContext(new string[] { "https://azure-devices-provisioning.net/.default" }),
                        new CancellationToken());
                }
            }

            return $"{_tokenType} {_cachedAccessToken.Value.Token}";
#endif
        }
    }
}
