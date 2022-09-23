// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace ArmReadWrite
{
    /// <summary>
    /// Parameters for the application.
    /// </summary>
    internal class Parameters
    {
        [Option(
            'u',
            "IotHubHostName",
            Required = true,
            HelpText = "The URI for the IoT hub.")]
        public string IotHubHostName { get; set; }

        [Option(
            'd',
            "DeviceId",
            Required = true,
            HelpText = "The device Id that you assigned when registering the device.")]
        public string DeviceId { get; set; }

        // This is the primary key for the device. This is in the portal.
        // Find your IoT hub in the portal > IoT devices > select your device > copy the key.
        [Option(
            'k',
            "DeviceKey",
            Required = true,
            HelpText = "Find your IoT hub in the portal > IoT devices > select your device > copy the key.")]
        public string DeviceKey { get; set; }

        [Option(
            "ReadTheFile",
            Required = false,
            HelpText = "If this is false, it will submit messages to the IoT hub. If this is true, it will read one of the output files and convert it to ASCII.")]
        public bool ReadTheFile { get; set; } = false;

        [Option(
            "FilePath",
            Required = false,
            HelpText = "If this is false, it will submit messages to the IoT hub. If this is true, it will read one of the output files and convert it to ASCII.")]
        public string FilePath { get; set; }

        // TODO: validate filepath when readthefile is true
    }
}
