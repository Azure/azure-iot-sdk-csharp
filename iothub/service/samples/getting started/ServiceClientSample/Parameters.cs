// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace Microsoft.Azure.Devices.Samples
{
    /// <summary>
    /// Parameters for the application.
    /// </summary>

    internal class Parameters
    {
        [Option(
            'c',
            "IoTHubConnectionString",
            Required = true,
            HelpText = "The connection string of the IoT hub instance to connect to.")]
        public string IoTHubConnectionString { get; set; }

        [Option(
            'd',
            "DeviceId",
            Required = true,
            HelpText = "The Id of the device to connect to.")]
        public string DeviceId { get; set; }

        [Option(
            'r',
            "Application running time (in seconds)",
            Required = false,
            HelpText = "The running time for this console application. Leave it unassigned to run the application until it is explicitly canceled using Control+C.")]
        public double? ApplicationRunningTime { get; set; }

        [Option(
           "TransportProtocol",
           Default = IotHubTransportProtocol.Tcp,
           HelpText = "The transport to use to communicate with the device provisioning instance.")]
        public IotHubTransportProtocol TransportProtocol { get; set; }
    }
}
