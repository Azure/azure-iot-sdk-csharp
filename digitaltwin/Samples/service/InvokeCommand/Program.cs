using Microsoft.Azure.Devices.DigitalTwin.Service;
using System;

namespace Sample
{
    /// <summary>
    /// Sample code that demonstrates how to invoke a command on a digital twin
    /// </summary>
    class Program
    {
        private static string IOTHUB_CONNECTION_STRING = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");
        private static string DEVICE_ID = Environment.GetEnvironmentVariable("DEVICE_ID");
        private static string INTERFACE_INSTANCE_NAME = Environment.GetEnvironmentVariable("INTERFACE_INSTANCE_NAME");
        private static string COMMAND_NAME = Environment.GetEnvironmentVariable("COMMAND_NAME");
        private static string PAYLOAD = Environment.GetEnvironmentVariable("PAYLOAD");

        public static int Main(string[] args)
        {
            verifyInputs();

            var digitalTwinServiceClient = new DigitalTwinServiceClient(IOTHUB_CONNECTION_STRING);

            Console.WriteLine("Invoking " + COMMAND_NAME + " on device " + DEVICE_ID + " with interface instance name " + INTERFACE_INSTANCE_NAME);

            var digitalTwinCommandResponse = digitalTwinServiceClient.InvokeCommand(DEVICE_ID, INTERFACE_INSTANCE_NAME, COMMAND_NAME, PAYLOAD).Value;

            Console.WriteLine("Command " + COMMAND_NAME + " invoked on the device successfully, the returned status was " + digitalTwinCommandResponse.Status + " and the request id was " + digitalTwinCommandResponse.RequestId);
            Console.WriteLine("The returned payload was ");
            Console.WriteLine(digitalTwinCommandResponse.Payload);

            Console.WriteLine("Enter any key to finish");
            Console.ReadLine();

            return 0;
        }

        private static void verifyInputs()
        {
            if (isNullOrEmpty(IOTHUB_CONNECTION_STRING) || isNullOrEmpty(DEVICE_ID) || isNullOrEmpty(INTERFACE_INSTANCE_NAME) || isNullOrEmpty(COMMAND_NAME))
            {
                Console.WriteLine(usage);
                Console.WriteLine("Enter any key to finish");
                Console.ReadLine();
                Environment.Exit(0);
            }

            if (string.IsNullOrEmpty(PAYLOAD))
            {
                PAYLOAD = null;
            }
        }

        private static bool isNullOrEmpty(String s)
        {
            return s == null || s.Length == 0;
        }

        private static String usage = "In order to run this sample, you must set environment variables for \n" +
                "IOTHUB_CONNECTION_STRING - Your IoT Hub's connection string\n" +
                "DEVICE_ID - The ID of the device to invoke the command onto\n" +
                "INTERFACE_INSTANCE_NAME - the interface the command belongs to\n" +
                "COMMAND_NAME - the name of the command to invoke on your digital twin\n" +
                "PAYLOAD - (optional) the json payload to include in the command";
    }
}
