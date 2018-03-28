// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Samples
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Shared;
    using Newtonsoft.Json;

    class Program
    {
        private static int MESSAGE_COUNT = 5;
        private const int TEMPERATURE_THRESHOLD = 30;
        private static float temperature;
        private static float humidity;
        private static readonly Random rnd = new Random();

        // String containing Hostname, Device Id & Device Key in one of the following formats:
        //  "HostName=<iothub_host_name>;GatewayHostName=<edge_host>;ModuleId=<module_id>;DeviceId=<device_id>;SharedAccessKey=<device_key>"
        //  "HostName=<iothub_host_name>;CredentialType=SharedAccessSignature;GatewayHostName=<edge_host>;ModuleId=<module_id>;DeviceId=<device_id>;SharedAccessSignature=SharedAccessSignature sr=<iot_host>/devices/<device_id>&sig=<token>&se=<expiry_time>";
        // Either set the IOTHUB_MODULE_CONN_STRING environment variable or within launchSettings.json:
        private static string ModuleConnectionString = Environment.GetEnvironmentVariable("IOTHUB_MODULE_CONN_STRING");

        static void Main(string[] args)
        {
            try
            {
                try
                {
                    DeviceClient moduleClient = DeviceClient.CreateFromConnectionString(ModuleConnectionString);
                    moduleClient.OpenAsync().Wait();
                    SendMessages(moduleClient).Wait();
                    Twin twin = moduleClient.GetTwinAsync().Result;
                    Console.WriteLine($"Module Twin Desired Properties: {twin.Properties.Desired.ToJson(Formatting.Indented)}");
                    Console.WriteLine($"Module Twin Tags: {twin.Tags.ToJson(Formatting.Indented)}");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                Console.WriteLine("Exited!\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

        static async Task SendMessages(DeviceClient moduleClient)
        {
            string moduleId = IotHubConnectionStringBuilder.Create(ModuleConnectionString).ModuleId;

            Console.WriteLine("Device sending {0} messages to IoT Hub...\n", MESSAGE_COUNT);

            for (int count = 0; count < MESSAGE_COUNT; count++)
            {
                temperature = rnd.Next(20, 35);
                humidity = rnd.Next(60, 80);
                string dataBuffer = string.Format("{{\"deviceId\":\"{0}\",\"messageId\":{1},\"temperature\":{2},\"humidity\":{3}}}", moduleId, count, temperature, humidity);
                var eventMessage = new Message(Encoding.UTF8.GetBytes(dataBuffer));
                eventMessage.Properties.Add("temperatureAlert", (temperature > TEMPERATURE_THRESHOLD) ? "true" : "false");
                Console.WriteLine("\t{0}> Sending message: {1}, Data: [{2}]", DateTime.Now.ToLocalTime(), count, dataBuffer);

                await moduleClient.SendEventAsync("sample/test/", eventMessage);
            }
        }
    }
}
