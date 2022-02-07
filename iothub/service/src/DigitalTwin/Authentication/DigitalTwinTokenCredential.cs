// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using Azure.Core;
using Microsoft.Azure.Devices.Authentication;
using Microsoft.Azure.Devices.Common;

namespace Microsoft.Azure.Devices.DigitalTwin.Authentication
{
    /// <summary>
    /// Allows authentication to the API using a JWT token generated for Azure active directory.
    /// The PnP client is auto generated from swagger and needs to implement a specific class to pass to the protocol layer
    /// unlike the rest of the clients which are hand-written. so, this implementation for authentication is specific to digital twin (Pnp).
    /// </summary>
    internal class DigitalTwinTokenCredential : DigitalTwinServiceClientCredentials
    {
        private readonly object _tokenLock = new object();
        private AccessToken? _cachedAccessToken;
        private readonly TokenCredential _credential;

        public DigitalTwinTokenCredential(TokenCredential credential)
        {
            _credential = credential;
        }

        public override string GetAuthorizationHeader()
        {
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

            return $"Bearer {_cachedAccessToken.Value.Token}";
        }
    }
}
