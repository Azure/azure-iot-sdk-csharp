// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// The Device Security Client for X509 authentication (DICE/RIoT authentication).
    /// </summary>
    public abstract class SecurityClientX509Certificate : SecurityClient
    {
        /// <summary>
        /// Gets the certificate used for TLS authentication by the device from the HSM.
        /// </summary>
        /// <returns>The client certificate used during TLS communications.</returns>
        public abstract X509Certificate2 GetAuthenticationCertificate();
        
        // TODO: Do we need another public API to extract the chain? (Is installing the chain in Trusted Root cert-store sufficient?)
    }
}
