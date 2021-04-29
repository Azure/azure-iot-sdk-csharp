// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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
            // Retrieve the device's properties.
            ClientProperties properties = await _deviceClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            // Verify if the device has previously reported a value for property "serialNumber".
            // If the expected value has not been previously reported then report it.
            string serialNumber = "SR-12345";
            if (!properties.Contains("serialNumber") || properties.Get<string>("serialNumber") != serialNumber)
            {
                var propertiesToBeUpdated = new ClientPropertyCollection
                {
                    ["serialNumber"] = serialNumber
                };
                await _deviceClient.UpdatePropertiesAsync(propertiesToBeUpdated, cancellationToken);
                _logger.LogDebug($"Property: Update - {propertiesToBeUpdated.GetSerailizedString()} in KB.");
            }

            // Send telemetry "workingSet".
            long workingSet = Process.GetCurrentProcess().PrivateMemorySize64 / 1024;
            using var message = new TelemetryMessage
            {
                MessageId = s_random.Next().ToString(),
                Telemetry = { ["workingSet"] = workingSet },
            };
            await _deviceClient.SendTelemetryAsync(message, cancellationToken);
            _logger.LogDebug($"Telemetry: Sent - {message.Telemetry.GetSerailizedString()} in KB.");

            // Subscribe and respond to event for writable property "targetHumidity".
            await _deviceClient.SubscribeToWritablePropertyEventAsync(async (writableProperties, userContext) =>
            {
                string propertyName = "targetHumidity";
                if (!writableProperties.Contains(propertyName))
                {
                    _logger.LogDebug($"Property: Update - Received a property update which is not implemented.\n{writableProperties.GetSerailizedString()}");
                    return;
                }

                double targetHumidity = writableProperties.GetValue<double>(propertyName);

                var propertyPatch = new ClientPropertyCollection
                {
                    { propertyName, targetHumidity, (int)StatusCode.Completed, writableProperties.Version, "The operation completed successfully." }
                };

                await _deviceClient.UpdatePropertiesAsync(propertyPatch, cancellationToken);
                _logger.LogDebug($"Property: Update - \"{propertyPatch.GetSerailizedString()}\" is complete.");
            },
            null,
            cancellationToken: cancellationToken);

            // Subscribe and respond to command "reboot".
            await _deviceClient.SubscribeToCommandsAsync(async (commandRequest, userContext) =>
            {
                try
                {
                    int delay = commandRequest.GetData<int>();
                    _logger.LogDebug($"Command: Received - Rebooting thermostat (resetting temperature reading to 0°C after {delay} seconds).");

                    await Task.Delay(TimeSpan.FromSeconds(delay));
                    _logger.LogDebug($"Command: Rebooting thermostat (resetting temperature reading to 0°C after {delay} seconds) has {StatusCode.Completed}.");

                    return new CommandResponse((int)StatusCode.Completed);
                }
                catch (JsonReaderException ex)
                {
                    _logger.LogDebug($"Command input is invalid: {ex.Message}.");
                    return new CommandResponse((int)StatusCode.BadRequest);
                }
            },
            null,
            cancellationToken: cancellationToken);

            Console.ReadKey();
        }
    }
}
