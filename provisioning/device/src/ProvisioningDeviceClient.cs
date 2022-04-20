// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;

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
            if (securityProvider is SecurityProviderX509 x509securityProvider)
            {
                CertificateInstaller.EnsureChainIsInstalled(x509securityProvider.GetAuthenticationCertificateChain());
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

            Logging.Associate(this, _security);
            Logging.Associate(this, _transport);
        }

        /// <summary>
        /// Registers the current device using the Device Provisioning Service and assigns it to an IoT Hub.
        /// </summary>
        /// <param name="timeout">The maximum amount of time to allow this operation to run for before timing out.</param>
        /// <remarks>
        /// Due to the AMQP library used by this library uses not accepting cancellation tokens, this overload and <see cref="RegisterAsync(ProvisioningRegistrationAdditionalData, TimeSpan)"/>
        /// are the only overloads for this method that allow for a specified timeout to be respected in the middle of an AMQP operation such as opening
        /// the AMQP connection. MQTT and HTTPS connections do not share that same limitation, though.
        /// </remarks>
        /// <returns>The registration result.</returns>
        public Task<DeviceRegistrationResult> RegisterAsync(TimeSpan timeout)
        {
            return RegisterAsync(null, timeout);
        }

        /// <summary>
        /// Registers the current device using the Device Provisioning Service and assigns it to an IoT Hub.
        /// </summary>
        /// <param name="data">
        /// The optional additional data that is passed through to the custom allocation policy webhook if
        /// a custom allocation policy webhook is setup for this enrollment.
        /// </param>
        /// <param name="timeout">The maximum amount of time to allow this operation to run for before timing out.</param>
        /// <remarks>
        /// Due to the AMQP library used by this library uses not accepting cancellation tokens, this overload and <see cref="RegisterAsync(TimeSpan)"/>
        /// are the only overloads for this method that allow for a specified timeout to be respected in the middle of an AMQP operation such as opening
        /// the AMQP connection. MQTT and HTTPS connections do not share that same limitation, though.
        /// </remarks>
        /// <returns>The registration result.</returns>
        public Task<DeviceRegistrationResult> RegisterAsync(ProvisioningRegistrationAdditionalData data, TimeSpan timeout)
        {
            Logging.RegisterAsync(this, _globalDeviceEndpoint, _idScope, _transport, _security);

            var request = new ProvisioningTransportRegisterMessage(_globalDeviceEndpoint, _idScope, _security, data?.JsonData, data?.ClientCertificateSigningRequest)
            {
                ProductInfo = ProductInfo,
            };

            return _transport.RegisterAsync(request, timeout);
        }

        /// <summary>
        /// Registers the current device using the Device Provisioning Service and assigns it to an IoT Hub.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <remarks>
        /// Due to the AMQP library used by this library uses not accepting cancellation tokens, the provided cancellation token will only be checked
        /// for cancellation in between AMQP operations, and not during. In order to have a timeout for this operation that is checked during AMQP operations
        /// (such as opening the connection), you must use <see cref="RegisterAsync(TimeSpan)"/> instead. MQTT and HTTPS connections do not have the same
        /// behavior as AMQP connections in this regard. MQTT and HTTPS connections will check this cancellation token for cancellation during their protocol level operations.
        /// </remarks>
        /// <returns>The registration result.</returns>
        public Task<DeviceRegistrationResult> RegisterAsync(CancellationToken cancellationToken = default)
        {
            Logging.RegisterAsync(this, _globalDeviceEndpoint, _idScope, _transport, _security);

            return RegisterAsync(null, cancellationToken);
        }

        /// <summary>
        /// Registers the current device using the Device Provisioning Service and assigns it to an IoT Hub.
        /// </summary>
        /// <param name="data">
        /// The optional additional data that is passed through to the custom allocation policy webhook if
        /// a custom allocation policy webhook is setup for this enrollment.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <remarks>
        /// Due to the AMQP library used by this library uses not accepting cancellation tokens, the provided cancellation token will only be checked
        /// for cancellation in between AMQP operations, and not during. In order to have a timeout for this operation that is checked during AMQP operations
        /// (such as opening the connection), you must use <see cref="RegisterAsync(ProvisioningRegistrationAdditionalData, TimeSpan)">this overload</see> instead.
        /// MQTT and HTTPS connections do not have the same behavior as AMQP connections in this regard. MQTT and HTTPS connections will check this cancellation
        /// token for cancellation during their protocol level operations.
        /// </remarks>
        /// <returns>The registration result.</returns>
        public Task<DeviceRegistrationResult> RegisterAsync(ProvisioningRegistrationAdditionalData data, CancellationToken cancellationToken = default)
        {
            Logging.RegisterAsync(this, _globalDeviceEndpoint, _idScope, _transport, _security);

            var request = new ProvisioningTransportRegisterMessage(_globalDeviceEndpoint, _idScope, _security, data?.JsonData, data?.ClientCertificateSigningRequest)
            {
                ProductInfo = ProductInfo,
            };

            return _transport.RegisterAsync(request, cancellationToken);
        }
    }
}
