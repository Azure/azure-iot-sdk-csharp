// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Azure.Core;
using Microsoft.Azure.Devices.Authentication;

namespace Microsoft.Azure.Devices.DigitalTwin.Authentication
{
    /// <summary>
    /// Allows authentication to the API using a JWT token generated for Azure active directory.
    /// The PnP client is auto generated from swagger and needs to implement a specific class to pass to the protocol layer
    /// unlike the rest of the clients which are hand-written. so, this implementation for authentication is specific to digital twin (Pnp).
    /// </summary>
    internal class DigitalTwinTokenCredential : DigitalTwinServiceClientCredentials
    {
        private TokenCredential _credential;

        public DigitalTwinTokenCredential(TokenCredential credential)
        {
            _credential = credential;
        }

        public override string GetAuthorizationHeader()
        {
            AccessToken token = _credential.GetToken(new TokenRequestContext(), new CancellationToken());
            return $"Bearer {token.Token}";
        }
    }
}
