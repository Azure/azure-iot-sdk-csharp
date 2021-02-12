// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;

#if !NET451

using Azure;

#endif

namespace Microsoft.Azure.Devices
{
    internal class IotHubSasCredentialProperties : IotHubConnectionProperties
    {
#if NET451

        public IotHubSasCredentialProperties()
        {
            throw new InvalidOperationException("IotHubSasCredential is not supported on NET451");
        }
#else
        private readonly IotHubSasCredential _credential;

        public IotHubSasCredentialProperties(string hostName, IotHubSasCredential credential) : base(hostName)
        {
            _credential = credential;
        }

#endif

        public override string GetAuthorizationHeader()
        {
#if NET451
            throw new InvalidOperationException($"IotHubSasCredential is not supported on NET451");

#else
            return _credential.SasCredential.Signature;
#endif
        }

        public override Task<CbsToken> GetTokenAsync(Uri namespaceAddress, string appliesTo, string[] requiredClaims)
        {
#if NET451
            throw new InvalidOperationException($"IotHubSasCredential is not supported on NET451");

#else
            var token = new CbsToken(
                _credential.SasCredential.Signature,
                CbsConstants.IotHubSasTokenType,
                _credential.ExpiresOn.UtcDateTime);
            return Task.FromResult(token);
#endif
        }
    }
}
