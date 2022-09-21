// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CommandLine;
using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.Devices.Provisioning.Client.Samples
{
    /// <summary>
    /// The transport to use.
    /// </summary>
    public enum Transport
    {
        /// <summary>
        /// MQTT 3.1.1.
        /// </summary>
        Mqtt,

        /// <summary>
        /// AMQP
        /// </summary>
        Amqp,
    };

    /// <summary>
    /// The type of enrollment for a device in the provisioning service.
    /// </summary>
    public enum EnrollmentType
    {
        /// <summary>
        ///  Enrollment for a single device.
        /// </summary>
        Individual,

        /// <summary>
        /// Enrollment for a group of devices.
        /// </summary>
        Group,
    }

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
            HelpText = "The primary key of the individual enrollment or the derived primary key of the group enrollment. See the ComputeDerivedSymmetricKeySample for how to generate the derived key.")]
        public string PrimaryKey { get; set; }

        [Option(
            'e',
            "EnrollmentType",
            Default = EnrollmentType.Individual,
            HelpText = "The type of enrollment: Individual or Group")]
        public EnrollmentType EnrollmentType { get; set; }

        [Option(
            "GlobalDeviceEndpoint",
            Default = "global.azure-devices-provisioning.net",
            HelpText = "The global endpoint for devices to connect to.")]
        public string GlobalDeviceEndpoint { get; set; }

        [Option(
            "Transport",
            Default = Transport.Mqtt,
            HelpText = "The transport to use for the connection.")]
        public Transport Transport { get; set; }

        [Option(
            "TransportProtocol",
            Default = ProvisioningClientTransportProtocol.Tcp,
            HelpText = "The transport to use to communicate with the device provisioning instance.")]
        public ProvisioningClientTransportProtocol TransportProtocol { get; set; }

        internal ProvisioningTransportHandler GetTransportHandler()
        {
            return Transport switch
            {
                Transport.Mqtt => new ProvisioningTransportHandlerMqtt(TransportProtocol),
                Transport.Amqp => new ProvisioningTransportHandlerAmqp(TransportProtocol),
                _ => throw new NotSupportedException($"Unsupported transport type {Transport}/{TransportProtocol}"),
            };
        }

        internal IotHubClientTransportSettings GetHubTransportSettings()
        {
            IotHubClientTransportProtocol protocol = TransportProtocol == ProvisioningClientTransportProtocol.Tcp
                ? IotHubClientTransportProtocol.Tcp
                : IotHubClientTransportProtocol.WebSocket;

            return Transport switch
            {
                Transport.Mqtt => new IotHubClientMqttSettings(protocol),
                Transport.Amqp => new IotHubClientAmqpSettings(protocol),
                _ => throw new NotSupportedException($"Unsupported transport type {Transport}/{TransportProtocol}"),
            };
        }
    }
}
