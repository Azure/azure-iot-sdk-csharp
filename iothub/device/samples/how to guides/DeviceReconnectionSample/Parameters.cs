using CommandLine;
using System;
using System.IO;
using System.Reflection;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// Parameters for the application.
    /// </summary>
    public class Parameters
    {
        [Option(
            's',
            "IdScope",
            Required = true,
            HelpText = "The Id Scope of the DPS instance")]
        public string IdScope { get; set; }

        [Option(
            'c',
            "CertificateName",
            Default = "certificate.pfx",
            HelpText = "The PFX certificate to load for device provisioning authentication.")]
        public string CertificateName { get; set; }

        [Option(
            'p',
            "CertificatePassword",
            HelpText = "The password of the PFX certificate file. If not specified, the program will prompt at run time.")]
        public string CertificatePassword { get; set; }

        [Option(
            'g',
            "GlobalDeviceEndpoint",
            Default = "global.azure-devices-provisioning.net",
            HelpText = "The global endpoint for devices to connect to.")]
        public string GlobalDeviceEndpoint { get; set; }

        [Option(
            't',
            "TransportType",
            Default = TransportType.Mqtt,
            Required = false,
            HelpText = "The transport to use to communicate with the device provisioning end-point and IoT hub. Possible values include Mqtt, Mqtt_WebSocket_Only, Mqtt_Tcp_Only, Amqp, Amqp_WebSocket_Only, Amqp_Tcp_Only, and Http1.")]
        public TransportType TransportType { get; set; }

        [Option(
            'r',
            "Application running time (in seconds)",
            Required = false,
            HelpText = "The running time for this console application. Leave it unassigned to run the application until it is explicitly canceled using Control+C.")]
        public long? ApplicationRunningTime { get; set; }

        [Option(
            'w',
            "MessageWaitTime",
            Default = 15,
            HelpText = "Time in second to wait between messages.")]
        public long MessageWaitTime { get; set; }

        [Option(
            'x',
            "ExponentOrderBackoff",
            Default = 15,
            HelpText = "Exponent order (x) of 2^x milliseconds backoff time for resending messages.")]
        public int ExponentOrderBackoff { get; set; }

        public string GetCertificatePath()
        {
            if (string.IsNullOrWhiteSpace(CertificateName))
            {
                throw new InvalidOperationException("The certificate name has not been set.");
            }

            string codeBase = Assembly.GetExecutingAssembly().Location;
            string workingDirectory = Path.GetDirectoryName(codeBase);

            // Ascend the directory looking for one that has a certificate with the specified name,
            // because the sample exe is likely in a build output folder ~3 levels below in
            // the project folder.
            while (!string.IsNullOrWhiteSpace(workingDirectory))
            {
                string certificatePath = Path.Combine(workingDirectory, CertificateName);
                if (File.Exists(certificatePath))
                {
                    return certificatePath;
                }

                workingDirectory = Directory.GetParent(workingDirectory)?.FullName;
            }

            // Once we get to the root, the call to parent will return null
            // so that is our failure condition.
            throw new InvalidOperationException($"Could not find the certificate file {CertificateName} in the sample execution folder or any parent folder.");
        }
    }
}
