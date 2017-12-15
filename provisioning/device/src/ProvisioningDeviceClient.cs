// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Allows devices to use the Device Provisioning Service.
    /// </summary>
    public class ProvisioningDeviceClient
    {
        private readonly string _globalDeviceEndpoint;
        private readonly string _idScope;
        private readonly ProvisioningTransportHandler _transport;
        private readonly SecurityProvider _security;

        /// <summary>
        /// Stores product information that will be appended to the user agent string that is sent to IoT Hub.
        /// </summary>
        public string ProductInfo { get; set; }

        /// <summary>
        /// Creates an instance of the Device Provisioning Client.
        /// </summary>
        /// <param name="globalDeviceEndpoint">The GlobalDeviceEndpoint for the Device Provisioning Service.</param>
        /// <param name="idScope">The IDScope for the Device Provisioning Service.</param>
        /// <param name="securityProvider">The security provider instance.</param>
        /// <param name="transport">The type of transport (e.g. HTTP, AMQP, MQTT).</param>
        /// <returns>An instance of the ProvisioningDeviceClient</returns>
        public static ProvisioningDeviceClient Create(
            string globalDeviceEndpoint,
            string idScope, 
            SecurityProvider securityProvider, 
            ProvisioningTransportHandler transport)
        {
            if (securityProvider is SecurityProviderX509)
            {
                CertificateInstaller.EnsureChainIsInstalled(
                    ((SecurityProviderX509)securityProvider).GetAuthenticationCertificateChain());
            }

            return new ProvisioningDeviceClient(globalDeviceEndpoint, idScope, securityProvider, transport);
        }

        private ProvisioningDeviceClient(
            string globalDeviceEndpoint,
            string idScope,
            SecurityProvider securityProvider,
            ProvisioningTransportHandler transport)
        {
            _globalDeviceEndpoint = globalDeviceEndpoint;
            _idScope = idScope;
            _transport = transport;
            _security = securityProvider;

            if (Logging.IsEnabled)
            {
                Logging.Associate(this, _security);
                Logging.Associate(this, _transport);
            }
        }

        /// <summary>
        /// Registers the current device using the Device Provisioning Service and assigns it to an IoT Hub.
        /// </summary>
        /// <returns>The registration result.</returns>
        public Task<DeviceRegistrationResult> RegisterAsync()
        {
            return RegisterAsync(CancellationToken.None);
        }

        /// <summary>
        /// Registers the current device using the Device Provisioning Service and assigns it to an IoT Hub.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The registration result.</returns>
        public Task<DeviceRegistrationResult> RegisterAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.RegisterAsync(this, _globalDeviceEndpoint, _idScope, _transport, _security);

            var request = new ProvisioningTransportRegisterMessage(_globalDeviceEndpoint, _idScope, _security);
            request.ProductInfo = ProductInfo;
            return _transport.RegisterAsync(request, cancellationToken);
        }
    }
}
