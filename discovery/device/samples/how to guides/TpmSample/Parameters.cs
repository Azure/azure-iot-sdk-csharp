// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace Microsoft.Azure.Devices.Discovery.Client.Samples
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
            'd',
            "DiscoveryDeviceEndpoint",
            Default = "int1.eastus.device.discovery.edgeprov-dev.azure.net",
            HelpText = "The discovery service endpoint for devices to connect to.")]
        public string DiscoveryDeviceEndpoint { get; set; }

        [Option(
            'p',
            "ProvisioningDeviceEndpoint",
            Default = null,
            HelpText = "The provisioning service endpoint for devices to connect to. Overrides edge provisioning endpoint from discovery service")]
        public string ProvisioningDeviceEndpoint { get; set; }
    }
}
