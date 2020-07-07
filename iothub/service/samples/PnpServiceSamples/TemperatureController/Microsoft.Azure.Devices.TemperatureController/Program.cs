using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using PnpHelpers;

namespace Microsoft.Azure.Devices.TemperatureController
{
    public class Program
    {
        // Get connection string and device id inputs
        private static readonly string s_hubConnectionString = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");
        private static readonly string s_deviceId = Environment.GetEnvironmentVariable("DEVICE_ID");

        // These are values as defined in DTMI used for device client sample with component.
        private const string ComponentName = "thermostat1";

        // Writable property to update
        private const string PropertyName = "targetTemperature";
        private const double PropertyValue = 60;

        // Method on a given component
        private const string MethodName = "getMaxMinReport";
        private static readonly DateTime dateTime = DateTime.Now ;

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

            PrintLog($"Get model Id and Update Component Property");
            await GetAndUpdateTwinAsync();

            PrintLog($"Invoke a method on a Component");
            await InvokeMethodAsync();
        }

        private static async Task InvokeMethodAsync()
        {
            // Create method name to invoke for component
            string methodToInvoke = PnpHelper.CreatePnpCommandName(MethodName, ComponentName);
            var methodInvocation = new CloudToDeviceMethod(methodToInvoke) { ResponseTimeout = TimeSpan.FromSeconds(30) };

            // Set Method Payload    
            var componentMethodPayload = PnpHelper.CreatePnpCommandRequestPayload(JsonConvert.SerializeObject(dateTime));
            methodInvocation.SetPayloadJson(componentMethodPayload);

            CloudToDeviceMethodResult result = await s_serviceClient.InvokeDeviceMethodAsync(s_deviceId, methodInvocation);

            if (result == null)
            {
                throw new Exception("Method invoke returns null");
            }

            PrintLog("Method result status is: " + result.Status);
        }

        private static async Task GetAndUpdateTwinAsync()
        {
            // Get a Twin and retrieves model Id set by Device client
            Twin twin = await s_registryManager.GetTwinAsync(s_deviceId);
            Console.WriteLine("Model Id of this Twin is: " + twin.ModelId);

            // Update the twin
            string propertyUpdate = PnpHelper.CreatePropertyPatch(PropertyName, JsonConvert.SerializeObject(PropertyValue), ComponentName);
            string twinPatch = $"{{ \"properties\": {{\"desired\": {propertyUpdate}}}}}";

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
