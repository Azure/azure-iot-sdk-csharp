// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using Microsoft.Azure.Devices.Client;
using System;
using System.IO;
using System.Reflection;

namespace Microsoft.Azure.Devices.Provisioning.Client.Samples
{
    public enum Transport
    {
        Mqtt,
        Amqp,
    };

    /// <summary>
    /// Parameters for the application
    /// </summary>
    internal class Parameters
    {
        [Option(
            's',
            "IdScope",
            Required = true,
            HelpText = "The Id Scope of the DPS instance")]
        public string IdScope { get; set; }

        [Option(
            'n',
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
            "GlobalDeviceEndpoint",
            Default = "global.azure-devices-provisioning.net",
            HelpText = "The global endpoint for devices to connect to.")]
        public string GlobalDeviceEndpoint { get; set; }

        [Option(
            "Transport",
            Default = Transport.Mqtt,
            HelpText = "The transport to use for the connection.")]
        public Transport Transport { get; set; }

        [Option(
            "TransportProtocol",
            Default = ProvisioningClientTransportProtocol.Tcp,
            HelpText = "The transport to use to communicate with the device provisioning instance.")]
        public ProvisioningClientTransportProtocol TransportProtocol { get; set; }

        internal string GetCertificatePath()
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

        internal ProvisioningClientOptions GetClientOptions()
        {
            return Transport switch
            {
                Transport.Mqtt => new ProvisioningClientOptions(new ProvisioningClientMqttSettings(TransportProtocol)),
                Transport.Amqp => new ProvisioningClientOptions(new ProvisioningClientAmqpSettings(TransportProtocol)),
                _ => throw new NotSupportedException($"Unsupported transport type {Transport}/{TransportProtocol}"),
            };
        }

        internal IotHubClientTransportSettings GetHubTransportSettings()
        {
            IotHubClientTransportProtocol protocol = TransportProtocol == ProvisioningClientTransportProtocol.Tcp
                ? IotHubClientTransportProtocol.Tcp
                : IotHubClientTransportProtocol.WebSocket;

            return Transport switch
            {
                Transport.Mqtt => new IotHubClientMqttSettings(protocol),
                Transport.Amqp => new IotHubClientAmqpSettings(protocol),
                _ => throw new NotSupportedException($"Unsupported transport type {Transport}/{TransportProtocol}"),
            };
        }
    }
}
