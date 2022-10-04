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
            HelpText = "The path to X509 certificate.")]
        public string CertificatePath { get; set; }

        [Option(
            'c',
            "ProvisioningConnectionString",
            Required = false,
            HelpText = "The connection string of device provisioning service. Not required when the PROVISIONING_CONNECTION_STRING environment variable is set.")]
        public string ProvisioningConnectionString { get; set; } = Environment.GetEnvironmentVariable("PROVISIONING_CONNECTION_STRING");
    }
}
