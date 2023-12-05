// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Discovery.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Security;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Discovery.Client.Samples
{
    /// <summary>
    /// Demonstrates how to onboard a device with the provisioning service with a certificate provided by the discovery service
    /// </summary>
    internal class DiscoveryDeviceClientSample
    {
        private readonly Parameters _parameters;

        public DiscoveryDeviceClientSample(Parameters parameters)
        {
            _parameters = parameters;
        }

        public async Task RunSampleAsync()
        {
            Console.WriteLine("Initializing security using the local TPM...");
            using SecurityProviderTpm security = new SecurityProviderTpmHsm(_parameters.RegistrationId);

            Console.WriteLine("Initializing transport");
            using DiscoveryTransportHandler transport = new DiscoveryTransportHandlerHttp();

            var client = DiscoveryDeviceClient.Create(
                _parameters.DiscoveryDeviceEndpoint,
                security,
                transport);

            Console.WriteLine($"Initialized for registration Id {security.GetRegistrationID()}.");

            Console.WriteLine("Getting nonce for challenge... ");
            byte[] nonce = await client.IssueChallengeAsync();

            Console.WriteLine($"Received nonce");

            OnboardingInfo onboardingInfo = await client.GetOnboardingInfoAsync(nonce);

            Console.WriteLine($"Received endpoint: {onboardingInfo.EdgeProvisioningEndpoint}");

            using SecurityProvider provSecurity = new SecurityProviderX509Certificate(onboardingInfo.ProvisioningCertificate[0], onboardingInfo.ProvisioningCertificate);

            Console.WriteLine("Initializing transport");
            using ProvisioningTransportHandlerHttp provTransport = new ProvisioningTransportHandlerHttp();

            var provisioningEndpoint = onboardingInfo.EdgeProvisioningEndpoint;

            if (_parameters.ProvisioningDeviceEndpoint != null)
            {
                provisioningEndpoint = _parameters.ProvisioningDeviceEndpoint;
            }

            Console.WriteLine($"Initializing porivisioning client with endpoint {provisioningEndpoint}");

            var provClient = ProvisioningDeviceClient.Create(
                provisioningEndpoint,
                provSecurity,
                provTransport);

            DeviceOnboardingResult result = await provClient.OnboardAsync("test");

            Console.WriteLine($"Done onboarding! {result.Id} {result.Result.RegistrationId}");

            if (result.Result.Metadata is HybridComputeMachine hybridComputeMachine)
            {
                Console.WriteLine($"Hybrid compute metadata: " +
                    $"{hybridComputeMachine.ResourceId} " +
                    $"{hybridComputeMachine.TenantId} " +
                    $"{hybridComputeMachine.ArcVirtualMachineId} " +
                    $"{hybridComputeMachine.Location}");
            }
        }
    }
}
