// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

namespace Microsoft.Azure.Devices.Samples
{
    public static class Program
    {
        // The IoT Hub connection string. This is available under the "Shared access policies" in the Azure portal.

        // For this sample either:
        // - pass this value as a command-prompt argument
        // - set the IOTHUB_CONNECTION_STRING environment variable 
        // - create a launchSettings.json (see launchSettings.json.template) containing the variable
        private static string s_connectionString = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");

        // ID of the device to interact with.
        // - pass this value as a command-prompt argument
        // - set the DEVICE_ID environment variable 
        // - create a launchSettings.json (see launchSettings.json.template) containing the variable
        private static string s_deviceId = Environment.GetEnvironmentVariable("DEVICE_ID");

        // Local port this sample will open to proxy traffic to a device.
        // - pass this value as a command-prompt argument
        // - set the LOCAL_PORT environment variable 
        // - create a launchSettings.json (see launchSettings.json.template) containing the variable
        private static string s_port = Environment.GetEnvironmentVariable("LOCAL_PORT");

        // Select one of the following transports used by ServiceClient to connect to IoT Hub.
        private static readonly TransportType s_transportType = TransportType.Amqp;
        //private static readonly TransportType s_transportType = TransportType.Amqp_WebSocket_Only;

        public static int Main(string[] args)
        {
            if (string.IsNullOrEmpty(s_connectionString) && args.Length > 0)
            {
                s_connectionString = args[0];
            }

            if (string.IsNullOrEmpty(s_deviceId) && args.Length > 1)
            {
                s_deviceId = args[1];
            }

            if (string.IsNullOrEmpty(s_port) && args.Length > 2)
            {
                s_port = args[2];
            }

            if (string.IsNullOrEmpty(s_connectionString) ||
                string.IsNullOrEmpty(s_deviceId) ||
                string.IsNullOrEmpty(s_port))
            {
                Console.WriteLine("Please provide a connection string, device ID and local port");
                Console.WriteLine("Usage: ServiceLocalProxyC2DStreamingSample [iotHubConnString] [deviceId] [localPortNumber]");
                return 1;
            }

            int port = int.Parse(s_port, CultureInfo.InvariantCulture);

            using (ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(s_connectionString, s_transportType))
            {
                var sample = new DeviceStreamSample(serviceClient, s_deviceId, port);
                sample.RunSampleAsync().GetAwaiter().GetResult();
            }

            Console.WriteLine("Done.\n");
            return 0;
        }
    }
}
