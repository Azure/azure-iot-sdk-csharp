// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Security
{
    /// <summary>
    /// The Provisioning Security Client implementation for DICE/RIoT.
    /// </summary>
    public class SecurityClientDice : ProvisioningSecurityClientX509Certificate
    {
        public SecurityClientDice(string registrationId) : base(registrationId)
        {
        }

        /// <summary>
        /// Gets the certificate used for TLS authentication by the device from the HSM.
        /// </summary>
        /// <returns>The client certificate used during TLS communications.</returns>
        public override Task<X509Certificate2> GetAuthenticationCertificate()
        {
            throw new System.NotImplementedException();
        }
    }
}
