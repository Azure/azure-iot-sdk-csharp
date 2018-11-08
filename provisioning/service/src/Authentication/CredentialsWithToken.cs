// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Authentication class which uses the given Shared Access Signature token
    /// </summary>
    public class CredentialsWithToken: ProvisioningServiceClientCredentials
    {
        private string _sasToken;

        /// <summary>
        /// Public constructor taking in the Shared Access Signature
        /// </summary>
        /// <param name="sasToken"></param>
        public CredentialsWithToken(string sasToken)
        {
            _sasToken = sasToken;
        }

        /// <summary>
        /// Returns the Shared Access Signature for Authentication
        /// </summary>
        /// <returns></returns>
        public override Task<string> GetSasToken()
        {
            return Task.FromResult(_sasToken);
        }
    }
}
