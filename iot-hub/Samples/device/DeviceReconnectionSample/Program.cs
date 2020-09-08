// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
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
        private static string s_deviceConnectionStringPrimary = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONN_STRING");
        private static string s_deviceConnectionStringSecondary = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONN_STRING_SECONDARY");

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
            // Create a console logger, that logs all events that are categorized at Debug level or higher.
            // For additional details, see https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger?view=dotnet-plat-ext-3.1.
            ILoggerFactory loggerFactory = new LoggerFactory();

            loggerFactory.AddColorConsoleLogger(
                new ColorConsoleLoggerConfiguration
                {
                    MinLogLevel = LogLevel.Debug,
                });
            var logger = loggerFactory.CreateLogger<Program>();

            const string SdkEventProviderPrefix = "Microsoft-Azure-";
            // Instantiating this seems to do all we need for outputing SDK events to our console log
            _ = new ConsoleEventListener(SdkEventProviderPrefix, logger);

            var connectionStrings = new List<string>(2);
            if (string.IsNullOrEmpty(s_deviceConnectionStringPrimary) && args.Length > 0)
            {
                s_deviceConnectionStringPrimary = args[0];
            }
            connectionStrings.Add(s_deviceConnectionStringPrimary);

            if (string.IsNullOrEmpty(s_deviceConnectionStringSecondary) && args.Length > 1)
            {
                s_deviceConnectionStringSecondary = args[1];
            }
            if (!string.IsNullOrWhiteSpace(s_deviceConnectionStringSecondary))
            {
                connectionStrings.Add(s_deviceConnectionStringSecondary);
            }

            var sample = new DeviceReconnectionSample(connectionStrings, GetTransportType(args), logger);
            await sample.RunSampleAsync();

            logger.LogInformation("Done.");
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
