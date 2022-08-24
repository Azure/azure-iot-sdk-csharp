// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The authentication model for the device; i.e. X.509 certificates, individual client scoped SAS tokens or IoT hub level scoped SAS tokens.
    /// </summary>
    public enum AuthenticationModel
    {
        /// <summary>
        /// This is the authentication model wherein a client uses X.509 certificates to authenticate its identity with IoT hub service.
        /// </summary>
        X509,

        /// <summary>
        /// This is the authentication model where the SAS tokens generated for a client are scoped to the client identity.
        /// For example, myHub.azure-devices.net/devices/device1.
        /// </summary>
        SasIndividual,

        /// <summary>
        /// This is the authentication model where the SAS tokens generated for a client are scoped to IoT hub level; for example, myHub.azure-devices.net
        /// This is generally not as secure as X.509 certificates or individually authenticated client authentication, as it opens up 
        /// the IoT hub service instance to vulnerabilities in case the SAS tokens are compromised.
        /// </summary>
        SasGrouped,
    }
}
