// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Provisioning.Security
{
    /// <summary>
    /// The DPS Security Client implementation for DICE/RIoT.
    /// </summary>
    public class SecurityClientDice : SecurityClientX509Certificate
    {
        /// <summary>
        /// Gets the certificate used for TLS authentication by the device from the HSM.
        /// </summary>
        /// <returns>The client certificate used during TLS communications.</returns>
        public override X509Certificate2 GetAuthenticationCertificate()
        {
            throw new System.NotImplementedException();
        }
    }
}
