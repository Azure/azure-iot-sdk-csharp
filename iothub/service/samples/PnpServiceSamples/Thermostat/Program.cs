using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;

namespace Thermostat
{
    public class Program
    {
        // Get connection string and device id inputs
        private static readonly string s_hubConnectionString = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");
        private static readonly string s_deviceId = Environment.GetEnvironmentVariable("DEVICE_ID");

        // These are values as defined in DTMI used for PnP no Component device client sample.
        // DTDL interface used: https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/samples/Thermostat.json
        private const string PropertyName = "targetTemperature";
        private const double PropertyValue = 60;
        private const string MethodToInvoke = "reboot";
        private const string MethodPayload = "{\"delay\":10}";

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
            PrintLog($"Initialize the service client.");
            InitializeServiceClient();

            PrintLog($"Get Twin model Id and Update Twin");
            await GetAndUpdateTwinAsync();

            PrintLog($"Invoke a method");
            await InvokeMethodAsync();
        }

        private static async Task InvokeMethodAsync()
        {
            var methodInvocation = new CloudToDeviceMethod(MethodToInvoke) { ResponseTimeout = TimeSpan.FromSeconds(30) };
            methodInvocation.SetPayloadJson(MethodPayload);
            CloudToDeviceMethodResult result = await s_serviceClient.InvokeDeviceMethodAsync(s_deviceId, methodInvocation);

            if(result == null)
            {
                throw new Exception($"Method {MethodToInvoke} invovation returned null");
            }

            PrintLog("Method result status is: " + result.Status);
        }

        private static async Task GetAndUpdateTwinAsync()
        {
            // Get a Twin and retrieves model Id set by Device client
            Twin twin = await s_registryManager.GetTwinAsync(s_deviceId);
            Console.WriteLine("Model Id of this Twin is: " + twin.ModelId);

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

        private static void PrintLog(string message)
        {
            Console.WriteLine($">> {DateTime.Now}: {message}");
        }
    }
}
