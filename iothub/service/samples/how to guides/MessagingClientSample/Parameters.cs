// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// Parameters for the application.
    /// </summary>
    internal class Parameters
    {
        [Option(
            'c',
            "HubConnectionString",
            Required = true,
            HelpText = "The connection string of the IoT hub instance to connect to.")]
        public string HubConnectionString { get; set; }

        [Option(
            'd',
            "deviceId",
            Required = true,
            HelpText = "The device Id to send the messages to.")]
        public string DeviceId { get; set; }
    }
}