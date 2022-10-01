// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using CommandLine;

namespace IndividualEnrollmentX509Sample
{
    internal class Parameters
    {
        [Option(
            'p',
            "CertificatePath",
            Required = true,
            HelpText = "The path to a .cer or .pem file for a X509 certificate.")]
        public string CertificatePath { get; set; }

        [Option(
            'c',
            "ProvisioningConnectionString",
            Required = false,
            HelpText = "The connection string of device provisioning service. Not required when the PROVISIONING_CONNECTION_STRING environment variable is set.")]
        public string ProvisioningConnectionString { get; set; } = Environment.GetEnvironmentVariable("PROVISIONING_CONNECTION_STRING");

        public static void ValidateProvisioningConnectionString(string provisioningConnectionString)
        {
            if (string.IsNullOrWhiteSpace(provisioningConnectionString))
            {
                Console.WriteLine("A provisioning connection string needs to be specified, " +
                    "please set the environment variable \"PROVISIONING_CONNECTION_STRING\" " +
                    "or pass in \"-c | --ProvisioningConnectionString\" through command line.");
                Environment.Exit(1);
            }
        }

        [Option(
            'd',
            "DeviceId",
            Required = false,
            HelpText = "The Id of device.")]
        public string DeviceId { get; set; } = $"my-device-{Guid.NewGuid()}";

        [Option(
            'r',
            "RegistrationId",
            Required = false,
            HelpText = "The Id of registration.")]
        public string RegistrationId { get; set; } = $"my-registration-{Guid.NewGuid()}";
    }
}
