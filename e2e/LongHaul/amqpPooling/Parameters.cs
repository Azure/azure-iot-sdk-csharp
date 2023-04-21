// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CommandLine;
using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.Devices.LongHual.AmqpPooling
{
    internal class Parameters
    {
        [Option(
            'c',
            "IoTHubConnectionString",
            Required = false,
            HelpText = "The connection string of the IoT hub instance to connect to.")]
        public string IotHubConnectionString { get; set; } = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");

        [Option(
            'd',
            "DevicesCountNumber",
            Default = 3,
            Required = false,
            HelpText = "The count number of how many devices to register in the IoT hub.")]
        public int DevicesCountNumber { get; set; }

        [Option(
            'p',
            "DeviceTransportProtocol",
            Default = IotHubClientTransportProtocol.Tcp,
            Required = false,
            HelpText = "The protocol over which a transport for devices to communicate with the IoT hub (i.e., Tcp, WebSocket).")]
        public IotHubClientTransportProtocol DeviceTransportProtocol { get; set; }

        [Option(
            's',
            "AmqpPoolingSize",
            Default = 3,
            Required = false,
            HelpText = "The size of Amqp connection pool.")]
        public int AmqpPoolingSize { get; set; }

        [Option(
            'i',
            "InstrumentationKey",
            Required = false,
            HelpText = "The instrumentation key string for application insights.")]
        public string InstrumentationKey { get; set; } = Environment.GetEnvironmentVariable("APPLICATION_INSIGHTS_INSTRUMENTATION_KEY");

        internal IotHubClientTransportSettings GetTransportSettingsWithPooling()
        {
            return new IotHubClientAmqpSettings(DeviceTransportProtocol)
            {
                ConnectionPoolSettings = new AmqpConnectionPoolSettings
                {
                    MaxPoolSize = unchecked((uint)AmqpPoolingSize),
                    UsePooling = true,
                }
            };
        }

        internal bool Validate()
        {
            return !string.IsNullOrWhiteSpace(IotHubConnectionString)
                && !string.IsNullOrWhiteSpace(InstrumentationKey)
                && DevicesCountNumber > 0
                && AmqpPoolingSize > 0;
        }
    }
}
