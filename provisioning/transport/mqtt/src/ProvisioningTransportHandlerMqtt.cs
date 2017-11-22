// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    public class ProvisioningTransportHandlerMqtt : ProvisioningTransportHandler
    {
        public TransportFallbackType FallbackType { get; private set; }

        public ProvisioningTransportHandlerMqtt(
            TransportFallbackType transportFallbackType = TransportFallbackType.TcpWithWebSocketFallback)
        {
            FallbackType = transportFallbackType;
        }

        public override Task<DeviceRegistrationResult> RegisterAsync(
            ProvisioningTransportRegisterMessage message, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
