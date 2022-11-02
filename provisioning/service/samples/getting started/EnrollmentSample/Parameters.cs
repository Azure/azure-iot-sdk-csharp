using System;
using System.Collections.Generic;
using System.Text;
using CommandLine.Text;
using CommandLine;
using Newtonsoft.Json;

namespace EnrollmentSample
{
    internal class Parameters
    {
        [Option(
            'c',
            "ProvisioningConnectionString",
            Required = false,
            HelpText = "The connection string of device provisioning service. Not required when the PROVISIONING_CONNECTION_STRING environment variable is set.")]
        public string ProvisioningConnectionString { get; set; } = Environment.GetEnvironmentVariable("PROVISIONING_CONNECTION_STRING");
    }
}
