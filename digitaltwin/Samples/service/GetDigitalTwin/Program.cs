using Microsoft.Azure.Devices.DigitalTwin.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Sample
{
    class Program
    {
        private static string IOTHUB_CONNECTION_STRING = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");
        private static string DEVICE_ID = Environment.GetEnvironmentVariable("DEVICE_ID");

        public static int Main(string[] args)
        {
            verifyInputs();

            var digitalTwinServiceClient = new DigitalTwinServiceClient(IOTHUB_CONNECTION_STRING);

            Console.WriteLine("Getting the status of digital twin " + DEVICE_ID);

            string digitalTwin = digitalTwinServiceClient.GetDigitalTwin(DEVICE_ID);
            string jsonFormatted = JValue.Parse(digitalTwin).ToString(Formatting.Indented);

            Console.WriteLine("Got the status of the digital twin successfully, the returned string was:");
            Console.WriteLine(jsonFormatted);

            Console.WriteLine("Enter any key to finish");
            Console.ReadLine();

            return 0;
        }

        private static void verifyInputs()
        {
            if (isNullOrEmpty(IOTHUB_CONNECTION_STRING) || isNullOrEmpty(DEVICE_ID))
            {
                Console.WriteLine(usage);
                Console.WriteLine("Enter any key to finish");
                Console.ReadLine();
                Environment.Exit(0);
            }
        }

        private static bool isNullOrEmpty(String s)
        {
            return s == null || s.Length == 0;
        }

        private static String usage = "In order to run this sample, you must set environment variables for \n" +
                "IOTHUB_CONNECTION_STRING - Your IoT Hub's connection string\n" +
                "DEVICE_ID - The ID of the device to get the digital twin of\n";
    }
}
