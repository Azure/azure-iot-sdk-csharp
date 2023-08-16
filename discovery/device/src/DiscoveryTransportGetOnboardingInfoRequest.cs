// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Discovery.Client.Transport
{
    /// <summary>
    /// Represents a Provisioning issue challenge message.
    /// </summary>
    public class DiscoveryTransportGetOnboardingInfoRequest : DiscoveryTransportRequest
    {
        /// <summary>
        /// 
        /// </summary>
        public string Nonce { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="globalDeviceEndpoint"></param>
        /// <param name="security"></param>
        /// <param name="nonce"></param>
        public DiscoveryTransportGetOnboardingInfoRequest(string globalDeviceEndpoint, SecurityProviderTpm security, string nonce) : base(globalDeviceEndpoint, security)
        {
            Nonce = nonce;
        }
    }
}
