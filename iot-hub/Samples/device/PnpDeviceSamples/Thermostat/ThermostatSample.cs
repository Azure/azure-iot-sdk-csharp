// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    internal enum StatusCode
    {
        Completed = 200,
        InProgress = 202,
        NotFound = 404,
        BadRequest = 400
    }

    public class ThermostatSample
    {
        private readonly Random _random = new Random();

        private double _temperature = 0d;
        private double _maxTemp = 0d;

        // Dictionary to hold the temperature updates sent over.
        // NOTE: Memory constrained devices should leverage storage capabilities of an external service to store this information and perform computation.
        // See https://docs.microsoft.com/en-us/azure/event-grid/compare-messaging-services for more details.
        private readonly Dictionary<DateTimeOffset, double> _temperatureReadingsDateTimeOffset = new Dictionary<DateTimeOffset, double>();

        private readonly DeviceClient _deviceClient;
        private readonly ILogger _logger;

        // A safe initial value for caching the writable properties version is 1, so the client
        // will process all previous property change requests and initialize the device application
        // after which this version will be updated to that, so we have a high water mark of which version number
        // has been processed.
        private static long s_localWritablePropertiesVersion = 1;

        public ThermostatSample(DeviceClient deviceClient, ILogger logger)
        {
            _deviceClient = deviceClient ?? throw new ArgumentNullException($"{nameof(deviceClient)} cannot be null.");
            _logger = logger ?? LoggerFactory.Create(builer => builer.AddConsole()).CreateLogger<ThermostatSample>();
        }

        public async Task PerformOperationsAsync(CancellationToken cancellationToken)
        {
            // This sample follows the following workflow:
            // -> Set handler to receive and respond to connection status changes.
            // -> Set handler to receive "targetTemperature" updates, and send the received update over reported property.
            // -> Set handler to receive "getMaxMinReport" command, and send the generated report as command response.
            // -> Periodically send "temperature" over telemetry.
            // -> Send "maxTempSinceLastReboot" over property update, when a new max temperature is set.

            _deviceClient.SetConnectionStatusChangesHandler(async (status, reason) =>
            {
                _logger.LogDebug($"Connection status change registered - status={status}, reason={reason}.");

                // Call GetWritablePropertiesAndHandleChangesAsync() to get writable properties from the server once the connection status changes into Connected.
                // This can get back "lost" property updates in a device reconnection from status Disconnected_Retrying or Disconnected.
                if (status == ConnectionStatus.Connected)
                {
                    await GetWritablePropertiesAndHandleChangesAsync();
                }
            });

            _logger.LogDebug($"Set handler to receive \"targetTemperature\" updates.");
            await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(TargetTemperatureUpdateCallbackAsync, _deviceClient, cancellationToken);

            _logger.LogDebug($"Set handler for \"getMaxMinReport\" command.");
            await _deviceClient.SetMethodHandlerAsync("getMaxMinReport", HandleMaxMinReportCommand, _deviceClient, cancellationToken);

            bool temperatureReset = true;
            while (!cancellationToken.IsCancellationRequested)
            {
                if (temperatureReset)
                {
                    // Generate a random value between 5.0°C and 45.0°C for the current temperature reading.
                    _temperature = Math.Round(_random.NextDouble() * 40.0 + 5.0, 1);
                    temperatureReset = false;
                }

                await SendTemperatureAsync();
                await Task.Delay(5 * 1000);
            }
        }

        private async Task GetWritablePropertiesAndHandleChangesAsync()
        {
            Twin twin = await _deviceClient.GetTwinAsync();
            _logger.LogInformation($"Device retrieving twin values on CONNECT: {twin.ToJson()}");

            TwinCollection twinCollection = twin.Properties.Desired;
            long serverWritablePropertiesVersion = twinCollection.Version;

            // Check if the writable property version is outdated on the local side.
            // For the purpose of this sample, we'll only check the writable property versions between local and server
            // side without comparing the property values.
            if (serverWritablePropertiesVersion > s_localWritablePropertiesVersion)
            {
                _logger.LogInformation($"The writable property version cached on local is changing " +
                    $"from {s_localWritablePropertiesVersion} to {serverWritablePropertiesVersion}.");

                foreach (KeyValuePair<string, object> propertyUpdate in twinCollection)
                {
                    string componentName = propertyUpdate.Key;
                    if (componentName == "targetTemperature")
                    {
                        await TargetTemperatureUpdateCallbackAsync(twinCollection, componentName);
                    }
                    else
                    {
                        _logger.LogWarning($"Property: Received an unrecognized property update from service:" +
                            $"\n[ {propertyUpdate.Key}: {propertyUpdate.Value} ].");
                    }
                }

                _logger.LogInformation($"The writable property version on local is currently {s_localWritablePropertiesVersion}.");
            }
        }

        // The desired property update callback, which receives the target temperature as a desired property update,
        // and updates the current temperature value over telemetry and reported property update.
        private async Task TargetTemperatureUpdateCallbackAsync(TwinCollection desiredProperties, object userContext)
        {
            const string propertyName = "targetTemperature";

            (bool targetTempUpdateReceived, double targetTemperature) = GetPropertyFromTwin<double>(desiredProperties, propertyName);
            if (targetTempUpdateReceived)
            {
                _logger.LogDebug($"Property: Received - {{ \"{propertyName}\": {targetTemperature}°C }}.");

                s_localWritablePropertiesVersion = desiredProperties.Version;

                string jsonPropertyPending = $"{{ \"{propertyName}\": {{ \"value\": {_temperature}, \"ac\": {(int)StatusCode.InProgress}, " +
                    $"\"av\": {desiredProperties.Version} }} }}";
                var reportedPropertyPending = new TwinCollection(jsonPropertyPending);
                await _deviceClient.UpdateReportedPropertiesAsync(reportedPropertyPending);
                _logger.LogDebug($"Property: Update - {{\"{propertyName}\": {targetTemperature}°C }} is {StatusCode.InProgress}.");

                // Update Temperature in 2 steps
                double step = (targetTemperature - _temperature) / 2d;
                for (int i = 1; i <= 2; i++)
                {
                    _temperature = Math.Round(_temperature + step, 1);
                    await Task.Delay(6 * 1000);
                }

                string jsonProperty = $"{{ \"{propertyName}\": {{ \"value\": {_temperature}, \"ac\": {(int)StatusCode.Completed}, " +
                    $"\"av\": {desiredProperties.Version}, \"ad\": \"Successfully updated target temperature\" }} }}";
                var reportedProperty = new TwinCollection(jsonProperty);
                await _deviceClient.UpdateReportedPropertiesAsync(reportedProperty);
                _logger.LogDebug($"Property: Update - {{\"{propertyName}\": {_temperature}°C }} is {StatusCode.Completed}.");
            }
            else
            {
                _logger.LogDebug($"Property: Received an unrecognized property update from service:\n{desiredProperties.ToJson()}");
            }
        }

        private static (bool, T) GetPropertyFromTwin<T>(TwinCollection collection, string propertyName)
        {
            return collection.Contains(propertyName) ? (true, (T)collection[propertyName]) : (false, default);
        }

        // The callback to handle "getMaxMinReport" command. This method will returns the max, min and average temperature
        // from the specified time to the current time.
        private Task<MethodResponse> HandleMaxMinReportCommand(MethodRequest request, object userContext)
        {
            try
            {
                DateTime sinceInUtc = JsonConvert.DeserializeObject<DateTime>(request.DataAsJson);
                var sinceInDateTimeOffset = new DateTimeOffset(sinceInUtc);
                _logger.LogDebug($"Command: Received - Generating max, min and avg temperature report since " +
                    $"{sinceInDateTimeOffset.LocalDateTime}.");

                Dictionary<DateTimeOffset, double> filteredReadings = _temperatureReadingsDateTimeOffset
                    .Where(i => i.Key > sinceInDateTimeOffset)
                    .ToDictionary(i => i.Key, i => i.Value);

                if (filteredReadings != null && filteredReadings.Any())
                {
                    var report = new
                    {
                        maxTemp = filteredReadings.Values.Max<double>(),
                        minTemp = filteredReadings.Values.Min<double>(),
                        avgTemp = filteredReadings.Values.Average(),
                        startTime = filteredReadings.Keys.Min(),
                        endTime = filteredReadings.Keys.Max(),
                    };

                    _logger.LogDebug($"Command: MaxMinReport since {sinceInDateTimeOffset.LocalDateTime}:" +
                        $" maxTemp={report.maxTemp}, minTemp={report.minTemp}, avgTemp={report.avgTemp}, " +
                        $"startTime={report.startTime.LocalDateTime}, endTime={report.endTime.LocalDateTime}");

                    byte[] responsePayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(report));
                    return Task.FromResult(new MethodResponse(responsePayload, (int)StatusCode.Completed));
                }

                _logger.LogDebug($"Command: No relevant readings found since {sinceInDateTimeOffset.LocalDateTime}, cannot generate any report.");
                return Task.FromResult(new MethodResponse((int)StatusCode.NotFound));
            }
            catch (JsonReaderException ex)
            {
                _logger.LogDebug($"Command input is invalid: {ex.Message}.");
                return Task.FromResult(new MethodResponse((int)StatusCode.BadRequest));
            }
        }

        // Send temperature updates over telemetry. The sample also sends the value of max temperature since last reboot over reported property update.
        private async Task SendTemperatureAsync()
        {
            await SendTemperatureTelemetryAsync();

            double maxTemp = _temperatureReadingsDateTimeOffset.Values.Max<double>();
            if (maxTemp > _maxTemp)
            {
                _maxTemp = maxTemp;
                await UpdateMaxTemperatureSinceLastRebootAsync();
            }
        }

        // Send temperature update over telemetry.
        private async Task SendTemperatureTelemetryAsync()
        {
            const string telemetryName = "temperature";

            string telemetryPayload = $"{{ \"{telemetryName}\": {_temperature} }}";
            using var message = new Message(Encoding.UTF8.GetBytes(telemetryPayload))
            {
                ContentEncoding = "utf-8",
                ContentType = "application/json",
            };

            await _deviceClient.SendEventAsync(message);
            _logger.LogDebug($"Telemetry: Sent - {{ \"{telemetryName}\": {_temperature}°C }}.");

            _temperatureReadingsDateTimeOffset.Add(DateTimeOffset.Now, _temperature);
        }

        // Send temperature over reported property update.
        private async Task UpdateMaxTemperatureSinceLastRebootAsync()
        {
            const string propertyName = "maxTempSinceLastReboot";

            var reportedProperties = new TwinCollection();
            reportedProperties[propertyName] = _maxTemp;

            await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            _logger.LogDebug($"Property: Update - {{ \"{propertyName}\": {_maxTemp}°C }} is {StatusCode.Completed}.");
        }
    }
}
