// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

namespace Microsoft.Azure.Devices.E2ETests.Discovery
{
    public class BearerTokenCredential : TokenCredential
    {

        /// <param name="token">The bearer access token value.</param>
        /// <param name="dateTimeOffset">The bearer access token expiry date.</param>
        public BearerTokenCredential(string token, DateTimeOffset dateTimeOffset)
        {
            accesstoken = new AccessToken(token, dateTimeOffset);
        }

        private AccessToken accesstoken;

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) => accesstoken;

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) => new ValueTask<AccessToken>(accesstoken);
    }
}
