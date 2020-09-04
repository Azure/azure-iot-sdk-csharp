// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class Program
    {
        // Both device connection strings with the following formats:
        //  "HostName=<iothub_host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>"
        //  "HostName=<iothub_host_name>;CredentialType=SharedAccessSignature;DeviceId=<device_id>;SharedAccessSignature=SharedAccessSignature sr=<iot_host>/devices/<device_id>&sig=<token>&se=<expiry_time>";

        // For this sample either
        // - pass these values as a command-prompt argument
        // - set the IOTHUB_DEVICE_CONN_STRING and IOTHUB_DEVICE_CONN_STRING2 environment variables
        // - create a launchSettings.json (see launchSettings.json.template) containing the variable
        private static string s_deviceConnectionString1 = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONN_STRING");
        private static string s_deviceConnectionString2 = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONN_STRING2");

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
            if ((string.IsNullOrEmpty(s_deviceConnectionString1)
                    || string.IsNullOrEmpty(s_deviceConnectionString1))
                && args.Length > 1)
            {
                s_deviceConnectionString1 = args[0];
                s_deviceConnectionString2 = args[1];
            }

            if (string.IsNullOrEmpty(s_deviceConnectionString1)
                || string.IsNullOrEmpty(s_deviceConnectionString1))
            {
                Console.WriteLine("Both PRIMARY and SECONDARY connection strings of a device are required for this sample.");
                return 1;
            }

            var sample = new KeyRolloverSample(s_deviceConnectionString1, s_deviceConnectionString2, GetTransportType(args));
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
            if (args.Length > 2
                && Enum.TryParse(args[2], true, out transportType))
            {
                return transportType;
            }

            // otherwise default to MQTT
            return TransportType.Mqtt;
        }
    }
}
