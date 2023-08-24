// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Discovery.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Discovery.Client
{
    /// <summary>
    /// Allows devices to use the Device Provisioning Service.
    /// </summary>
    public class DiscoveryDeviceClient
    {
        private readonly string _globalDeviceEndpoint;
        private readonly DiscoveryTransportHandler _transport;
        private readonly SecurityProvider _security;

        /// <summary>
        /// Creates an instance of the Device Provisioning Client.
        /// </summary>
        /// <param name="globalDeviceEndpoint">The GlobalDeviceEndpoint for the Device Provisioning Service.</param>
        /// <param name="securityProvider">The security provider instance.</param>
        /// <param name="transport">The type of transport (e.g. HTTP, AMQP, MQTT).</param>
        /// <returns>An instance of the ProvisioningDeviceClient</returns>
        public static DiscoveryDeviceClient Create(
            string globalDeviceEndpoint,
            SecurityProvider securityProvider,
            DiscoveryTransportHandler transport)
        {
            return new DiscoveryDeviceClient(globalDeviceEndpoint, securityProvider, transport);
        }

        private DiscoveryDeviceClient(
            string globalDeviceEndpoint,
            SecurityProvider securityProvider,
            DiscoveryTransportHandler transport)
        {
            _globalDeviceEndpoint = globalDeviceEndpoint;
            _transport = transport;
            _security = securityProvider;

            Logging.Associate(this, _security);
            Logging.Associate(this, _transport);
        }

        /// <summary>
        /// Stores product information that will be appended to the user agent string that is sent to IoT hub.
        /// </summary>
        public string ProductInfo { get; set; }

        /// <summary>
        /// Gets challenge string to decrypt to onboard device
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">Security provider must be TPM</exception>
        public Task<string> IssueChallengeAsync(CancellationToken cancellationToken = default)
        {
            if (_security is SecurityProviderTpm securityProviderTpm)
            {
                var request = new DiscoveryTransportIssueChallengeRequest(
                    _globalDeviceEndpoint,
                    securityProviderTpm)
                {
                    ProductInfo = ProductInfo,
                };

                return _transport.IssueChallengeAsync(request, cancellationToken);
            }
            else
            {
                throw new NotSupportedException($"{nameof(_security)} must be of type {nameof(SecurityProviderTpm)}");
            }
        }

        /// <summary>
        /// Gets challenge string to decrypt to onboard device
        /// </summary>
        /// <param name="nonce"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">Security provider must be TPM</exception>
        public Task<OnboardingInfo> GetOnboardingInfoAsync(string nonce, CancellationToken cancellationToken = default)
        {
            if (_security is SecurityProviderTpm securityProviderTpm)
            {
                var request = new DiscoveryTransportGetOnboardingInfoRequest(
                    _globalDeviceEndpoint,
                    securityProviderTpm,
                    nonce)
                {
                    ProductInfo = ProductInfo,
                };

                return _transport.GetOnboardingInfoAsync(request, cancellationToken);
            }
            else
            {
                throw new NotSupportedException($"{nameof(_security)} must be of type {nameof(SecurityProviderTpm)}");
            }
        }
    }
}
