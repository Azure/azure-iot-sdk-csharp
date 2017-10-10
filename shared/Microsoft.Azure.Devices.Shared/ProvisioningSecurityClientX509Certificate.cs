// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// The Device Security Client for X509 authentication (DICE/RIoT authentication).
    /// </summary>
    public abstract class ProvisioningSecurityClientX509Certificate : ProvisioningSecurityClient
    {
        /// <summary>
        /// Initializes a new instance of the SecurityClientX509Certificate class.
        /// </summary>
        /// <param name="registrationId">The Provisioning service Registration ID for this device.</param>
        public ProvisioningSecurityClientX509Certificate(string registrationId) : base(registrationId)
        {
        }

        /// <summary>
        /// Gets the certificate used for TLS authentication by the device from the HSM.
        /// </summary>
        /// <returns>The client certificate used during TLS communications.</returns>
        public abstract Task<X509Certificate2> GetAuthenticationCertificate();
    }
}
