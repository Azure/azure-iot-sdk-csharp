// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Security;
using Microsoft.Azure.Devices.Provisioning.Security.Samples;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Text;
using System.Threading.Tasks;

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
            SecurityProviderTpm security = null;

            try
            {
                if (_parameters.UseTpmSimulator)
                {
                    Console.WriteLine("Starting TPM simulator...");
                    SecurityProviderTpmSimulator.StartSimulatorProcess();
                    security = new SecurityProviderTpmSimulator(_parameters.RegistrationId);
                }
                else
                {
                    Console.WriteLine("Initializing security using the local TPM...");
                    security = new SecurityProviderTpmHsm(_parameters.RegistrationId);
                }

                Console.WriteLine($"Initializing the device provisioning client...");

                using var transport = GetTransportHandler();
                ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(
                    _parameters.GlobalDeviceEndpoint,
                    _parameters.IdScope,
                    security,
                    transport);

                Console.WriteLine($"Initialized for registration Id {security.GetRegistrationID()}.");

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
                IAuthenticationMethod auth = new DeviceAuthenticationWithTpm(result.DeviceId, security);

                Console.WriteLine($"Testing the provisioned device with IoT Hub...");
                using DeviceClient iotClient = DeviceClient.Create(result.AssignedHub, auth, _parameters.TransportType);

                Console.WriteLine("Sending a telemetry message...");
                using var message = new Message(Encoding.UTF8.GetBytes("TestMessage"));
                await iotClient.SendEventAsync(message);
            }
            finally
            {
                if (_parameters.UseTpmSimulator)
                {
                    SecurityProviderTpmSimulator.StopSimulatorProcess();
                }

                security?.Dispose();
            }

            Console.WriteLine("Finished.");
        }

        private ProvisioningTransportHandler GetTransportHandler()
        {
            return _parameters.TransportType switch
            {
                TransportType.Amqp => new ProvisioningTransportHandlerAmqp(),
                TransportType.Amqp_Tcp_Only => new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly),
                TransportType.Amqp_WebSocket_Only => new ProvisioningTransportHandlerAmqp(TransportFallbackType.WebSocketOnly),
                TransportType.Http1 => new ProvisioningTransportHandlerHttp(),
                TransportType.Mqtt => throw new NotSupportedException("MQTT is not supported for TPM"),
                TransportType.Mqtt_Tcp_Only => throw new NotSupportedException("MQTT is not supported for TPM"),
                TransportType.Mqtt_WebSocket_Only => throw new NotSupportedException("MQTT is not supported for TPM"),
                _ => throw new NotSupportedException($"Unsupported transport type {_parameters.TransportType}"),
            };
        }
    }
}
