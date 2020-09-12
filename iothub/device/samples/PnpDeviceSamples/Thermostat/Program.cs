// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;

namespace Thermostat
{
    public class Program
    {
        // DTDL interface used: https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/samples/Thermostat.json
        private const string ModelId = "dtmi:com:example:Thermostat;1";

        // This environment variable indicates if DPS or IoT Hub connection string will be used to provision the device.
        // Expected values: (case-insensitive)
        // "DPS" - The sample will use DPS to provision the device.
        // "connectionString" - The sample will use IoT Hub connection string to provision the device.
        private static readonly string s_deviceSecurityType = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_SECURITY_TYPE");

        // Required if IOTHUB_DEVICE_SECURITY_TYPE is set to "connectionString".
        private static readonly string s_deviceConnectionString = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONNECTION_STRING");

        // Required if IOTHUB_DEVICE_SECURITY_TYPE is set to "DPS".
        private static readonly string s_dpsEndpoint = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_DPS_ENDPOINT");
        private static readonly string s_dpsIdScope = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_DPS_ID_SCOPE");
        private static readonly string s_deviceRegistrationId = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_DPS_DEVICE_ID");
        private static readonly string s_deviceSymmetricKey = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_DPS_DEVICE_KEY");

        private static ILogger s_logger;

        public static async Task Main(string[] _)
        {
            s_logger = InitializeConsoleDebugLogger();

            if (string.IsNullOrWhiteSpace(s_deviceSecurityType))
            {
                throw new ArgumentNullException("Device security type needs to be specified, please set the environment variable \"IOTHUB_DEVICE_SECURITY_TYPE\".");
            }

            s_logger.LogInformation("Press Control+C to quit the sample.");
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                s_logger.LogInformation("Sample execution cancellation requested; will exit.");
            };

            s_logger.LogDebug($"Set up the device client.");
            using DeviceClient deviceClient = await SetupDeviceClientAsync(s_deviceSecurityType, cts.Token);
            var sample = new ThermostatSample(deviceClient, s_logger);
            await sample.PerformOperationsAsync(cts.Token);
        }

        private static ILogger InitializeConsoleDebugLogger()
        {
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                .AddFilter(level => level >= LogLevel.Debug)
                .AddConsole(options =>
                {
                    options.TimestampFormat = "[MM/dd/yyyy HH:mm:ss]";
                });
            });

            return loggerFactory.CreateLogger<ThermostatSample>();
        }

        private static async Task<DeviceClient> SetupDeviceClientAsync(string deviceSecurityType, CancellationToken cancellationToken)
        {
            DeviceClient deviceClient;
            switch (deviceSecurityType.ToLowerInvariant())
            {
                case "dps":
                    s_logger.LogDebug($"Initializing via DPS");
                    if (ValidateArgsForDpsFlow())
                    {
                        DeviceRegistrationResult dpsRegistrationResult = await ProvisionDeviceAsync(cancellationToken);
                        var authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(dpsRegistrationResult.DeviceId, s_deviceSymmetricKey);
                        deviceClient = InitializeDeviceClient(dpsRegistrationResult.AssignedHub, authMethod);
                        break;
                    }
                    throw new ArgumentException("Required environment variables are not set for DPS flow, please recheck your environment.");

                case "connectionstring":
                    s_logger.LogDebug($"Initializing via IoT Hub connection string");
                    if (ValidateArgsForIotHubFlow())
                    {
                        deviceClient = InitializeDeviceClient(s_deviceConnectionString);
                        break;
                    }
                    throw new ArgumentException("Required environment variables are not set for IoT Hub flow, please recheck your environment.");

                default:
                    throw new ArgumentException($"Unrecognized value for IOTHUB_DEVICE_SECURITY_TYPE received: {s_deviceSecurityType}." +
                        $" It should be either \"DPS\" or \"connectionString\" (case-insensitive).");
            }
            return deviceClient;
        }

        // Provision a device via DPS, by sending the PnP model Id as DPS payload.
        private static async Task<DeviceRegistrationResult> ProvisionDeviceAsync(CancellationToken cancellationToken)
        {
            SecurityProvider symmetricKeyProvider = new SecurityProviderSymmetricKey(s_deviceRegistrationId, s_deviceSymmetricKey, null);
            ProvisioningTransportHandler mqttTransportHandler = new ProvisioningTransportHandlerMqtt();
            var pdc = ProvisioningDeviceClient.Create(s_dpsEndpoint, s_dpsIdScope, symmetricKeyProvider, mqttTransportHandler);

            var pnpPayload = new ProvisioningRegistrationAdditionalData
            {
                JsonData = $"{{ \"modelId\": \"{ModelId}\" }}",
            };
            return await pdc.RegisterAsync(pnpPayload, cancellationToken);
        }

        // Initialize the device client instance using connection string based authentication, over Mqtt protocol (TCP, with fallback over Websocket) and setting the ModelId into ClientOptions.
        // This method also sets a connection status change callback, that will get triggered any time the device's connection status changes.
        private static DeviceClient InitializeDeviceClient(string deviceConnectionString)
        {
            var options = new ClientOptions
            {
                ModelId = ModelId,
            };

            var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt, options);
            deviceClient.SetConnectionStatusChangesHandler((status, reason) =>
            {
                s_logger.LogDebug($"Connection status change registered - status={status}, reason={reason}.");
            });

            return deviceClient;
        }

        // Initialize the device client instance using symmetric key based authentication, over Mqtt protocol (TCP, with fallback over Websocket) and setting the ModelId into ClientOptions.
        // This method also sets a connection status change callback, that will get triggered any time the device's connection status changes.
        private static DeviceClient InitializeDeviceClient(string hostname, IAuthenticationMethod authenticationMethod)
        {
            var options = new ClientOptions
            {
                ModelId = ModelId,
            };
            
            var deviceClient = DeviceClient.Create(hostname, authenticationMethod, TransportType.Mqtt, options);
            deviceClient.SetConnectionStatusChangesHandler((status, reason) =>
            {
                s_logger.LogDebug($"Connection status change registered - status={status}, reason={reason}.");
            });

            return deviceClient;
        }

        private static bool ValidateArgsForDpsFlow()
        {
            return !string.IsNullOrWhiteSpace(s_dpsEndpoint)
                && !string.IsNullOrWhiteSpace(s_dpsIdScope)
                && !string.IsNullOrWhiteSpace(s_deviceRegistrationId)
                && !string.IsNullOrWhiteSpace(s_deviceSymmetricKey);
        }

        private static bool ValidateArgsForIotHubFlow()
        {
            return !string.IsNullOrWhiteSpace(s_deviceConnectionString);
        }
    }
}
