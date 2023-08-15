// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Discovery.Client.Transport
{
    /// <summary>
    /// Represents a Provisioning issue challenge message.
    /// </summary>
    public class DiscoveryTransportIssueChallengeRequest : DiscoveryTransportRequest
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="globalDeviceEndpoint"></param>
        /// <param name="security"></param>
        public DiscoveryTransportIssueChallengeRequest(string globalDeviceEndpoint, SecurityProviderTpm security) : base(globalDeviceEndpoint, security)
        {
        }
    }
}
