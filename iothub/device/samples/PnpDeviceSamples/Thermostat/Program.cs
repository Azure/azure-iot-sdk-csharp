﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Thermostat
{
    internal enum StatusCode
    {
        Completed = 200,
        InProgress = 202,
        NotFound = 404,
        BadRequest = 400
    }

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

        private static readonly Random s_random = new Random();

        private static DeviceClient s_deviceClient;
        private static ILogger s_logger;

        private static double s_temperature = 0d;
        private static double s_maxTemp = 0d;

        // Dictionary to hold the temperature updates sent over.
        // NOTE: Memory constrained devices should leverage storage capabilities of an external service to store this information and perform computation.
        // See https://docs.microsoft.com/en-us/azure/event-grid/compare-messaging-services for more details.
        private static readonly Dictionary<DateTimeOffset, double> s_temperatureReadings = new Dictionary<DateTimeOffset, double>();

        public static async Task Main(string[] _)
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
            s_logger = loggerFactory.CreateLogger<Program>();

            await RunSampleAsync();
        }

        private static async Task RunSampleAsync()
        {
            // This sample follows the following workflow:
            // -> Initialize device client instance.
            // -> Set handler to receive "targetTemperature" updates, and send the received update over reported property.
            // -> Set handler to receive "getMaxMinReport" command, and send the generated report as command response.
            // -> Periodically send "temperature" over telemetry.
            // -> Send "maxTempSinceLastReboot" over property update, when a new max temperature is set.

            if (string.IsNullOrWhiteSpace(s_deviceSecurityType))
            {
                throw new ArgumentNullException("Device security type needs to be specified, please set the environment variable \"IOTHUB_DEVICE_SECURITY_TYPE\".");
            }

            s_logger.LogDebug($"Initialize the device client.");
            switch (s_deviceSecurityType.ToLowerInvariant())
            {
                case "dps":
                    if (ValidateArgsForDpsFlow())
                    {
                        DeviceRegistrationResult dpsRegistrationResult = await ProvisionDeviceAsync();
                        var authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(dpsRegistrationResult.DeviceId, s_deviceSymmetricKey);
                        InitializeDeviceClient(dpsRegistrationResult.AssignedHub, authMethod);
                        break;
                    }
                    throw new ArgumentException("Required environment variables are not set for DPS flow, please recheck your environment.");

                case "connectionstring":
                    if (ValidateArgsForIotHubFlow())
                    {
                        InitializeDeviceClient(s_deviceConnectionString);
                        break;
                    }
                    throw new ArgumentException("Required environment variables are not set for IoT Hub flow, please recheck your environment.");

                default:
                    throw new ArgumentException($"Unrecognized value for IOTHUB_DEVICE_SECURITY_TYPE received: {s_deviceSecurityType}." +
                        $" It should be either \"DPS\" or \"connectionString\" (case-insensitive).");
            }

            s_logger.LogDebug($"Set handler to receive \"targetTemperature\" updates.");
            await s_deviceClient.SetDesiredPropertyUpdateCallbackAsync(TargetTemperatureUpdateCallbackAsync, s_deviceClient);

            s_logger.LogDebug($"Set handler for \"getMaxMinReport\" command.");
            await s_deviceClient.SetMethodHandlerAsync("getMaxMinReport", HandleMaxMinReportCommandAsync, s_deviceClient);

            bool temperatureReset = true;
            await Task.Run(async () =>
            {
                while (true)
                {
                    if (temperatureReset)
                    {
                        // Generate a random value between 5.0°C and 45.0°C for the current temperature reading.
                        s_temperature = Math.Round(s_random.NextDouble() * 40.0 + 5.0, 1);
                        temperatureReset = false;
                    }

                    await SendTemperatureAsync();
                    await Task.Delay(5 * 1000);
                }
            });
        }

        // Provision a device via DPS, by sending the PnP model Id as DPS payload.
        private static async Task<DeviceRegistrationResult> ProvisionDeviceAsync()
        {
            SecurityProvider symmetricKeyProvider = new SecurityProviderSymmetricKey(s_deviceRegistrationId, s_deviceSymmetricKey, null);
            ProvisioningTransportHandler mqttTransportHandler = new ProvisioningTransportHandlerMqtt();
            var pdc = ProvisioningDeviceClient.Create(s_dpsEndpoint, s_dpsIdScope, symmetricKeyProvider, mqttTransportHandler);

            var pnpPayload = new ProvisioningRegistrationAdditionalData
            {
                JsonData = $"{{ \"modelId\": \"{ModelId}\" }}",
            };
            return await pdc.RegisterAsync(pnpPayload);
        }

        // Initialize the device client instance using connection string based authentication, over Mqtt protocol (TCP, with fallback over Websocket) and setting the ModelId into ClientOptions.
        // This method also sets a connection status change callback, that will get triggered any time the device's connection status changes.
        private static void InitializeDeviceClient(string deviceConnectionString)
        {
            var options = new ClientOptions
            {
                ModelId = ModelId,
            };
            s_deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt, options);
            s_deviceClient.SetConnectionStatusChangesHandler((status, reason) =>
            {
                s_logger.LogDebug($"Connection status change registered - status={status}, reason={reason}.");
            });
        }

        // Initialize the device client instance using symmetric key based authentication, over Mqtt protocol (TCP, with fallback over Websocket) and setting the ModelId into ClientOptions.
        // This method also sets a connection status change callback, that will get triggered any time the device's connection status changes.
        private static void InitializeDeviceClient(string hostname, IAuthenticationMethod authenticationMethod)
        {
            var options = new ClientOptions
            {
                ModelId = ModelId,
            };
            s_deviceClient = DeviceClient.Create(hostname, authenticationMethod, TransportType.Mqtt, options);
            s_deviceClient.SetConnectionStatusChangesHandler((status, reason) =>
            {
                s_logger.LogDebug($"Connection status change registered - status={status}, reason={reason}.");
            });
        }

        // The desired property update callback, which receives the target temperature as a desired property update,
        // and updates the current temperature value over telemetry and reported property update.
        private static async Task TargetTemperatureUpdateCallbackAsync(TwinCollection desiredProperties, object userContext)
        {
            string propertyName = "targetTemperature";

            (bool targetTempUpdateReceived, double targetTemperature) = GetPropertyFromTwin<double>(desiredProperties, propertyName);
            if (targetTempUpdateReceived)
            {
                s_logger.LogDebug($"Property: Received - {{ \"{propertyName}\": {targetTemperature}°C }}.");

                string jsonPropertyPending = $"{{ \"{propertyName}\": {{ \"value\": {s_temperature}, \"ac\": {(int)StatusCode.InProgress}, \"av\": {desiredProperties.Version} }} }}";
                var reportedPropertyPending = new TwinCollection(jsonPropertyPending);
                await s_deviceClient.UpdateReportedPropertiesAsync(reportedPropertyPending);
                s_logger.LogDebug($"Property: Update - {{\"{propertyName}\": {targetTemperature}°C }} is {StatusCode.InProgress}.");

                // Update Temperature in 2 steps
                double step = (targetTemperature - s_temperature) / 2d;
                for (int i = 1; i <= 2; i++)
                {
                    s_temperature = Math.Round(s_temperature + step, 1);
                    await Task.Delay(6 * 1000);
                }

                string jsonProperty = $"{{ \"{propertyName}\": {{ \"value\": {s_temperature}, \"ac\": {(int)StatusCode.Completed}, \"av\": {desiredProperties.Version}, \"ad\": \"Successfully updated target temperature\" }} }}";
                var reportedProperty = new TwinCollection(jsonProperty);
                await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperty);
                s_logger.LogDebug($"Property: Update - {{\"{propertyName}\": {s_temperature}°C }} is {StatusCode.Completed}.");
            }
            else
            {
                s_logger.LogDebug($"Property: Received an unrecognized property update from service.");
            }
        }

        private static async Task SendTemperatureAsync()
        {
            await SendTemperatureTelemetryAsync();

            double maxTemp = s_temperatureReadings.Values.Max<double>();
            if (maxTemp > s_maxTemp)
            {
                s_maxTemp = maxTemp;
                await UpdateMaxTemperatureSinceLastRebootAsync();
            }
        }

        private static async Task SendTemperatureTelemetryAsync()
        {
            string telemetryName = "temperature";

            string telemetryPayload = $"{{ \"{telemetryName}\": {s_temperature} }}";
            var message = new Message(Encoding.UTF8.GetBytes(telemetryPayload))
            {
                ContentEncoding = "utf-8",
                ContentType = "application/json",
            };

            await s_deviceClient.SendEventAsync(message);
            s_logger.LogDebug($"Telemetry: Sent - {{ \"{telemetryName}\": {s_temperature}°C }}.");

            s_temperatureReadings.Add(DateTimeOffset.Now, s_temperature);
        }

        private static async Task UpdateMaxTemperatureSinceLastRebootAsync()
        {
            string propertyName = "maxTempSinceLastReboot";

            var reportedProperties = new TwinCollection();
            reportedProperties[propertyName] = s_maxTemp;

            await s_deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            s_logger.LogDebug($"Property: Update - {{ \"{propertyName}\": {s_maxTemp}°C }} is {StatusCode.Completed}.");
        }

        // The callback to handle "getMaxMinReport" command. This method will returns the max, min and average temperature from the specified time to the current time.
        private static async Task<MethodResponse> HandleMaxMinReportCommandAsync(MethodRequest request, object userContext)
        {
            try
            {
                DateTime since = JsonConvert.DeserializeObject<DateTime>(request.DataAsJson);
                var sinceInDateTimeOffset = new DateTimeOffset(since);
                s_logger.LogDebug($"Command: Received - Generating max, min and avg temperature report since {sinceInDateTimeOffset.LocalDateTime}.");

                var filteredReadings = s_temperatureReadings.Where(i => i.Key > sinceInDateTimeOffset).ToDictionary(i => i.Key, i => i.Value);

                if (filteredReadings != null && filteredReadings.Any())
                {
                    var report = new
                    {
                        maxTemp = filteredReadings.Values.Max<double>(),
                        minTemp = filteredReadings.Values.Min<double>(),
                        avgTemp = filteredReadings.Values.Average(),
                        startTime = filteredReadings.Keys.Min().DateTime,
                        endTime = filteredReadings.Keys.Max().DateTime,
                    };

                    s_logger.LogDebug($"Command: MaxMinReport since {sinceInDateTimeOffset.LocalDateTime}:" +
                        $" maxTemp={report.maxTemp}, minTemp={report.minTemp}, avgTemp={report.avgTemp}, startTime={report.startTime}, endTime={report.endTime}");

                    byte[] responsePayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(report));
                    return await Task.FromResult(new MethodResponse(responsePayload, (int)StatusCode.Completed));
                }

                s_logger.LogDebug($"Command: No relevant readings found since {sinceInDateTimeOffset.LocalDateTime}, cannot generate any report.");
            }
            catch (JsonReaderException ex)
            {
                s_logger.LogDebug($"Command input is invalid: {ex.Message}.");
                return await Task.FromResult(new MethodResponse((int)StatusCode.BadRequest));
            }
            return await Task.FromResult(new MethodResponse((int)StatusCode.NotFound));
        }

        private static (bool, T) GetPropertyFromTwin<T>(TwinCollection collection, string propertyName)
        {
            return collection.Contains(propertyName) ? (true, (T)collection[propertyName]) : (false, default);
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
