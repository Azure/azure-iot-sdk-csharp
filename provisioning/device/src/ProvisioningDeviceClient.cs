// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Client.Transport;
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
        private const string DefaultGlobalDeviceEndpoint = "global.azure-devices-provisioning.net";
        private readonly string _idScope;
        private readonly ProvisioningTransportHandler _transport;
        private readonly SecurityClient _security;

        /// <summary>
        /// Stores custom product information that will be appended to the user agent string that is sent to IoT Hub.
        /// </summary>
        public string ProductInfo { get; set; }

        /// <summary>
        /// The global device endpoint.
        /// </summary>
        public string GlobalDeviceEndpoint { get; set; }

        /// <summary>
        /// Creates an instance of the Device Provisioning Client.
        /// </summary>
        /// <param name="idScope">The IDScope for the Device Provisioning Service.</param>
        /// <param name="securityClient">The security client instance.</param>
        /// <param name="transport">The type of transport (e.g. HTTP, AMQP, MQTT).</param>
        /// <returns>An instance of the ProvisioningDeviceClient</returns>
        public static ProvisioningDeviceClient Create(
            string idScope, 
            SecurityClient securityClient, 
            ProvisioningTransportHandler transport)
        {
            if (securityClient is SecurityClientHsmX509)
            {
                CertificateInstaller.EnsureChainIsInstalled(
                    ((SecurityClientHsmX509)securityClient).GetAuthenticationCertificateChain());
            }

            return new ProvisioningDeviceClient(DefaultGlobalDeviceEndpoint, idScope, securityClient, transport);
        }

        private ProvisioningDeviceClient(
            string globalDeviceEndpoint,
            string idScope,
            SecurityClient securityClient,
            ProvisioningTransportHandler transport)
        {
            GlobalDeviceEndpoint = globalDeviceEndpoint;
            _idScope = idScope;
            _transport = transport;
            _security = securityClient;

            if (Logging.IsEnabled)
            {
                Logging.Associate(this, _security);
                Logging.Associate(this, _transport);
            }
        }

        /// <summary>
        /// Registers the current device using the Device Provisioning Service and assigns it to a Hub.
        /// </summary>
        /// <returns>The registration result.</returns>
        public Task<DeviceRegistrationResult> RegisterAsync()
        {
            return RegisterAsync(CancellationToken.None);
        }

        /// <summary>
        /// Registers the current device using the Device Provisioning Service and assigns it to a Hub.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The registration result.</returns>
        public Task<DeviceRegistrationResult> RegisterAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.RegisterAsync(this, GlobalDeviceEndpoint, _idScope, _transport, _security);

            var request = new ProvisioningTransportRegisterMessage(GlobalDeviceEndpoint, _idScope, _security);
            request.ProductInfo = ProductInfo;
            return _transport.RegisterAsync(request, cancellationToken);
        }
    }
}
