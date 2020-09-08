// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class Program
    {
        // String containing Hostname, Device Id & Device Key in one of the following formats:
        //  "HostName=<iothub_host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>"
        //  "HostName=<iothub_host_name>;CredentialType=SharedAccessSignature;DeviceId=<device_id>;SharedAccessSignature=SharedAccessSignature sr=<iot_host>/devices/<device_id>&sig=<token>&se=<expiry_time>";

        // For this sample either
        // - pass this value as a command-prompt argument
        // - set the IOTHUB_DEVICE_CONN_STRING environment variable 
        // - create a launchSettings.json (see launchSettings.json.template) containing the variable
        private static string s_deviceConnectionString = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONN_STRING");

        // Specify one of the following transports used by DeviceClient to connect to IoT Hub.
        //   Mqtt
        //   Mqtt_WebSocket_Only
        //   Mqtt_Tcp_Only
        //   Amqp
        //   Amqp_WebSocket_Only
        //   Amqp_Tcp_only
        //   Http1
        private static readonly string s_transportType = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_TRANSPORT_TYPE");

        public static async Task<int> Main(string[] args)
        {
            if (string.IsNullOrEmpty(s_deviceConnectionString) && args.Length > 0)
            {
                s_deviceConnectionString = args[0];
            }

            using var deviceClient = DeviceClient.CreateFromConnectionString(s_deviceConnectionString, GetTransportType(args));
            var sample = new TwinSample(deviceClient);
            await sample.RunSampleAsync();

            Console.WriteLine("Done.");
            return 0;
        }

        private static TransportType GetTransportType(string[] args)
        {
            // Check environment variable for transport type
            if (Enum.TryParse(s_transportType, true, out TransportType transportType))
            {
                return transportType;
            }

            // then check argument for transport type
            if (args.Length > 1
                && Enum.TryParse(args[1], true, out transportType))
            {
                return transportType;
            }

            // otherwise default to MQTT
            return TransportType.Mqtt;
        }
    }
}
