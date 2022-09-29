// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Security;

namespace Microsoft.Azure.Devices.Provisioning.Client.Samples
{
    /// <summary>
    /// Demonstrates how to register a device with the device provisioning service using a certificate, and then
    /// use the registration information to authenticate to IoT Hub.
    /// </summary>
    internal class ProvisioningDeviceClientSample
    {
        private readonly Parameters _parameters;

        public ProvisioningDeviceClientSample(Parameters parameters)
        {
            _parameters = parameters;
        }

        public async Task RunSampleAsync()
        {
            Console.WriteLine("Initializing security using the local TPM...");
            using var security = new AuthenticationProviderTpmHsm(_parameters.RegistrationId);

            Console.WriteLine($"Initializing the device provisioning client...");

            ProvisioningClientOptions clientOptions = _parameters.GetClientOptions();
            var provClient = new ProvisioningDeviceClient(
                _parameters.GlobalDeviceEndpoint,
                _parameters.IdScope,
                security,
                clientOptions);

            Console.WriteLine($"Initialized for registration Id {security.GetRegistrationId()}.");

            Console.WriteLine("Registering with the device provisioning service... ");
            DeviceRegistrationResult result = await provClient.RegisterAsync();

            Console.WriteLine($"Registration status: {result.Status}.");
            if (result.Status != ProvisioningRegistrationStatusType.Assigned)
            {
                Console.WriteLine($"Registration status did not assign a hub, so exiting this sample.");
                return;
            }

            Console.WriteLine($"Device {result.DeviceId} registered to {result.AssignedHub}.");

            Console.WriteLine("Creating TPM authentication for IoT Hub...");
            var auth = new DeviceAuthenticationWithTpm(result.DeviceId, security);

            Console.WriteLine($"Testing the provisioned device with IoT Hub...");
            var hubOptions = new IotHubClientOptions(_parameters.GetHubTransportSettings());
            using var iotClient = new IotHubDeviceClient(result.AssignedHub, auth, hubOptions);

            Console.WriteLine("Sending a telemetry message...");
            var message = new OutgoingMessage("TestMessage");
            await iotClient.SendEventAsync(message);

            await iotClient.CloseAsync();
            Console.WriteLine("Finished.");
        }
    }
}
