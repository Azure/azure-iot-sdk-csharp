using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;

namespace PnpServiceSample
{
    public class Program
    {        
        // Get connection string and device id inputs
        private static readonly string hubConnectionString = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");
        private static readonly string deviceId = Environment.GetEnvironmentVariable("DEVICE_ID");

        // These are default values as per PnP no Component device client sample.
        private const string propertyName = "targetTemperature";
        private const double propertyValue = 60;
        private const string methodToInvoke = "reboot";
        private const string methodPayload = "{\"delay\":10}";

        private static ServiceClient s_serviceClient;
        private static RegistryManager s_registryManager;

        public static async Task Main(string[] args)
        {
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

            PrintLog($"Press any key to exit");
            Console.ReadKey();
        }

        private static async Task InvokeMethodAsync()
        {
            var methodInvocation = new CloudToDeviceMethod(methodToInvoke) { ResponseTimeout = TimeSpan.FromSeconds(30) };
            methodInvocation.SetPayloadJson(methodPayload);
            await s_serviceClient.InvokeDeviceMethodAsync(deviceId, methodInvocation);
        }

        private static async Task GetAndUpdateTwinAsync()
        {
            // Get a Twin and retrieves model Id set by Device client
            Twin twin = await s_registryManager.GetTwinAsync(deviceId);
            Console.WriteLine("Model Id of this Twin is: " + twin.ModelId);

            // Update the twin
            Twin twinPatch = new Twin();
            twinPatch.Properties.Desired[propertyName] = propertyValue;
            await s_registryManager.UpdateTwinAsync(deviceId, twinPatch, twin.ETag);
        }

        private static void InitializeServiceClient()
        {
            s_registryManager = RegistryManager.CreateFromConnectionString(hubConnectionString);
            s_serviceClient = ServiceClient.CreateFromConnectionString(hubConnectionString);
        }

        private static void PrintLog(string message)
        {
            Console.WriteLine($">> {DateTime.Now}: {message}");
        }
    }
}
