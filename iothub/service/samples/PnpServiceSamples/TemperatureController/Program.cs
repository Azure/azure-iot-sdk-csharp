// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace TemperatureController
{
    public class Program
    {
        // Get connection string and device id inputs
        private static readonly string s_hubConnectionString = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");

        private static readonly string s_deviceId = Environment.GetEnvironmentVariable("DEVICE_ID");

        // These are values as defined in DTMI used for PnP device client sample: https://github.com/Azure/azure-iot-sdk-csharp/tree/master/iothub/device/samples/PnpDeviceSamples/TemperatureController
        // DTDL interface used: https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/samples/TemperatureController.json
        private const string ComponentName = "thermostat1";

        // Writable property to update
        private const string PropertyName = "targetTemperature";

        private const double PropertyValue = 60;

        // Command on a root interface
        private const string RebootCommandName = "reboot";

        private const int RebootDelayInSecs = 3;

        // Command on a "Thermostat" component
        private const string ReportCommandName = "getMaxMinReport";

        private static readonly DateTime s_dateTime = DateTime.Now;

        private static ServiceClient s_serviceClient;
        private static RegistryManager s_registryManager;
        private static ILogger s_logger;

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
            s_logger.LogDebug($"Initialize the service client.");
            InitializeServiceClient();

            s_logger.LogDebug($"Get model Id and Update Component Property");
            await GetAndUpdateTwinAsync();

            s_logger.LogDebug($"Invoke a command on a Component");
            await InvokeCommandOnComponentAsync();

            s_logger.LogDebug($"Invoke a command on root interface");
            await InvokeCommandOnRootInterfaceAsync();
        }

        private static async Task InvokeCommandOnComponentAsync()
        {
            // Create command name to invoke for component
            string commandToInvoke = $"{ComponentName}*{ReportCommandName}";
            var commandInvocation = new CloudToDeviceMethod(commandToInvoke) { ResponseTimeout = TimeSpan.FromSeconds(30) };

            // Set command payload
            string componentCommandPayload = JsonConvert.SerializeObject(s_dateTime);
            commandInvocation.SetPayloadJson(componentCommandPayload);

            CloudToDeviceMethodResult result = await s_serviceClient.InvokeDeviceMethodAsync(s_deviceId, commandInvocation);

            if (result == null)
            {
                throw new Exception($"Command {ReportCommandName} invocation returned null");
            }

            s_logger.LogDebug($"Command {ReportCommandName} invocation result status is: {result.Status}");
        }

        private static async Task InvokeCommandOnRootInterfaceAsync()
        {
            // Create command name to invoke for component
            string commandToInvoke = RebootCommandName;
            var commandInvocation = new CloudToDeviceMethod(commandToInvoke) { ResponseTimeout = TimeSpan.FromSeconds(30) };

            // Set command payload
            string componentCommandPayload = JsonConvert.SerializeObject(RebootDelayInSecs);
            commandInvocation.SetPayloadJson(componentCommandPayload);

            CloudToDeviceMethodResult result = await s_serviceClient.InvokeDeviceMethodAsync(s_deviceId, commandInvocation);

            if (result == null)
            {
                throw new Exception($"Command {ReportCommandName} invocation returned null");
            }

            s_logger.LogDebug($"Command {ReportCommandName} invocation result status is: {result.Status}");
        }

        private static async Task GetAndUpdateTwinAsync()
        {
            // Get a Twin and retrieves model Id set by Device client
            Twin twin = await s_registryManager.GetTwinAsync(s_deviceId);
            s_logger.LogDebug($"Model Id of this Twin is: {twin.ModelId}");

            // Update the twin
            string propertyUpdate = CreatePropertyPatch(PropertyName, JsonConvert.SerializeObject(PropertyValue), ComponentName);
            string twinPatch = $"{{ \"properties\": {{\"desired\": {propertyUpdate} }} }}";

            await s_registryManager.UpdateTwinAsync(s_deviceId, twinPatch, twin.ETag);
        }

        private static void InitializeServiceClient()
        {
            s_registryManager = RegistryManager.CreateFromConnectionString(s_hubConnectionString);
            s_serviceClient = ServiceClient.CreateFromConnectionString(s_hubConnectionString);
        }

        /* The property update patch (for a property within a component) needs to be in the following format:
         * {
         *  "sampleComponentName":
         *      {
         *          "__t": "c",
         *          "samplePropertyName": 20
         *      }
         *  }
         */

        private static string CreatePropertyPatch(string propertyName, string serializedPropertyValue, string componentName)
        {
            return $"{{" +
                    $"  \"{componentName}\": " +
                    $"      {{" +
                    $"          \"__t\": \"c\"," +
                    $"          \"{propertyName}\": {serializedPropertyValue}" +
                    $"      }} " +
                    $"}}";
        }
    }
}
