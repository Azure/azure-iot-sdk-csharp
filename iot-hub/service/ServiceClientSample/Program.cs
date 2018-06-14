// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Samples
{
    public class Program
    {
        // Either set the IOTHUB_DEVICE_CONN_STRING environment variable or within launchSettings.json:
        private static string s_connectionString = Environment.GetEnvironmentVariable("IOTHUB_CONN_STRING_CSHARP");

        // Select one of the following transports used by ServiceClient to connect to IoT Hub.
        private static TransportType s_transportType = TransportType.Amqp;
        //private static TransportType s_transportType = TransportType.Amqp_WebSocket_Only;

        public static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("\nUsage: \n");
                Console.WriteLine("\tServiceClientSample <deviceID> [connectionString]\n");
                return 1;
            }

            string deviceId = args[0];

            if (string.IsNullOrEmpty(s_connectionString) && args.Length > 1)
            {
                s_connectionString = args[1];
            }

            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(s_connectionString, s_transportType);

            var sample = new ServiceClientSample(serviceClient);
            sample.RunSampleAsync(deviceId).GetAwaiter().GetResult();

            Console.WriteLine("Done.\n");
            return 0;
        }
    }
}
