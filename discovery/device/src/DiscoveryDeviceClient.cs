// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Discovery.Client.Transport;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Discovery.Client
{
    /// <summary>
    /// Allows devices to use the Device Discovery Service.
    /// </summary>
    public class DiscoveryDeviceClient
    {
        private readonly string _globalDeviceEndpoint;
        private readonly DiscoveryTransportHandler _transport;
        private readonly SecurityProviderTpm _security;

        /// <summary>
        /// Creates an instance of the Device Discovery Client.
        /// </summary>
        /// <param name="globalDeviceEndpoint">The GlobalDeviceEndpoint for the Device Discovery Service.</param>
        /// <param name="securityProvider">The security provider instance.</param>
        /// <param name="transport">The type of transport (e.g. HTTP, AMQP, MQTT).</param>
        /// <returns>An instance of the DiscoveryDeviceClient</returns>
        /// <exception cref="NotSupportedException">Security provider must be TPM</exception>
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

            if (securityProvider is SecurityProviderTpm securityProviderTpm)
            {
                _security = securityProviderTpm;
            }
            else
            {
                throw new NotSupportedException($"{nameof(_security)} must be of type {nameof(SecurityProviderTpm)}");
            }

            Logging.Associate(this, _security);
            Logging.Associate(this, _transport);
        }

        /// <summary>
        /// Stores product information that will be appended to the user agent string that is sent to IoT hub.
        /// </summary>
        public string ProductInfo { get; set; }

        /// <summary>
        /// Issues challenge to authenticate device.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>nonce for authentication challenge</returns>
        public Task<string> IssueChallengeAsync(CancellationToken cancellationToken = default)
        {
            Logging.IssueChallengeAsync(this, _globalDeviceEndpoint, _transport, _security);

            var request = new DiscoveryTransportIssueChallengeRequest(
                _globalDeviceEndpoint,
                _security)
            {
                ProductInfo = ProductInfo,
            };

            return _transport.IssueChallengeAsync(request, cancellationToken);
        }

        /// <summary>
        /// Gets information necessary to onboard device
        /// </summary>
        /// <param name="nonce"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<OnboardingInfo> GetOnboardingInfoAsync(string nonce, CancellationToken cancellationToken = default)
        {
            Logging.GetOnboardingInfoAsync(this, _globalDeviceEndpoint, _transport, _security);

            var request = new DiscoveryTransportGetOnboardingInfoRequest(
                _globalDeviceEndpoint,
                _security,
                nonce)
            {
                ProductInfo = ProductInfo,
            };

            return _transport.GetOnboardingInfoAsync(request, cancellationToken);
        }
    }
}
