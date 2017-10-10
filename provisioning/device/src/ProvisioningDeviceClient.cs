// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Device Provisioning Client
    /// </summary>
    public class ProvisioningDeviceClient
    {
        private readonly string _globalDeviceEndpoint;
        private readonly string _idScope;
        private readonly ProvisioningTransportClient _transport;
        private readonly ProvisioningSecurityClient _security;

        /// <summary>
        /// Creates an instance of the Device Provisioning Client.
        /// </summary>
        /// <param name="globalDeviceEndpoint">The global device endpoint host-name.</param>
        /// <param name="idScope">The IDScope for the Device Provisioning Service.</param>
        /// <param name="securityClient">The security client instance.</param>
        /// <param name="transport">The type of transport (e.g. HTTP, AMQP, MQTT).</param>
        /// <returns>An instance of the ProvisioningDeviceClient</returns>
        public static ProvisioningDeviceClient Create(
            string globalDeviceEndpoint, 
            string idScope, 
            ProvisioningSecurityClient securityClient, 
            ProvisioningTransportClient transport)
        {
            return new ProvisioningDeviceClient(globalDeviceEndpoint, idScope, securityClient, transport);
        }

        private ProvisioningDeviceClient(
            string globalDeviceEndpoint,
            string idScope,
            ProvisioningSecurityClient securityClient,
            ProvisioningTransportClient transport)
        {
            _globalDeviceEndpoint = globalDeviceEndpoint;
            _idScope = idScope;
            _transport = transport;
            _security = securityClient;
        }

        /// <summary>
        /// Registers the current device using the Device Provisioning Service and assigns it to a Hub.
        /// </summary>
        /// <returns>The registration result.</returns>
        public Task<ProvisioningRegistrationResult> RegisterAsync()
        {
            return RegisterAsync(CancellationToken.None);
        }

        /// <summary>
        /// Registers the current device using the Device Provisioning Service and assigns it to a Hub.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The registration result.</returns>
        public Task<ProvisioningRegistrationResult> RegisterAsync(CancellationToken cancellationToken)
        {
            return _transport.RegisterAsync(_globalDeviceEndpoint, _idScope, _security, cancellationToken);
        }

        /// <summary>
        /// Ensures that the communication channel between the device and the service is gracefully closed.
        /// </summary>
        public Task CloseAsync()
        {
            return _transport.CloseAsync();
        }
    }
}
