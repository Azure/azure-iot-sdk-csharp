// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Devices.Provisioning.Client.Samples
{
    /// <summary>
    /// Demonstrates how to register a device with the device provisioning service using a symmetric key, and then
    /// use the registration information to authenticate to IoT Hub.
    /// </summary>
    internal class ProvisioningDeviceClientSample
    {
        private readonly Parameters _parameters;
        private readonly ILogger _logger;
        private static CancellationTokenSource s_appCancellation;

        public ProvisioningDeviceClientSample(Parameters parameters, ILogger logger)
        {
            _parameters = parameters;
            _logger = logger;
        }

        public async Task RunSampleAsync()
        {
            s_appCancellation = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                s_appCancellation.Cancel();
                _logger.LogWarning("Sample execution cancellation requested; will exit.");
            };

            _logger.LogInformation($"Initializing the device provisioning client...");

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

            _logger.LogInformation($"Initialized for registration Id {security.GetRegistrationId()}.");

            _logger.LogInformation("Registering with the device provisioning service...");
            DeviceRegistrationResult result = await provClient.RegisterAsync();

            _logger.LogInformation($"Registration status: {result.Status}.");
            if (result.Status != ProvisioningRegistrationStatus.Assigned)
            {
                _logger.LogError($"Registration status did not assign a hub. Exiting this sample.");
                return;
            }

            _logger.LogInformation($"Device {result.DeviceId} registered to {result.AssignedHub}.");

            _logger.LogInformation("Creating symmetric key authentication for IoT Hub...");
            IAuthenticationMethod auth = new ClientAuthenticationWithSharedAccessKeyRefresh(
                security.PrimaryKey,
                result.DeviceId);

            _logger.LogInformation($"Testing the provisioned device with IoT Hub...");
            var hubOptions = new IotHubClientOptions(_parameters.GetHubTransportSettings());
            await using var iotHubClient = new IotHubDeviceClient(result.AssignedHub, auth, hubOptions);

            await iotHubClient.OpenAsync();
            _logger.LogInformation("Sending a telemetry message...");
            var message = new TelemetryMessage("TestMessage");
            await iotHubClient.SendTelemetryAsync(message);

            await iotHubClient.CloseAsync();
            _logger.LogInformation("Finished.");
        }
    }
}
