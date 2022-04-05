// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

namespace Microsoft.Azure.Devices.Tests
{
    /// <summary>
    /// Implementation of TokenCredential class for unit tests.
    /// </summary>
    public class TestTokenCredential : TokenCredential
    {
        public const string TokenValue = "token";
        private DateTimeOffset _expiry;

        public TestTokenCredential(DateTimeOffset expiry)
        {
            _expiry = expiry;
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new AccessToken(TokenValue, _expiry);
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(GetToken(requestContext, cancellationToken));
        }
    }
}
