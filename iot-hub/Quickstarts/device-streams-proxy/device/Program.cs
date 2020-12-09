// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Threading;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public static class Program
    {
        // String containing Host Name, Device Id & Device Key in one of the following formats:
        //  "HostName=<iothub_host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>"
        //  "HostName=<iothub_host_name>;CredentialType=SharedAccessSignature;DeviceId=<device_id>;SharedAccessSignature=SharedAccessSignature sr=<iot_host>/devices/<device_id>&sig=<token>&se=<expiry_time>";

        // For this sample either
        // - pass this value as a command-prompt argument
        // - set the IOTHUB_DEVICE_CONN_STRING environment variable 
        // - create a launchSettings.json (see launchSettings.json.template) containing the variable
        private static string s_deviceConnectionString = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONN_STRING");

        // Host name or IP address of a service the device will proxy traffic to.
        // - pass this value as a command-prompt argument
        // - set the REMOTE_HOST_NAME environment variable 
        // - create a launchSettings.json (see launchSettings.json.template) containing the variable
        private static string s_hostName = Environment.GetEnvironmentVariable("REMOTE_HOST_NAME");

        // Port of a service the device will proxy traffic to.
        // - pass this value as a command-prompt argument
        // - set the REMOTE_PORT environment variable 
        // - create a launchSettings.json (see launchSettings.json.template) containing the variable
        private static string s_port = Environment.GetEnvironmentVariable("REMOTE_PORT");

        // Select one of the following transports used by DeviceClient to connect to IoT Hub.
        private static readonly TransportType s_transportType = TransportType.Amqp;
        //private static readonly TransportType s_transportType = TransportType.Mqtt;
        //private static readonly TransportType s_transportType = TransportType.Amqp_WebSocket_Only;
        //private static readonly TransportType s_transportType = TransportType.Mqtt_WebSocket_Only;

        public static int Main(string[] args)
        {
            if (string.IsNullOrEmpty(s_deviceConnectionString) && args.Length > 0)
            {
                s_deviceConnectionString = args[0];
            }

            if (string.IsNullOrEmpty(s_hostName) && args.Length > 1)
            {
                s_hostName = args[1];
            }

            if (string.IsNullOrEmpty(s_port) && args.Length > 2)
            {
                s_port = args[2];
            }

            if (string.IsNullOrEmpty(s_deviceConnectionString) ||
                string.IsNullOrEmpty(s_hostName) ||
                string.IsNullOrEmpty(s_port))
            {
                Console.WriteLine("Please provide a connection string, target host and port");
                Console.WriteLine("Usage: DeviceLocalProxyC2DStreamingSample [iotHubConnString] [targetServiceHostName] [targetServicePort]");
                return 1;
            }

            int port = int.Parse(s_port, CultureInfo.InvariantCulture);

            using (DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(s_deviceConnectionString, s_transportType))
            {
                if (deviceClient == null)
                {
                    Console.WriteLine("Failed to create DeviceClient!");
                    return 1;
                }

                var sample = new DeviceStreamSample(deviceClient, s_hostName, port);
                sample.RunSampleAsync(new CancellationTokenSource()).GetAwaiter().GetResult();
            }

            Console.WriteLine("Done.\n");
            return 0;
        }
    }
}
