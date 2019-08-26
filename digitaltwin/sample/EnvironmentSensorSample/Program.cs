// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Client;
using Azure.Iot.DigitalTwin.Device;

namespace EnvironmentalSensorSample
{
    class Program
    {
        // String containing Hostname, Device Id & Device Key in one of the following formats:
        //  "HostName=<iothub_host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>"
        //  "HostName=<iothub_host_name>;CredentialType=SharedAccessSignature;DeviceId=<device_id>;SharedAccessSignature=SharedAccessSignature sr=<iot_host>/devices/<device_id>&sig=<token>&se=<expiry_time>";
        private static string s_deviceConnectionString = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONN_STRING");
        
        // Select one of the following transports used by DeviceClient to connect to IoT Hub.
        private static TransportType s_transportType = TransportType.Mqtt;

        public static int Main(string[] args)
        {
            if (string.IsNullOrEmpty(s_deviceConnectionString) && args.Length > 0)
            {
                s_deviceConnectionString = args[0];
            }
            
            using (var deviceClient = DeviceClient.CreateFromConnectionString(s_deviceConnectionString, s_transportType))
            {
                DigitalTwinClient digitalTwinClient = new DigitalTwinClient(deviceClient);

                if (digitalTwinClient == null)
                {
                    Console.WriteLine("Failed to create DeviceClient!");
                    return 1;
                }

                var sample = new DigitalTwinClientSample(digitalTwinClient);
                sample.RunSampleAsync().GetAwaiter().GetResult();
            }

            Console.WriteLine("Waiting to receive updates from cloud...\n");
            Console.ReadLine();
            return 0;
        }
    }
}
