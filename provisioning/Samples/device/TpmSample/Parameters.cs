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
            'e',
            "GetTpmEndorsementKey",
            HelpText = "Gets the TPM endorsement key. Use this option by itself to get the EK needed to create a DPS individual enrollment.")]
        public bool GetTpmEndorsementKey { get; set; }

        [Option(
            'u',
            "UseTpmSimulator",
            HelpText = "Runs the TPM simulator - useful when the local device does not have a TPM chip.")]
        public bool UseTpmSimulator { get; set; }

        [Option(
            's',
            "IdScope",
            HelpText = "The Id Scope of the DPS instance. For normal runs, this is required.")]
        public string IdScope { get; set; }

        [Option(
            'r',
            "RegistrationId",
            HelpText = "The registration Id from the individual enrollment. For normal runs, this is required.")]
        public string RegistrationId { get; set; }

        [Option(
            'g',
            "GlobalDeviceEndpoint",
            Default = "global.azure-devices-provisioning.net",
            HelpText = "The global endpoint for devices to connect to.")]
        public string GlobalDeviceEndpoint { get; set; }

        [Option(
            't',
            "TransportType",
            Default = TransportType.Amqp,
            HelpText = "The transport to use to communicate with the device provisioning instance. Possible values include Amqp, Amqp_WebSocket_Only, Amqp_Tcp_only, and Http1.")]
        public TransportType TransportType { get; set; }
    }
}
