// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Discovery.Client;
using Microsoft.Azure.Devices.Discovery.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Security;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Samples
{
    /// <summary>
    /// Demonstrates how to register a device with the device provisioning service using a certificate, and then
    /// use the registration information to authenticate to IoT Hub.
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
                _parameters.GlobalDeviceEndpoint,
                security,
                transport);

            Console.WriteLine($"Initialized for registration Id {security.GetRegistrationID()}.");

            Console.WriteLine("Getting nonce for challenge... ");
            string nonce = await client.IssueChallengeAsync();

            Console.WriteLine($"Received nonce");

            string cert = await client.GetOnboardingInfoAsync(nonce);

            Console.WriteLine($"Received cert: {cert}");
        }
    }
}
