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
        /// 
        /// </summary>
        public byte[] Nonce { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="globalDeviceEndpoint"></param>
        /// <param name="security"></param>
        /// <param name="nonce"></param>
        public DiscoveryTransportGetOnboardingInfoRequest(string globalDeviceEndpoint, SecurityProviderTpm security, byte[] nonce) : base(globalDeviceEndpoint, security)
        {
            Nonce = nonce;
        }
    }
}
