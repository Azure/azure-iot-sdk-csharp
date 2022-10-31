// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure.Devices.Logging;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class Program
    {
        // DTDL interface used: https://github.com/Azure/iot-plugandplay-models/blob/main/dtmi/com/example/thermostat-1.json
        private const string ModelId = "dtmi:com:example:Thermostat;1";

        private const string SdkEventProviderPrefix = "Microsoft-Azure-";

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

            // Set up logging
            ILogger logger = InitializeConsoleDebugLogger();

            // Instantiating this seems to do all we need for outputting SDK events to our console log.
            using var skdLog = new ConsoleEventListener(SdkEventProviderPrefix, logger);

            if (!parameters.Validate(logger))
            {
                Console.WriteLine(CommandLine.Text.HelpText.AutoBuild(result, null, null));
                Environment.Exit(1);
            }

            logger.LogInformation("Press Control+C to quit the sample.");
            using var cts = parameters.ApplicationRunningTime.HasValue
                ? new CancellationTokenSource(TimeSpan.FromSeconds(parameters.ApplicationRunningTime.Value))
                : new CancellationTokenSource();

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                logger.LogInformation("Sample execution cancellation requested; will exit.");
            };

            logger.LogDebug($"Set up the device client.");

            try
            {
                using IotHubDeviceClient deviceClient = await SetupDeviceClientAsync(parameters, logger, cts.Token);
                var sample = new ThermostatSample(deviceClient, logger);
                await sample.PerformOperationsAsync(cts.Token);

                // PerformOperationsAsync is designed to run until cancellation has been explicitly requested, either through
                // cancellation token expiration or by Console.CancelKeyPress.
                // As a result, by the time the control reaches the call to close the device client, the cancellation token source would
                // have already had cancellation requested.
                // Hence, if you want to pass a cancellation token to any subsequent calls, a new token needs to be generated.
                // For device client APIs, you can also call them without a cancellation token, which will set a default
                // cancellation timeout of 4 minutes: https://github.com/Azure/azure-iot-sdk-csharp/blob/64f6e9f24371bc40ab3ec7a8b8accbfb537f0fe1/iothub/device/src/InternalClient.cs#L1922
                await deviceClient.CloseAsync(CancellationToken.None);
            }
            catch (OperationCanceledException) { } // User canceled operation
        }

        private static ILogger InitializeConsoleDebugLogger()
        {
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                .AddFilter(level => level >= LogLevel.Debug)
                .AddSystemdConsole(options =>
                {
                    options.TimestampFormat = "[MM/dd/yyyy HH:mm:ss]";
                });
            });

            return loggerFactory.CreateLogger<ThermostatSample>();
        }

        private static async Task<IotHubDeviceClient> SetupDeviceClientAsync(Parameters parameters, ILogger logger, CancellationToken cancellationToken)
        {
            IotHubDeviceClient deviceClient;
            switch (parameters.DeviceSecurityType.ToLowerInvariant())
            {
                case "dps":
                    logger.LogDebug($"Initializing via DPS");
                    DeviceRegistrationResult dpsRegistrationResult = await ProvisionDeviceAsync(parameters, cancellationToken);
                    var authMethod = new ClientAuthenticationWithSharedAccessKeyRefresh(parameters.DeviceSymmetricKey, dpsRegistrationResult.DeviceId);
                    deviceClient = InitializeDeviceClient(dpsRegistrationResult.AssignedHub, authMethod);
                    break;

                case "connectionstring":
                    logger.LogDebug($"Initializing via IoT hub connection string");
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
            var symmetricKeyProvider = new AuthenticationProviderSymmetricKey(parameters.DeviceId, parameters.DeviceSymmetricKey, null);
            var pdc = new ProvisioningDeviceClient(
                parameters.DpsEndpoint,
                parameters.DpsIdScope,
                symmetricKeyProvider);

            var pnpPayload = new RegistrationRequestPayload
            {
                JsonData = $"{{ \"modelId\": \"{ModelId}\" }}",
            };
            return await pdc.RegisterAsync(pnpPayload, cancellationToken);
        }

        // Initialize the device client instance using connection string based authentication
        // and setting the ModelId into ClientOptions.
        private static IotHubDeviceClient InitializeDeviceClient(string deviceConnectionString)
        {
            var options = new IotHubClientOptions
            {
                ModelId = ModelId,
            };

            var deviceClient = new IotHubDeviceClient(deviceConnectionString, options);

            return deviceClient;
        }

        // Initialize the device client instance using symmetric key based authentication, over Mqtt protocol (TCP, with fallback over Websocket) and setting the ModelId into ClientOptions.
        private static IotHubDeviceClient InitializeDeviceClient(string hostname, IAuthenticationMethod authenticationMethod)
        {
            var options = new IotHubClientOptions
            {
                ModelId = ModelId,
            };

            var deviceClient = new IotHubDeviceClient(hostname, authenticationMethod, options);

            return deviceClient;
        }
    }
}
