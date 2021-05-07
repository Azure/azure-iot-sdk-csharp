// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class SimpleTemperatureControllerSampleNew
    {
        private static readonly Random s_random = new();

        private readonly DeviceClient _deviceClient;
        private readonly ILogger _logger;

        public SimpleTemperatureControllerSampleNew(DeviceClient deviceClient, ILogger logger)
        {
            _deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient), $"{nameof(deviceClient)} cannot be null.");

            if (logger == null)
            {
                using ILoggerFactory loggerFactory = LoggerFactory.Create(builer => builer.AddConsole());
                _logger = loggerFactory.CreateLogger<SimpleTemperatureControllerSampleNew>();
            }
            else
            {
                _logger = logger;
            }
        }

        public async Task PerformOperationsAsync(CancellationToken cancellationToken)
        {
            // Send telemetry "workingSet".
            long workingSet = Process.GetCurrentProcess().PrivateMemorySize64 / 1024;
            using var message = new TelemetryMessage
            {
                MessageId = s_random.Next().ToString(),
                Telemetry = { ["workingSet"] = workingSet },
            };
            await _deviceClient.SendTelemetryAsync(message, cancellationToken);
            _logger.LogDebug($"Telemetry: Sent - {message.Telemetry.GetSerializedString()} in KB.");

            // Retrieve the device's properties.
            ClientProperties properties = await _deviceClient.GetClientPropertiesAsync(cancellationToken);

            // Verify if the device has previously reported a value for property "serialNumber".
            // If the expected value has not been previously reported then report it.
            string serialNumber = "SR-12345";
            if (!properties.TryGetValue("serialNumber", out string serialNumberReported)
                || serialNumberReported != serialNumber)
            {
                var propertiesToBeUpdated = new ClientPropertyCollection
                {
                    ["serialNumber"] = serialNumber
                };
                ClientPropertiesUpdateResponse updateResponse = await _deviceClient
                    .UpdateClientPropertiesAsync(propertiesToBeUpdated, cancellationToken);
                _logger.LogDebug($"Property: Update - {propertiesToBeUpdated.GetSerializedString()} in KB," +
                    $" version = {updateResponse.Version}.");
            }

            // Subscribe and respond to event for writable property "targetHumidity".
            await _deviceClient.SubscribeToWritablePropertiesEventAsync(
                async (writableProperties, userContext) =>
                {
                    string propertyName = "targetHumidity";
                    if (!writableProperties.TryGetValue(propertyName, out double targetHumidityRequested))
                    {
                        _logger.LogDebug($"Property: Update - Received a property update" +
                            $" which is not implemented.\n{writableProperties.GetSerializedString()}");
                        return;
                    }

                    var propertyPatch = new ClientPropertyCollection();
                    propertyPatch.Add(
                        propertyName,
                        targetHumidityRequested,
                        StatusCodes.OK,
                        writableProperties.Version,
                        "The operation completed successfully.");

                    ClientPropertiesUpdateResponse updateResponse = await _deviceClient
                        .UpdateClientPropertiesAsync(propertyPatch, cancellationToken);
                    _logger.LogDebug($"Property: Update - \"{propertyPatch.GetSerializedString()}\"," +
                        $" version = {updateResponse.Version} is complete.");
                },
                null,
                cancellationToken);

            // Subscribe and respond to command "reboot".
            await _deviceClient.SubscribeToCommandsAsync(
                async (commandRequest, userContext) =>
                {
                    try
                    {
                        switch (commandRequest.CommandName)
                        {
                            case "reboot":
                                int delay = commandRequest.GetData<int>();
                                _logger.LogDebug($"Command: Received - Rebooting thermostat" +
                                    $" (resetting temperature reading to 0°C after {delay} seconds).");

                                await Task.Delay(TimeSpan.FromSeconds(delay));
                                _logger.LogDebug($"Command: Rebooting thermostat (resetting temperature" +
                                    $" reading to 0°C after {delay} seconds) has {StatusCodes.OK}.");

                                return new CommandResponse(StatusCodes.OK);

                            default:
                                _logger.LogWarning($"Received a command request that isn't" +
                                    $" implemented - command name = {commandRequest.CommandName}");
                                return new CommandResponse(StatusCodes.NotFound);
                        }
                    }
                    catch (JsonReaderException ex)
                    {
                        _logger.LogError($"Command input is invalid: {ex.Message}.");
                        return new CommandResponse(StatusCodes.BadRequest);
                    }
                },
                null,
                cancellationToken);

            Console.ReadKey();
        }
    }
}
