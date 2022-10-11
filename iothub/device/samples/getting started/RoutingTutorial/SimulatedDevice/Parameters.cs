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
    /// Parameters for the application.
    /// </summary>
    internal class Parameters
    {
        [Option(
            'c',
            "PrimaryConnectionString",
            Required = true,
            HelpText = "The primary connection string for the device to simulate.")]
        public string PrimaryConnectionString { get; set; }

        [Option(
            "ReadTheFile",
            Required = false,
            Default = false,
            HelpText = "If this is false, it will submit messages to the IoT hub. If this is true, it will read one of the output files and convert it to ASCII.")]
        public bool ReadTheFile { get; set; } = false;

        [Option(
            "FilePath",
            Required = false,
            HelpText = "If this is false, it will submit messages to the IoT hub. If this is true, it will read one of the output files and convert it to ASCII.")]
        public string FilePath { get; set; }

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
