// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Discovery.Client.Transport
{
    /// <summary>
    /// Represents a discovery onboarding info request.
    /// </summary>
    public class DiscoveryTransportGetOnboardingInfoRequest : DiscoveryTransportRequest
    {
        /// <summary>
        /// Security nonce that is to be signed for authentication.
        /// </summary>
        public byte[] Nonce { get; private set; }

        /// <summary>
        /// Creates a new instance of the DiscoveryTransportGetOnboardingInfoRequest class.
        /// </summary>
        /// <param name="globalDeviceEndpoint">The Global Device Endpoint for this message.</param>
        /// <param name="security">The SecurityProvider used to authenticate the client.</param>
        /// <param name="nonce">Security nonce that is to be signed for authentication.</param>
        public DiscoveryTransportGetOnboardingInfoRequest(string globalDeviceEndpoint, SecurityProviderTpm security, byte[] nonce) : base(globalDeviceEndpoint, security)
        {
            Nonce = nonce;
        }
    }
}
