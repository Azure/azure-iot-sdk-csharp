using Microsoft.Azure.Devices.DigitalTwin.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace UpdateProperty
{
    /// <summary>
    /// Sample code that demonstrates updating properties on a digital twin. The update properties api allows 
    /// you to batch updates such that you can update multiple properties across one to many interfaces on
    /// a single digital twin
    /// </summary>
    class Program
    {
        private static string IOTHUB_CONNECTION_STRING = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");
        private static string DEVICE_ID = Environment.GetEnvironmentVariable("DEVICE_ID");
        private static string INTERFACE_INSTANCE_NAME = Environment.GetEnvironmentVariable("INTERFACE_INSTANCE_NAME");
        private static string PROPERTY_NAME = Environment.GetEnvironmentVariable("PROPERTY_NAME");
        private static string PROPERTY_VALUE = Environment.GetEnvironmentVariable("PROPERTY_VALUE");

        public static int Main(string[] args)
        {
            verifyInputs();

            var digitalTwinServiceClient = new DigitalTwinServiceClient(IOTHUB_CONNECTION_STRING);

            Console.WriteLine("Updating " + PROPERTY_NAME + " on device " + DEVICE_ID + " with interface instance name " + INTERFACE_INSTANCE_NAME);

            string digitalTwinPatch = buildUpdatePatchSinglePropertyOnSingleInterface(PROPERTY_NAME, PROPERTY_VALUE);

            string digitalTwin = digitalTwinServiceClient.UpdateDigitalTwinProperties(DEVICE_ID, INTERFACE_INSTANCE_NAME, digitalTwinPatch).Value;

            string jsonFormatted = JValue.Parse(digitalTwin).ToString(Formatting.Indented);

            Console.WriteLine("Property updated on the device successfully, the returned payload was");
            Console.WriteLine(jsonFormatted);

            Console.WriteLine("Enter any key to finish");
            Console.ReadLine();

            return 0;
        }

        private static string buildUpdatePatchSinglePropertyOnSingleInterface(string propertyName, string propertyValue)
        {
            string patch =
                "{" +
                "  \"properties\": {" +
                "    \"" + propertyName + "\": {" +
                "      \"desired\": {" +
                "        \"value\": \"" + propertyValue + "\"" +
                "      }" +
                "    }" +
                "  }" +
                "}";

            return patch;
        }

        private static string buildUpdatePatchMultiplePropertiesOnSameInterface(string propertyName, string propertyValue, string property2Name, string property2Value)
        {
            string patch =
                "{" +
                "  \"properties\": {" +
                "    \"" + propertyName + "\": {" +
                "      \"desired\": {" +
                "        \"value\": \"" + propertyValue + "\"" +
                "      }" +
                "    }," +
                "    \"" + property2Name + "\": {" +
                "      \"desired\": {" +
                "        \"value\": \"" + property2Value + "\"" +
                "      }" +
                "    }" +
                "  }" +
                "}";

            return patch;
        }

        private static void verifyInputs()
        {
            if (isNullOrEmpty(IOTHUB_CONNECTION_STRING) || isNullOrEmpty(DEVICE_ID) || isNullOrEmpty(INTERFACE_INSTANCE_NAME) || isNullOrEmpty(PROPERTY_NAME) || isNullOrEmpty(PROPERTY_VALUE))
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
                "DEVICE_ID - The ID of the device to update the property on\n" +
                "INTERFACE_INSTANCE_NAME - the interface the property belongs to\n" +
                "PROPERTY_NAME - the name of the property to update on your digital twin\n" +
                "PROPERTY_VALUE - the value of the property to set";
    }
}
