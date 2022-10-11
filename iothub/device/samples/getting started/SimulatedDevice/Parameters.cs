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
    /// Command line parameters for the SimulatedDevice sample.
    /// </summary>
    internal class Parameters
    {
        [Option(
            'c',
            "DeviceConnectionString",
            Required = true,
            HelpText = "The IoT hub device connection string. This is available by clicking any existing device under the 'Devices' blade in the Azure portal." +
            "\nDefaults to value of environment variable IOTHUB_DEVICE_CONNECTION_STRING.")]
        public string DeviceConnectionString { get; set; } = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONNECTION_STRING");

        [Option(
            't',
            "Transport",
            Default = Transport.Mqtt,
            Required = false,
            HelpText = "The transport to use for the connection.")]
        public Transport Transport { get; set; }

        [Option(
            "TransportProtocol",
            Default = IotHubClientTransportProtocol.Tcp,
            HelpText = "The transport to use to communicate with the device provisioning instance.")]
        public IotHubClientTransportProtocol TransportProtocol { get; set; }

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
