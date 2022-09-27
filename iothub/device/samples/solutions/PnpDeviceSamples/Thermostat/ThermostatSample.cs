// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Samples
{
    internal enum StatusCode
    {
        Completed = 200,
        InProgress = 202,
        ReportDeviceInitialProperty = 203,
        BadRequest = 400,
        NotFound = 404
    }

    public class ThermostatSample
    {
        // The default reported "value" and "av" for each "Thermostat" component on the client initial startup.
        // See https://docs.microsoft.com/azure/iot-develop/concepts-convention#writable-properties for more details in acknowledgment responses.
        private const double DefaultPropertyValue = 0d;
        private const long DefaultAckVersion = 0L;

        private const string TargetTemperatureProperty = "targetTemperature";

        private readonly Random _random = new();

        private double _temperature;
        private double _maxTemp;

        // Dictionary to hold the temperature updates sent over.
        // NOTE: Memory constrained devices should leverage storage capabilities of an external service to store this information and perform computation.
        // See https://docs.microsoft.com/en-us/azure/event-grid/compare-messaging-services for more details.
        private readonly Dictionary<DateTimeOffset, double> _temperatureReadingsDateTimeOffset = new Dictionary<DateTimeOffset, double>();

        private readonly IotHubDeviceClient _deviceClient;
        private readonly ILogger _logger;

        // A safe initial value for caching the writable properties version is 1, so the client
        // will process all previous property change requests and initialize the device application
        // after which this version will be updated to that, so we have a high water mark of which version number
        // has been processed.
        private static long s_localWritablePropertiesVersion = 1;

        public ThermostatSample(IotHubDeviceClient deviceClient, ILogger logger)
        {
            _deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task PerformOperationsAsync(CancellationToken cancellationToken)
        {
            // This sample follows the following workflow:
            // -> Set handler to receive and respond to connection status changes.
            // -> Set handler to receive "targetTemperature" updates, and send the received update over reported property.
            // -> Set handler to receive "getMaxMinReport" command, and send the generated report as command response.
            // -> Check if the device properties are empty on the initial startup. If so, report the default values with ACK to the hub.
            // -> Periodically send "temperature" over telemetry.
            // -> Send "maxTempSinceLastReboot" over property update, when a new max temperature is set.

            _deviceClient.SetConnectionStatusChangeCallback(async (info) =>
            {
                _logger.LogDebug($"Connection status change registered - status={info.Status}, reason={info.ChangeReason}.");

                // Call GetWritablePropertiesAndHandleChangesAsync() to get writable properties from the server once the connection status changes into Connected.
                // This can get back "lost" property updates in a device reconnection from status Disconnected_Retrying or Disconnected.
                if (info.Status == ConnectionStatus.Connected)
                {
                    await GetWritablePropertiesAndHandleChangesAsync();
                }
            });

            _logger.LogDebug($"Set handler to receive \"targetTemperature\" updates.");
            await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(TargetTemperatureUpdateCallbackAsync, cancellationToken);

            _logger.LogDebug($"Set handler for \"getMaxMinReport\" command.");
            await _deviceClient.SetMethodCallbackAsync(OnDirectMethodAsync, cancellationToken);

            _logger.LogDebug("Check if the device properties are empty on the initial startup.");
            await CheckEmptyPropertiesAsync(cancellationToken);

            bool temperatureReset = true;
            while (!cancellationToken.IsCancellationRequested)
            {
                if (temperatureReset)
                {
                    // Generate a random value between 5.0°C and 45.0°C for the current temperature reading.
                    _temperature = Math.Round(_random.NextDouble() * 40.0 + 5.0, 1);
                    temperatureReset = false;
                }

                await SendTemperatureAsync(cancellationToken);
                await Task.Delay(5 * 1000, cancellationToken);
            }
        }

        private async Task<DirectMethodResponse> OnDirectMethodAsync(DirectMethodRequest request)
        {
            return request.MethodName switch
            {
                "getMaxMinReport" => await HandleMaxMinReportCommandAsync(request),
                _ => new DirectMethodResponse(400),
            };
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
                    string propertyName = propertyUpdate.Key;
                    if (propertyName == TargetTemperatureProperty)
                    {
                        await TargetTemperatureUpdateCallbackAsync(twinCollection);
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
        private async Task TargetTemperatureUpdateCallbackAsync(TwinCollection desiredProperties)
        {
            (bool targetTempUpdateReceived, double targetTemperature) = GetPropertyFromTwin<double>(desiredProperties, TargetTemperatureProperty);
            if (targetTempUpdateReceived)
            {
                _logger.LogDebug($"Property: Received - {{ \"{TargetTemperatureProperty}\": {targetTemperature}°C }}.");

                s_localWritablePropertiesVersion = desiredProperties.Version;

                string jsonPropertyPending = $"{{ \"{TargetTemperatureProperty}\": {{ \"value\": {targetTemperature}, \"ac\": {(int)StatusCode.InProgress}, " +
                    $"\"av\": {desiredProperties.Version}, \"ad\": \"In progress - reporting current temperature\" }} }}";
                var reportedPropertyPending = new TwinCollection(jsonPropertyPending);
                await _deviceClient.UpdateReportedPropertiesAsync(reportedPropertyPending);
                _logger.LogDebug($"Property: Update - {{\"{TargetTemperatureProperty}\": {targetTemperature}°C }} is {StatusCode.InProgress}.");

                // Update Temperature in 2 steps
                double step = (targetTemperature - _temperature) / 2d;
                for (int i = 1; i <= 2; i++)
                {
                    _temperature = Math.Round(_temperature + step, 1);
                    await Task.Delay(6 * 1000);
                }

                string jsonProperty = $"{{ \"{TargetTemperatureProperty}\": {{ \"value\": {targetTemperature}, \"ac\": {(int)StatusCode.Completed}, " +
                    $"\"av\": {desiredProperties.Version}, \"ad\": \"Successfully updated target temperature\" }} }}";
                var reportedProperty = new TwinCollection(jsonProperty);
                await _deviceClient.UpdateReportedPropertiesAsync(reportedProperty);
                _logger.LogDebug($"Property: Update - {{\"{TargetTemperatureProperty}\": {targetTemperature}°C }} is {StatusCode.Completed}.");
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
        private Task<DirectMethodResponse> HandleMaxMinReportCommandAsync(DirectMethodRequest request)
        {
            try
            {
                bool sinceInUtcReceived = request.TryGetPayload(out DateTime sinceInUtc);

                if (sinceInUtcReceived)
                {
                    var sinceInDateTimeOffset = new DateTimeOffset(sinceInUtc);
                    _logger.LogDebug($"Command: Received - Generating max, min and avg temperature report since " +
                        $"{sinceInDateTimeOffset.LocalDateTime}.");

                    var filteredReadings = _temperatureReadingsDateTimeOffset
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
                        return Task.FromResult(new DirectMethodResponse((int)StatusCode.Completed) { Payload = responsePayload });
                    }
                }

                _logger.LogDebug($"Command: No relevant readings found, cannot generate any report.");
                return Task.FromResult(new DirectMethodResponse((int)StatusCode.NotFound));
            }
            catch (JsonReaderException ex)
            {
                _logger.LogDebug($"Command input is invalid: {ex.Message}.");
                return Task.FromResult(new DirectMethodResponse((int)StatusCode.BadRequest));
            }
        }

        // Send temperature updates over telemetry. The sample also sends the value of max temperature since last reboot over reported property update.
        private async Task SendTemperatureAsync(CancellationToken cancellationToken)
        {
            await SendTemperatureTelemetryAsync(cancellationToken);

            double maxTemp = _temperatureReadingsDateTimeOffset.Values.Max<double>();
            if (maxTemp > _maxTemp)
            {
                _maxTemp = maxTemp;
                await UpdateMaxTemperatureSinceLastRebootAsync(cancellationToken);
            }
        }

        // Send temperature update over telemetry.
        private async Task SendTemperatureTelemetryAsync(CancellationToken cancellationToken)
        {
            const string telemetryName = "temperature";

            string telemetryPayload = $"{{ \"{telemetryName}\": {_temperature} }}";
            var message = new Message(Encoding.UTF8.GetBytes(telemetryPayload))
            {
                ContentEncoding = "utf-8",
                ContentType = "application/json",
            };

            await _deviceClient.SendEventAsync(message, cancellationToken);
            _logger.LogDebug($"Telemetry: Sent - {{ \"{telemetryName}\": {_temperature}°C }}.");

            _temperatureReadingsDateTimeOffset.Add(DateTimeOffset.Now, _temperature);
        }

        // Send temperature over reported property update.
        private async Task UpdateMaxTemperatureSinceLastRebootAsync(CancellationToken cancellationToken)
        {
            const string propertyName = "maxTempSinceLastReboot";

            var reportedProperties = new TwinCollection();
            reportedProperties[propertyName] = _maxTemp;

            await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties, cancellationToken);
            _logger.LogDebug($"Property: Update - {{ \"{propertyName}\": {_maxTemp}°C }} is {StatusCode.Completed}.");
        }

        private async Task CheckEmptyPropertiesAsync(CancellationToken cancellationToken)
        {
            Twin twin = await _deviceClient.GetTwinAsync(cancellationToken);
            TwinCollection writableProperty = twin.Properties.Desired;
            TwinCollection reportedProperty = twin.Properties.Reported;

            // Check if the device properties (both writable and reported) are empty.
            if (!writableProperty.Contains(TargetTemperatureProperty) && !reportedProperty.Contains(TargetTemperatureProperty))
            {
                await ReportInitialPropertyAsync(TargetTemperatureProperty, cancellationToken);
            }
        }

        private async Task ReportInitialPropertyAsync(string propertyName, CancellationToken cancellationToken)
        {
            // If the device properties are empty, report the default value with ACK(ac=203, av=0) as part of the PnP convention.
            // "DefaultPropertyValue" is set from the device when the desired property is not set via the hub.
            string jsonProperty = $"{{ \"{propertyName}\": {{ \"value\": {DefaultPropertyValue}, \"ac\": {(int)StatusCode.ReportDeviceInitialProperty}, " +
                    $"\"av\": {DefaultAckVersion}, \"ad\": \"Initialized with default value\"}} }}";

            var reportedProperty = new TwinCollection(jsonProperty);
            await _deviceClient.UpdateReportedPropertiesAsync(reportedProperty, cancellationToken);
            _logger.LogDebug($"Report the default values.\nProperty: Update - {jsonProperty} is {StatusCode.Completed}.");
        }
    }
}
