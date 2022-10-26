// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// The client for provisioning devices using Azure Device Provisioning Service.
    /// </summary>
    public class ProvisioningDeviceClient
    {
        private readonly string _globalDeviceEndpoint;
        private readonly string _idScope;
        private readonly AuthenticationProvider _authentication;
        private readonly ProvisioningClientOptions _options;
        private readonly ProvisioningTransportHandler _provisioningTransportHandler;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="globalDeviceEndpoint">The GlobalDeviceEndpoint for the Device Provisioning Service.</param>
        /// <param name="idScope">The IDScope for the Device Provisioning Service.</param>
        /// <param name="authenticationProvider">The security provider instance.</param>
        /// <param name="options">The options that allow configuration of the provisioning device client instance during initialization.</param>
        /// <returns>An instance of the ProvisioningDeviceClient</returns>
        public ProvisioningDeviceClient(
            string globalDeviceEndpoint,
            string idScope,
            AuthenticationProvider authenticationProvider,
            ProvisioningClientOptions options = default)
        {
            if (authenticationProvider is AuthenticationProviderX509 x509Auth)
            {
                CertificateInstaller.EnsureChainIsInstalled(x509Auth.CertificateChain);
            }

            _options = options != default
                ? options.Clone()
                : new();

            _provisioningTransportHandler = _options.TransportSettings is ProvisioningClientMqttSettings
                ? new ProvisioningTransportHandlerMqtt(_options)
                : new ProvisioningTransportHandlerAmqp(_options);

            _globalDeviceEndpoint = globalDeviceEndpoint;
            _idScope = idScope;
            _authentication = authenticationProvider;

            Logging.Associate(this, _authentication);
            Logging.Associate(this, _options);
        }

        /// <summary>
        /// Registers the current device using the Device Provisioning Service and assigns it to an IoT hub.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The registration result.</returns>
        public Task<DeviceRegistrationResult> RegisterAsync(CancellationToken cancellationToken = default)
        {
            Logging.RegisterAsync(this, _globalDeviceEndpoint, _idScope, _options, _authentication);

            return RegisterAsync(null, cancellationToken);
        }

        /// <summary>
        /// Registers the current device using the Device Provisioning Service and assigns it to an IoT hub.
        /// </summary>
        /// <param name="data">
        /// The optional additional data that is passed through to the custom allocation policy webhook if
        /// a custom allocation policy webhook is setup for this enrollment.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The registration result.</returns>
        public Task<DeviceRegistrationResult> RegisterAsync(RegistrationRequestPayload data, CancellationToken cancellationToken = default)
        {
            Logging.RegisterAsync(this, _globalDeviceEndpoint, _idScope, _options, _authentication);

            var request = new ProvisioningTransportRegisterRequest(_globalDeviceEndpoint, _idScope, _authentication, data?.JsonData);

            return _provisioningTransportHandler.RegisterAsync(request, cancellationToken);
        }
    }
}
