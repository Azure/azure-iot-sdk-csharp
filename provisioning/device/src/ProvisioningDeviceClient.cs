// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Authentication;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Allows devices to use the Device Provisioning Service.
    /// </summary>
    public class ProvisioningDeviceClient
    {
        private readonly string _globalDeviceEndpoint;
        private readonly string _idScope;
        private readonly AuthenticationProvider _authentication;
        private readonly ProvisioningClientOptions _options;

        /// <summary>
        /// Creates an instance of the Device Provisioning Client.
        /// </summary>
        /// <param name="globalDeviceEndpoint">The GlobalDeviceEndpoint for the Device Provisioning Service.</param>
        /// <param name="idScope">The IDScope for the Device Provisioning Service.</param>
        /// <param name="authenticationProvider">The security provider instance.</param>
        /// <param name="options">The options that allow configuration of the provisioning device client instance during initialization.</param>
        /// <returns>An instance of the ProvisioningDeviceClient</returns>
        public static ProvisioningDeviceClient Create(
            string globalDeviceEndpoint,
            string idScope,
            AuthenticationProvider authenticationProvider,
            ProvisioningClientOptions options = default)
        {
            if (authenticationProvider is AuthenticationProviderX509 x509Auth)
            {
                CertificateInstaller.EnsureChainIsInstalled(x509Auth.GetAuthenticationCertificateChain());
            }

            return new ProvisioningDeviceClient(globalDeviceEndpoint, idScope, authenticationProvider, options);
        }

        private ProvisioningDeviceClient(
            string globalDeviceEndpoint,
            string idScope,
            AuthenticationProvider authenticationProvider,
            ProvisioningClientOptions options = default)
        {
            _globalDeviceEndpoint = globalDeviceEndpoint;
            _idScope = idScope;
            _options = options;
            _authentication = authenticationProvider;

            Logging.Associate(this, _authentication);
            Logging.Associate(this, _options);
        }

        /// <summary>
        /// Stores product information that will be appended to the user agent string that is sent to IoT hub.
        /// </summary>
        public string ProductInfo { get; set; }

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
        public Task<DeviceRegistrationResult> RegisterAsync(ProvisioningRegistrationAdditionalData data, CancellationToken cancellationToken = default)
        {
            Logging.RegisterAsync(this, _globalDeviceEndpoint, _idScope, _options, _authentication);

            var request = new ProvisioningTransportRegisterRequest(_globalDeviceEndpoint, _idScope, _authentication, data?.JsonData)
            {
                ProductInfo = ProductInfo,
            };

            return _options.ProvisioningTransportHandler.RegisterAsync(request, cancellationToken);
        }
    }
}
