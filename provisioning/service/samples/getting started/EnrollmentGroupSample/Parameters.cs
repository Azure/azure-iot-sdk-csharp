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
            "ProvisioningConnectionString",
            Required = false,
            HelpText = "The connection string of device provisioning service. Not required when the PROVISIONING_CONNECTION_STRING environment variable is set.")]
        public string ProvisioningConnectionString { get; set; } = Environment.GetEnvironmentVariable("PROVISIONING_CONNECTION_STRING");

        [Option(
            'v',
            "ApiVersion",
            Required = false,
            HelpText = "The service API version to use: '2019-03-31' (default), '2026-11-01' (explicit opt-in), or '2026-11-02-preview' (explicit opt-in; enables preview-only fields). When omitted, the default 2019-03-31 version is used and no 2026 fields are sent.")]
        public string ApiVersion { get; set; }

        [Option(
            "NamespaceName",
            Required = false,
            HelpText = "Optional. The namespace name (3-64 chars) to associate with the enrollment group. Available starting with service API version 2026-11-01; only sent when a 2026 API version is selected.")]
        public string NamespaceName { get; set; }

        [Option(
            "CertificateAuthorityName",
            Required = false,
            HelpText = "Optional. The certificate authority name (3-63 chars) to associate with the enrollment group. Available starting with service API version 2026-11-01; only sent when a 2026 API version is selected.")]
        public string CertificateAuthorityName { get; set; }

        [Option(
            "CertificatePolicyName",
            Required = false,
            HelpText = "Optional. The certificate policy name (3-63 chars) to associate with the enrollment group. Available starting with service API version 2026-11-01; only sent when a 2026 API version is selected.")]
        public string CertificatePolicyName { get; set; }

        [Option(
            "DeviceTypeRef",
            Required = false,
            HelpText = "Optional. A single device type reference. Preview only (2026-11-02-preview); only sent when the preview API version is selected.")]
        public string DeviceTypeRef { get; set; }

        /// <summary>
        /// Maps the <see cref="ApiVersion"/> command-line value to a <see cref="ServiceVersion"/>.
        /// Defaults to <see cref="ServiceVersion.V2019_03_31"/> when not specified.
        /// </summary>
        public ServiceVersion GetServiceVersion()
        {
            return ApiVersion switch
            {
                null or "" or "2019-03-31" => ServiceVersion.V2019_03_31,
                "2026-11-01" => ServiceVersion.V2026_11_01,
                "2026-11-02-preview" => ServiceVersion.V2026_11_02_Preview,
                _ => throw new ArgumentException(
                    $"Unknown API version '{ApiVersion}'. Expected '2019-03-31', '2026-11-01', or '2026-11-02-preview'."),
            };
        }
    }
}
