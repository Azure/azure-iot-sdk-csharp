using CommandLine;
using System;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    /// <summary>
    /// Parameters for the application
    /// </summary>
    internal class Parameters
    {
        [Option(
            'c',
            "CertificatePath",
            Required = true,
            HelpText = "The path to X509 certificate.")]
        public string CertificatePath { get; set; }

        // The ProvisioningConnectionString argument is not required when either:
        // - set the PROVISIONING_CONNECTION_STRING environment variable
        // - create a launchSettings.json (see launchSettings.json.template) containing the variable
        [Option(
            'p',
            "ProvisioningConnectionString",
            Required = false,
            HelpText = "The primary connection string of device provisioning service. Not required when the PROVISIONING_CONNECTION_STRING environment variable is set.")]
        public string ProvisioningConnectionString { get; set; } = Environment.GetEnvironmentVariable("PROVISIONING_CONNECTION_STRING");
    }
}
