// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace Microsoft.Azure.Devices.Samples
{
    /// <summary>
    /// Parameters for the application.
    /// </summary>

    public enum Transport
    {
        Amqp_WebSocket_Only = IotHubTransportProtocol.WebSocket,
        Amqp = IotHubTransportProtocol.Tcp
    };

    internal class Parameters
    {
        [Option(
            'c',
            "IoTHubConnectionString",
            Required = true,
            HelpText = "The connection string of the IoT hub instance to connect to.")]
        public string IoTHubConnectionString { get; set; }

        [Option(
            "Transport",
            Default = Transport.Amqp,
            Required = false,
            HelpText = "The transport to use to communicate with the IoT hub. Possible values include Amqp and Amqp_WebSocket_Only.")]
        public Transport Transport { get; set; }

        [Option(
            'r',
            "Application running time (in seconds)",
            Required = false,
            HelpText = "The running time for this console application. Leave it unassigned to run the application until it is explicitly canceled using Control+C.")]
        public double? ApplicationRunningTime { get; set; }

        [Option(
            'd',
            "Device Id",
            Required = false,
            HelpText = "The sample will only complete incoming notifications when the Device Id matches the origin of the notification. Do not set to complete all incoming notifications.")]
        public string DeviceId { get; set; }
    }
}
