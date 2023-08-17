// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

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
            'r',
            "RegistrationId",
            HelpText = "The registration Id from the individual enrollment. For normal runs, this is required.")]
        public string RegistrationId { get; set; }

        [Option(
            'g',
            "GlobalDeviceEndpoint",
            Default = "dev1.eastus.device.discovery.edgeprov-dev.azure.net",
            HelpText = "The global endpoint for devices to connect to.")]
        public string GlobalDeviceEndpoint { get; set; }
    }
}
