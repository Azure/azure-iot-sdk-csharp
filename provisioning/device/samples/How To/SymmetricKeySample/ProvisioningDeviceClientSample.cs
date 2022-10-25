// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.Devices.Provisioning.Client.Samples
{
    /// <summary>
    /// Demonstrates how to register a device with the device provisioning service using a symmetric key, and then
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
            Console.WriteLine($"Initializing the device provisioning client...");

            // For individual enrollments, the first parameter must be the registration Id, where in the enrollment
            // the device Id is already chosen. However, for group enrollments the device Id can be requested by
            // the device, as long as the key has been computed using that value.
            // Also, the secondary could be included, but was left out for the simplicity of this sample.
            var security = new AuthenticationProviderSymmetricKey(
                _parameters.Id,
                _parameters.PrimaryKey,
                null);

            ProvisioningClientOptions clientOptions = _parameters.GetClientOptions();

            var provClient = new ProvisioningDeviceClient(
                _parameters.GlobalDeviceEndpoint,
                _parameters.IdScope,
                security,
                clientOptions);

            Console.WriteLine($"Initialized for registration Id {security.GetRegistrationId()}.");

            Console.WriteLine("Registering with the device provisioning service...");
            DeviceRegistrationResult result = await provClient.RegisterAsync();

            Console.WriteLine($"Registration status: {result.Status}.");
            if (result.Status != ProvisioningRegistrationStatusType.Assigned)
            {
                Console.WriteLine($"Registration status did not assign a hub, so exiting this sample.");
                return;
            }

            Console.WriteLine($"Device {result.DeviceId} registered to {result.AssignedHub}.");

            Console.WriteLine("Creating symmetric key authentication for IoT Hub...");
            IAuthenticationMethod auth = new ClientAuthenticationWithSharedAccessKeyRefresh(
                security.PrimaryKey,
                result.DeviceId);

            Console.WriteLine($"Testing the provisioned device with IoT Hub...");
            var hubOptions = new IotHubClientOptions(_parameters.GetHubTransportSettings())
            {
            };
            using var iotClient = new IotHubDeviceClient(result.AssignedHub, auth, hubOptions);

            Console.WriteLine("Sending a telemetry message...");
            var message = new TelemetryMessage("TestMessage");
            await iotClient.SendTelemetryAsync(message);

            await iotClient.CloseAsync();
            Console.WriteLine("Finished.");
        }
    }
}
