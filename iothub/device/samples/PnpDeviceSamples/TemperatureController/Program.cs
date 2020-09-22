// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.PlugAndPlayConvention;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;

namespace TemperatureController
{
    public class Program
    {
        // DTDL interface used: https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/samples/TemperatureController.json
        private const string ModelId = "dtmi:com:example:TemperatureController;1";

        private static ILogger s_logger;

        public static async Task Main(string[] args)
        {
            // Parse application parameters
            Parameters parameters = null;
            ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed(parsedParams =>
                {
                    parameters = parsedParams;
                })
                .WithNotParsed(errors =>
                {
                    Environment.Exit(1);
                });

            s_logger = InitializeConsoleDebugLogger();
            if (!parameters.Validate())
            {
                throw new ArgumentException("Required parameters are not set. Please recheck required variables by using \"--help\"");
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
            using DeviceClient deviceClient = await SetupDeviceClientAsync(parameters, cts.Token);
            var sample = new TemperatureControllerSample(deviceClient, s_logger);
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

            return loggerFactory.CreateLogger<TemperatureControllerSample>();
        }

        private static async Task<DeviceClient> SetupDeviceClientAsync(Parameters parameters, CancellationToken cancellationToken)
        {
            DeviceClient deviceClient;
            switch (parameters.DeviceSecurityType.ToLowerInvariant())
            {
                case "dps":
                    s_logger.LogDebug($"Initializing via DPS");
                    DeviceRegistrationResult dpsRegistrationResult = await ProvisionDeviceAsync(parameters, cancellationToken);
                    var authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(dpsRegistrationResult.DeviceId, parameters.DeviceSymmetricKey);
                    deviceClient = InitializeDeviceClient(dpsRegistrationResult.AssignedHub, authMethod);
                    break;

                case "connectionstring":
                    s_logger.LogDebug($"Initializing via IoT Hub connection string");
                    deviceClient = InitializeDeviceClient(parameters.PrimaryConnectionString);
                    break;

                default:
                    throw new ArgumentException($"Unrecognized value for device provisioning received: {parameters.DeviceSecurityType}." +
                        $" It should be either \"dps\" or \"connectionString\" (case-insensitive).");
            }
            return deviceClient;
        }

        // Provision a device via DPS, by sending the PnP model Id as DPS payload.
        private static async Task<DeviceRegistrationResult> ProvisionDeviceAsync(Parameters parameters, CancellationToken cancellationToken)
        {
            SecurityProvider symmetricKeyProvider = new SecurityProviderSymmetricKey(parameters.DeviceId, parameters.DeviceSymmetricKey, null);
            ProvisioningTransportHandler mqttTransportHandler = new ProvisioningTransportHandlerMqtt();
            var pdc = ProvisioningDeviceClient.Create(parameters.DpsEndpoint, parameters.DpsIdScope, symmetricKeyProvider, mqttTransportHandler);

            var pnpPayload = new ProvisioningRegistrationAdditionalData
            {
                JsonData = PnpConventionHelper.CreateDpsPayload(ModelId),
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
    }
}
