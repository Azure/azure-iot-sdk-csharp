// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class Program
    {
        // String containing Hostname, Device Id, Module Id & Device Key in one of the following formats:
        //  "HostName=<iothub_host_name>;DeviceId=<device_id>;ModuleId=<module_id>;SharedAccessKey=<device_key>"
        //  "HostName=<iothub_host_name>;CredentialType=SharedAccessSignature;DeviceId=<device_id>;ModuleId=<module_id>;SharedAccessSignature=SharedAccessSignature sr=<iot_host>/devices/<device_id>&sig=<token>&se=<expiry_time>";
        // For this sample either
        // - pass this value as a command-prompt argument
        // - set the IOTHUB_MODULE_CONN_STRING environment variable 
        // - create a launchSettings.json (see launchSettings.json.template) containing the variable
        private static string s_moduleConnectionString = Environment.GetEnvironmentVariable("IOTHUB_MODULE_CONN_STRING");

        // Select one of the following transports used by ModuleClient to connect to IoT Hub.
        private static TransportType s_transportType = TransportType.Amqp;
        //private static TransportType s_transportType = TransportType.Mqtt;
        //private static TransportType s_transportType = TransportType.Http1;
        //private static TransportType s_transportType = TransportType.Amqp_WebSocket_Only;
        //private static TransportType s_transportType = TransportType.Mqtt_WebSocket_Only;

        public static int Main(string[] args)
        {
            if (string.IsNullOrEmpty(s_moduleConnectionString) && args.Length > 0)
            {
                s_moduleConnectionString = args[0];
            }

            ModuleClient moduleClient = ModuleClient.CreateFromConnectionString(s_moduleConnectionString, s_transportType);

            if (moduleClient == null)
            {
                Console.WriteLine("Failed to create ModuleClient!");
                return 1;
            }

            var sample = new TwinSample(moduleClient);
            sample.RunSampleAsync().GetAwaiter().GetResult();

            Console.WriteLine("Done.\n");
            return 0;
        }
    }
}
