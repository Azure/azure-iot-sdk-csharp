// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CommandLine;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public enum Transport
    {
        Mqtt,
        Amqp
    };

    /// <summary>
    /// Command line parameters for the SimulatedDeviceWithCommand sample
    /// </summary>
    internal class Parameters
    {
        [Option(
            'c',
            "DeviceConnectionString",
            Required = true,
             HelpText = "The IoT hub device connection string. This is available under the 'Devices' in the Azure portal." +
            "\nDefaults to value of environment variable IOTHUB_DEVICE_CONNECTION_STRING.")]
        public string DeviceConnectionString { get; set; } = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONNECTION_STRING");

        [Option(
            't',
            "TransportType",
            Default = Transport.Mqtt,
            Required = false,
            HelpText = "The transport (except HTTP) to use to communicate with the IoT hub. Possible values are Mqtt are Amqp.")]
        public Transport Transport { get; set; }

        [Option(
           "TransportProtocol",
           Default = IotHubClientTransportProtocol.Tcp,
           HelpText = "The transport to use to communicate with the device client.")]
        public IotHubClientTransportProtocol TransportProtocol { get; set; }

        [Option(
            'r',
            "Application running time (in seconds)",
            Required = false,
            HelpText = "The running time for this console application. Leave it unassigned to run the application until it is explicitly canceled using Control+C.")]
        public double? ApplicationRunningTime { get; set; }

        internal IotHubClientTransportSettings GetHubTransportSettings()
        {
            return Transport switch
            {
                Transport.Mqtt => new IotHubClientMqttSettings(TransportProtocol),
                Transport.Amqp => new IotHubClientAmqpSettings(TransportProtocol),
                _ => throw new NotSupportedException($"Unsupported transport type {Transport}/{TransportProtocol}"),
            };
        }
    }
}
