// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Transport
{
    public class MqttTransportClient : ProvisioningTransportClient
    {
        public TransportFallbackType FallbackType { get; private set; }

        public MqttTransportClient(TransportFallbackType transportFallbackType = TransportFallbackType.TcpWithWebSocketFallback)
        {
            FallbackType = transportFallbackType;
        }

        public override Task<ProvisioningRegistrationResult> RegisterAsync(string globalDeviceEndpoint, string idScope, ProvisioningSecurityClient securityClient, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public override Task CloseAsync()
        {
            throw new System.NotImplementedException();
        }
    }
}
