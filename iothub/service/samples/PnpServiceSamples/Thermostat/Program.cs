using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Thermostat
{
    public class Program
    {
        // Get connection string and device id inputs
        private static readonly string s_hubConnectionString = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");
        private static readonly string s_deviceId = Environment.GetEnvironmentVariable("DEVICE_ID");

        // These are values as defined in DTMI used for PnP no Component device client sample.
        // DTDL interface used: https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/samples/Thermostat.json

        // Writable property to update
        private const string PropertyName = "targetTemperature";
        private const double PropertyValue = 60;

        // Method on a given component
        private const string MethodName = "getMaxMinReport";
        private static readonly DateTime dateTime = DateTime.Now;

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

            s_logger.LogDebug($"Get Twin model Id and Update Twin");
            await GetAndUpdateTwinAsync();

            s_logger.LogDebug($"Invoke a method");
            await InvokeMethodAsync();
        }

        private static async Task InvokeMethodAsync()
        {
            var methodInvocation = new CloudToDeviceMethod(MethodName) { ResponseTimeout = TimeSpan.FromSeconds(30) };
            var payload = "{\"since\":" + JsonConvert.SerializeObject(dateTime) + "}";
            methodInvocation.SetPayloadJson(payload);
            CloudToDeviceMethodResult result = await s_serviceClient.InvokeDeviceMethodAsync(s_deviceId, methodInvocation);

            if(result == null)
            {
                throw new Exception($"Method {MethodName} invovation returned null");
            }

            s_logger.LogDebug("Method invocation result status is: " + result.Status);
        }

        private static async Task GetAndUpdateTwinAsync()
        {
            // Get a Twin and retrieves model Id set by Device client
            Twin twin = await s_registryManager.GetTwinAsync(s_deviceId);
            s_logger.LogDebug("Model Id of this Twin is: " + twin.ModelId);

            // Update the twin
            Twin twinPatch = new Twin();
            twinPatch.Properties.Desired[PropertyName] = PropertyValue;
            await s_registryManager.UpdateTwinAsync(s_deviceId, twinPatch, twin.ETag);
        }

        private static void InitializeServiceClient()
        {
            s_registryManager = RegistryManager.CreateFromConnectionString(s_hubConnectionString);
            s_serviceClient = ServiceClient.CreateFromConnectionString(s_hubConnectionString);
        }
    }
}
