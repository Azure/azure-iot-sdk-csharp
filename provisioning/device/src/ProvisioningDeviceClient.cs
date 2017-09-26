// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Device Provisioning Client
    /// </summary>
    public class ProvisioningDeviceClient
    {
        /// <summary>
        /// Creates an instance of the Device Provisioning Client.
        /// </summary>
        /// <param name="globalDeviceEndpoint">The global device endpoint host-name.</param>
        /// <param name="idScope">The IDScope for the Device Provisioning Service.</param>
        /// <param name="securityClient">The security client instance.</param>
        /// <param name="transport">The type of transport (e.g. HTTP, AMQP, MQTT).</param>
        /// <returns>An instance of the DPSDeviceClient</returns>
        public static ProvisioningDeviceClient Create(
            string globalDeviceEndpoint, 
            string idScope, 
            SecurityClient securityClient, 
            TransportHandler transport)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Registers the current device using the Device Provisioning Service and assigns it to a Hub.
        /// </summary>
        /// <param name="force">Forces the registration by ensuring that all information is rebuilt on the service 
        /// side.</param>
        /// <returns>The DPSRegistrationResult.</returns>
        public async Task<ProvisioningRegistrationResult> RegisterAsync(bool force = false)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Registers the current device using the Device Provisioning Service and assigns it to a Hub.
        /// </summary>
        /// <param name="force">Forces the registration by ensuring that all information is rebuilt on the service 
        /// side.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The DPSRegistrationResult.</returns>
        public async Task<ProvisioningRegistrationResult> RegisterAsync(bool force, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Ensures that the communication channel between the device and the service is gracefully closed.
        /// </summary>
        public async Task CloseAsync()
        {
            // Needed at least for WebSocket close handshake.
            throw new System.NotImplementedException();
        }
    }
}
