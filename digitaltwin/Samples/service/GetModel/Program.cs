using Microsoft.Azure.Devices.DigitalTwin.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Sample
{
    /// <summary>
    /// Sample code demonstrating how to lookup a model (interface or device capability model) from the model repo
    /// </summary>
    class Program
    {
        private static string IOTHUB_CONNECTION_STRING = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");
        private static String MODEL_ID = Environment.GetEnvironmentVariable("MODEL_ID");

        public static int Main(string[] args)
        {
            verifyInputs();

            var digitalTwinServiceClient = new DigitalTwinServiceClient(IOTHUB_CONNECTION_STRING);

            Console.WriteLine("Getting model with id " + MODEL_ID + " from the model repo...");

            string modelDefinition = digitalTwinServiceClient.GetModel(MODEL_ID);
            string jsonFormatted = JValue.Parse(modelDefinition).ToString(Formatting.Indented);

            Console.WriteLine("Successfully retrieved the model, the definition is:");
            Console.WriteLine(jsonFormatted);

            Console.WriteLine("Enter any key to finish");
            Console.ReadLine();

            return 0;
        }

        private static void verifyInputs()
        {
            if (isNullOrEmpty(IOTHUB_CONNECTION_STRING) || isNullOrEmpty(MODEL_ID))
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
                "MODEL_ID - Your model id to look up the full definition for";
    }
}
