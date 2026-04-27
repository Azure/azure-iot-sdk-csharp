// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CommandLine;
using Microsoft.Azure.Devices.Client;

namespace SimulatedDevice
{
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
            "TransportType",
            Default = TransportType.Mqtt,
            Required = false,
            HelpText = "The transport (except HTTP) to use to communicate with the IoT hub. Possible values include Mqtt, Mqtt_WebSocket_Only, Mqtt_Tcp_Only, Amqp, Amqp_WebSocket_Only, and Amqp_Tcp_Only.")]
        public TransportType TransportType { get; set; }
    }
}
