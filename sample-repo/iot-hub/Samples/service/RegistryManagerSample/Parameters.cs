using CommandLine;

namespace Microsoft.Azure.Devices.Samples
{
    internal class Parameters
    {
        [Option(
            'c',
            "IoTHubConnectionString",
            Required = true,
            HelpText = "The service connection string with permissions to manage devices.")]
        public string IoTHubConnectionString { get; set; }

        [Option(
            'p',
            "X509SelfSignedCertificatePrimaryThumbprint",
            Required = true,
            HelpText = "Primary X509 thumbprint of the self-signed certificate used for device authentication.")]
        public string PrimaryThumbprint { get; set; }

        [Option(
            's',
            "X509SelfSignedCertificateSecondaryThumbprint",
            Required = false,
            HelpText = "Secondary X509 thumbprint of the self-signed certificate used for device authentication.")]
        public string SecondaryThumbprint { get; set; }

        [Option(
            'd',
            "DevicePrefix",
            Required = false,
            Default = "RegistryManagerSample_",
            HelpText = "The prefix to use when creating devices.")]
        public string DevicePrefix { get; set; }
    }
}
