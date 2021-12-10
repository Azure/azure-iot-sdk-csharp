﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Azure;
using Microsoft.Azure.Devices.Common.Service.Auth;

namespace Microsoft.Azure.Devices.Provisioning.Service.Auth
{
    internal class ProvisioningSasCredential: IAuthorizationHeaderProvider
    {
        private AzureSasCredential _azureSasCredential;

        public ProvisioningSasCredential(AzureSasCredential azureSasCredential)
        {
            _azureSasCredential = azureSasCredential;
        }

        public string GetAuthorizationHeader()
        {
            return _azureSasCredential.Signature;
        }
    }
}
