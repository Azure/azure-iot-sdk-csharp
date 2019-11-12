using Azure.IoT.DigitalTwin.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Sample
{
    class Program
    {
        private static string CONNECTION_STRING = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        private static string DIGITAL_TWIN_ID = Environment.GetEnvironmentVariable("DIGITAL_TWIN_ID");

        public static int Main(string[] args)
        {
            verifyInputs();

            var digitalTwinServiceClient = new DigitalTwinServiceClient(CONNECTION_STRING);

            Console.WriteLine("Getting the status of digital twin " + DIGITAL_TWIN_ID);

            string digitalTwin = digitalTwinServiceClient.GetDigitalTwin(DIGITAL_TWIN_ID);
            string jsonFormatted = JValue.Parse(digitalTwin).ToString(Formatting.Indented);

            Console.WriteLine("Got the status of the digital twin successfully, the returned string was:");
            Console.WriteLine(jsonFormatted);

            Console.WriteLine("Enter any key to finish");
            Console.ReadLine();

            return 0;
        }

        private static void verifyInputs()
        {
            if (isNullOrEmpty(CONNECTION_STRING) || isNullOrEmpty(DIGITAL_TWIN_ID))
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
                "DIGITAL_TWIN_ID - your digital twin id to invoke the command onto\n";
    }
}
