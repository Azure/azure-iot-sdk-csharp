// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Discovery.Client
{
    /// <summary>
    /// Holds information returned from the getOnboardingInfo endpoint
    /// </summary>
    public class OnboardingInfo
    {
        /// <summary>
        /// The endpoint to reach the provisioning service
        /// </summary>
        public string EdgeProvisioningEndpoint { get; private set; }
        /// <summary>
        /// Credentials to authenticate with the provisioning service
        /// </summary>
        public X509Certificate2 ProvisioningCertificate { get; private set; }

        /// <summary>
        /// Creates an instance of the OnboardingInfo class
        /// </summary>
        /// <param name="edgeProvisioningEndpoint"></param>
        /// <param name="provisioningCertificate"></param>
        public OnboardingInfo(string edgeProvisioningEndpoint, X509Certificate2 provisioningCertificate)
        {
            EdgeProvisioningEndpoint = edgeProvisioningEndpoint;
            ProvisioningCertificate = provisioningCertificate;
        }
    }
}
