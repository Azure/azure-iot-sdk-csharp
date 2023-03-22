// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using CommandLine;

namespace Microsoft.Azure.IoT.Thief.Device
{
    public enum TransportType
    {
        Mqtt,
        Amqp,
    };

    internal class Parameters
    {
        [Option(
            'c',
            "ConnectionString",
            Required = false,
            HelpText = "The connection string for the device to simulate.")]
        public string ConnectionString { get; set; }

        [Option(
            'i',
            "InstrumentationKey",
            Required = false,
            HelpText = "The instrumentation key string for application insights.")]
        public string InstrumentationKey { get; set; }

        [Option(
            't',
            "Transport",
            Default = TransportType.Mqtt,
            Required = false,
            HelpText = "The transport to use for the connection.")]
        public TransportType Transport { get; set; }

        [Option(
            'p',
            "TransportProtocol",
            Default = IotHubClientTransportProtocol.Tcp,
            Required = false,
            HelpText = "The protocol over which a transport (i.e., MQTT, AMQP) communicates.")]
        public IotHubClientTransportProtocol TransportProtocol { get; set; }
    }
}
