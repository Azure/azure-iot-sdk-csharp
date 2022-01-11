// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure;
using Microsoft.Azure.Devices.Authentication;

namespace Microsoft.Azure.Devices.DigitalTwin.Authentication
{
    /// <summary>
    /// Allows authentication to the API using a Shared Access Key provided by custom implementation.
    /// The PnP client is auto generated from swagger and needs to implement a specific class to pass to the protocol layer
    /// unlike the rest of the clients which are hand-written. So, this implementation for authentication is specific to digital twin (PnP).
    /// </summary>
    internal class DigitalTwinSasCredential : DigitalTwinServiceClientCredentials
    {
        private readonly AzureSasCredential _credential;

        public DigitalTwinSasCredential(AzureSasCredential credential)
        {
            _credential = credential;
        }

        public override string GetAuthorizationHeader()
        {
            return _credential.Signature;
        }
    }
}
