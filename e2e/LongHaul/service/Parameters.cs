// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CommandLine;

namespace Microsoft.Azure.Devices.LongHaul.Service
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
            'i',
            "InstrumentationKey",
            Required = false,
            HelpText = "The instrumentation key string for application insights.")]
        public string InstrumentationKey { get; set; } = Environment.GetEnvironmentVariable("APPLICATION_INSIGHTS_INSTRUMENTATION_KEY");

        [Option(
            'd',
            "DeviceId",
            Required = false,
            HelpText = "The Id of the device to receive the direct method.")]
        public string DeviceId { get; set; } = "LongHaulDevice1";

        [Option(
            'p',
            "TransportProtocol",
            Required = false,
            Default = IotHubTransportProtocol.Tcp,
            HelpText = "The protocol over which a transport (i.e., HTTP) communicates.")]
        public IotHubTransportProtocol TransportProtocol { get; set; }

        internal bool Validate()
        {
            return !string.IsNullOrWhiteSpace(IotHubConnectionString)
                || !string.IsNullOrWhiteSpace(InstrumentationKey);
        }
    }
}
