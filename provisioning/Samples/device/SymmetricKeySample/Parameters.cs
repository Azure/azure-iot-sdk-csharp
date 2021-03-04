// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.Devices.Provisioning.Client.Samples
{
    /// <summary>
    /// Parameters for the application
    /// </summary>
    internal class Parameters
    {
        [Option(
            's',
            "IdScope",
            Required = true,
            HelpText = "The Id Scope of the DPS instance")]
        public string IdScope { get; set; }

        [Option(
            'i',
            "Id",
            Required = true,
            HelpText = "The registration Id when using individual enrollment, or the desired device Id when using group enrollment.")]
        public string Id { get; set; }

        [Option(
            'p',
            "PrimaryKey",
            Required = true,
            HelpText = "The primary key of the individual or group enrollment.")]
        public string PrimaryKey { get; set; }

        [Option(
            'e',
            "EnrollmentType",
            Default = EnrollmentType.Individual,
            HelpText = "The type of enrollment: Individual or Group")]
        public EnrollmentType EnrollmentType { get; set; }

        [Option(
            'g',
            "GlobalDeviceEndpoint",
            Default = "global.azure-devices-provisioning.net",
            HelpText = "The global endpoint for devices to connect to.")]
        public string GlobalDeviceEndpoint { get; set; }

        [Option(
            't',
            "TransportType",
            Default = TransportType.Mqtt,
            HelpText = "The transport to use to communicate with the device provisioning instance. Possible values include Mqtt, Mqtt_WebSocket_Only, Mqtt_Tcp_Only, Amqp, Amqp_WebSocket_Only, Amqp_Tcp_only, and Http1.")]
        public TransportType TransportType { get; set; }
    }
}
