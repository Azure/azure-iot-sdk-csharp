// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class SimpleTemperaturControllerSampleOld
    {
        private static readonly Random s_random = new();

        private readonly DeviceClient _deviceClient;
        private readonly ILogger _logger;

        public SimpleTemperaturControllerSampleOld(DeviceClient deviceClient, ILogger logger)
        {
            _deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient), $"{nameof(deviceClient)} cannot be null.");

            if (logger == null)
            {
                using ILoggerFactory loggerFactory = LoggerFactory.Create(builer => builer.AddConsole());
                _logger = loggerFactory.CreateLogger<SimpleTemperaturControllerSampleOld>();
            }
            else
            {
                _logger = logger;
            }
        }

        public async Task PerformOperationsAsync(CancellationToken cancellationToken)
        {
            // Retrieve the device's properties.
            Twin properties = await _deviceClient.GetTwinAsync(cancellationToken: cancellationToken);

            // Verify if the device has previously reported a value for property "serialNumber".
            // If the expected value has not been previously reported then report it.
            string serialNumber = "SR-12345";
            if (!properties.Properties.Reported.Contains("serialNumber") || properties.Properties.Reported["serialNumber"] != serialNumber)
            {
                var propertiesToBeUpdated = new TwinCollection
                {
                    ["serialNumber"] = serialNumber
                };
                await _deviceClient.UpdateReportedPropertiesAsync(propertiesToBeUpdated, cancellationToken);
                _logger.LogDebug($"Property: Update - {propertiesToBeUpdated.ToJson()} in KB.");
            }

            // Send telemetry "workingSet".
            long workingSet = Process.GetCurrentProcess().PrivateMemorySize64 / 1024;
            var telemtry = new Dictionary<string, object>
            {
                ["workingSet"] = workingSet,
            };
            using var message = new Message(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(telemtry)))
            {
                MessageId = s_random.Next().ToString(),
                ContentEncoding = "utf-8",
                ContentType= "application/json",
            };

            await _deviceClient.SendEventAsync(message, cancellationToken);
            _logger.LogDebug($"Telemetry: Sent - {JsonConvert.SerializeObject(telemtry)} in KB.");

            // Subscribe and respond to event for writable property "temperatureRange".
            await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(async (desired, userContext) =>
            {
                string propertyName = "temperatureRange";
                if (!desired.Contains(propertyName))
                {
                    _logger.LogDebug($"Property: Update - Received a property update which is not implemented.\n{desired.ToJson()}");
                    return;
                }

                TemperatureRange temperatureRangeDesired = desired[propertyName];

                var propertyPatch = new TwinCollection();
                var temperatureUpdateResponse = new TwinCollection
                {
                    ["value"] = temperatureRangeDesired,
                    ["ac"] = (int)StatusCode.Completed,
                    ["av"] = desired.Version,
                    ["ad"] = "The operation completed successfully."
                };
                propertyPatch[propertyName] = temperatureUpdateResponse;
                
                await _deviceClient.UpdateReportedPropertiesAsync(propertyPatch, cancellationToken);
                _logger.LogDebug($"Property: Update - \"{propertyPatch.ToJson()}\" is complete.");
            },
            cancellationToken,
            cancellationToken);

            // Subscribe and respond to command "reboot".
            await _deviceClient.SetMethodHandlerAsync("reboot", async (commandRequest, userContext) =>
            {
                try
                {
                    int delay = JsonConvert.DeserializeObject<int>(commandRequest.DataAsJson);
                    _logger.LogDebug($"Command: Received - Rebooting thermostat (resetting temperature reading to 0°C after {delay} seconds).");

                    await Task.Delay(TimeSpan.FromSeconds(delay));
                    _logger.LogDebug($"Command: Rebooting thermostat (resetting temperature reading to 0°C after {delay} seconds) has {StatusCode.Completed}.");

                    return new MethodResponse((int)StatusCode.Completed);
                }
                catch (JsonReaderException ex)
                {
                    _logger.LogDebug($"Command input is invalid: {ex.Message}.");
                    return new MethodResponse((int)StatusCode.BadRequest);
                }
            },
            cancellationToken,
            cancellationToken);

            Console.ReadKey();
        }
    }
}
