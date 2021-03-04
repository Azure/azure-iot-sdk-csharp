// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace X509DeviceCertWithChainSample
{
    /// <summary>
    /// Parameters for the application.
    /// </summary>
    internal class Parameters
    {
        [Option(
            'h',
            "hostName",
            Required = true,
            HelpText = "The hostname of IotHub.")]
        public string HostName { get; set; }

        [Option(
            'd',
            "deviceName",
            Required = true,
            HelpText = "The name of the device.")]
        public string DeviceName { get; set; }

        [Option(
            "devicePfxPassword",
            Required = true,
            HelpText = "The password of device certificate.")]
        public string DevicePfxPassword { get; set; }

        [Option(
            "rootCertPath",
            Required = true,
            HelpText = "Path to rootCA certificate.")]
        public string RootCertPath { get; set; }

        [Option(
            "intermediate1CertPassword",
            Required = true,
            HelpText = "Path to intermediate 1 certificate.")]
        public string Intermediate1CertPath { get; set; }

        [Option(
            "intermediate2CertPassword",
            Required = true,
            HelpText = "Path to intermediate 2 certificate.")]
        public string Intermediate2CertPath { get; set; }

        [Option(
            "devicePfxPath",
            Required = true,
            HelpText = "Path to device pfx.")]
        public string DevicePfxPath { get; set; }

        [Option(
            't',
            "TransportType",
            Default = TransportType.Mqtt_Tcp_Only,
            Required = false,
            HelpText = "The transport to use to communicate with the IoT Hub. Possible values include Mqtt_Tcp_Only, Amqp_Tcp_only.")]
        public TransportType TransportType { get; set; }
    }
}
