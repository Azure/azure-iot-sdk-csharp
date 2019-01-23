// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public static class Program
    {
        // String containing Hostname, Device Id & Device Key in one of the following formats:
        //  "HostName=<iothub_host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>"
        //  "HostName=<iothub_host_name>;CredentialType=SharedAccessSignature;DeviceId=<device_id>;SharedAccessSignature=SharedAccessSignature sr=<iot_host>/devices/<device_id>&sig=<token>&se=<expiry_time>";

        // For this sample either
        // - pass this value as a command-prompt argument
        // - set the IOTHUB_DEVICE_CONN_STRING environment variable 
        // - create a launchSettings.json (see launchSettings.json.template) containing the variable
        private static string s_deviceConnectionString = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONN_STRING");

        // Select one of the following transports used by DeviceClient to connect to IoT Hub.
        private static TransportType s_transportType = TransportType.Amqp;
        //private static TransportType s_transportType = TransportType.Mqtt;
        //private static TransportType s_transportType = TransportType.Amqp_WebSocket_Only;
        //private static TransportType s_transportType = TransportType.Mqtt_WebSocket_Only;

        public static int Main(string[] args)
        {
            if (string.IsNullOrEmpty(s_deviceConnectionString) && args.Length > 0)
            {
                s_deviceConnectionString = args[0];
            }

            if (string.IsNullOrEmpty(s_deviceConnectionString))
            {
                Console.WriteLine("Please provide a device connection string");
                Console.WriteLine("Usage: DeviceClientC2DStreamingSample [iotHubDeviceConnString]");
                return 1;
            }

            using (DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(s_deviceConnectionString, s_transportType))
            {
                if (deviceClient == null)
                {
                    Console.WriteLine("Failed to create DeviceClient!");
                    return 1;
                }

                var sample = new DeviceStreamSample(deviceClient);
                sample.RunSampleAsync().GetAwaiter().GetResult();
            }

            Console.WriteLine("Done.\n");
            return 0;
        }
    }
}
