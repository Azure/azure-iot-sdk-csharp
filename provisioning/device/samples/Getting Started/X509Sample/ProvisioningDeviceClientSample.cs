// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Authentication;
using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.Devices.Provisioning.Client.Samples
{
    /// <summary>
    /// Demonstrates how to register a device with the device provisioning service using a certificate, and then
    /// use the registration information to authenticate to IoT Hub.
    /// </summary>
    internal class ProvisioningDeviceClientSample
    {
        private readonly Parameters _parameters;

        public ProvisioningDeviceClientSample(Parameters parameters)
        {
            _parameters = parameters;
        }

        public async Task RunSampleAsync()
        {
            Console.WriteLine($"Loading the certificate...");
            using X509Certificate2 certificate = LoadProvisioningCertificate();
            var security = new AuthenticationProviderX509Certificate(certificate);

            Console.WriteLine($"Initializing the device provisioning client...");

            ProvisioningClientOptions clientOptions = _parameters.GetClientOptions();
            var provClient = new ProvisioningDeviceClient(
                _parameters.GlobalDeviceEndpoint,
                _parameters.IdScope,
                security,
                clientOptions);

            Console.WriteLine($"Initialized for registration Id {security.GetRegistrationId()}.");

            Console.WriteLine("Registering with the device provisioning service... ");
            DeviceRegistrationResult result = await provClient.RegisterAsync();

            Console.WriteLine($"Registration status: {result.Status}.");
            if (result.Status != ProvisioningRegistrationStatusType.Assigned)
            {
                Console.WriteLine($"Registration status did not assign a hub, so exiting this sample.");
                return;
            }

            Console.WriteLine($"Device {result.DeviceId} registered to {result.AssignedHub}.");

            Console.WriteLine("Creating X509 authentication for IoT Hub...");
            using var auth = new DeviceAuthenticationWithX509Certificate(
                result.DeviceId,
                certificate);

            Console.WriteLine($"Testing the provisioned device with IoT Hub...");
            IotHubClientTransportSettings transportSettings = _parameters.GetHubTransportSettings();
            var hubOptions = new IotHubClientOptions(transportSettings);
            using var iotClient = new IotHubDeviceClient(result.AssignedHub, auth, hubOptions);

            Console.WriteLine("Sending a telemetry message...");
            var message = new Devices.Client.Message(Encoding.UTF8.GetBytes("TestMessage"));
            await iotClient.SendEventAsync(message);

            await iotClient.CloseAsync();
            Console.WriteLine("Finished.");
        }

        private X509Certificate2 LoadProvisioningCertificate()
        {
            ReadCertificatePassword();

            var certificateCollection = new X509Certificate2Collection();
            certificateCollection.Import(
                _parameters.GetCertificatePath(),
                _parameters.CertificatePassword,
                X509KeyStorageFlags.UserKeySet);

            X509Certificate2 certificate = null;

            foreach (X509Certificate2 element in certificateCollection)
            {
                Console.WriteLine($"Found certificate: {element?.Thumbprint} {element?.Subject}; PrivateKey: {element?.HasPrivateKey}");
                if (certificate == null && element.HasPrivateKey)
                {
                    certificate = element;
                }
                else
                {
                    element.Dispose();
                }
            }

            if (certificate == null)
            {
                throw new FileNotFoundException($"{_parameters.CertificateName} did not contain any certificate with a private key.");
            }

            Console.WriteLine($"Using certificate {certificate.Thumbprint} {certificate.Subject}");

            return certificate;
        }

        private void ReadCertificatePassword()
        {
            if (!string.IsNullOrWhiteSpace(_parameters.CertificatePassword))
            {
                return;
            }

            var password = new StringBuilder();
            Console.WriteLine($"Enter the PFX password for {_parameters.CertificateName}:");

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                    {
                        password.Remove(password.Length - 1, 1);
                        Console.Write("\b \b");
                    }
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else
                {
                    Console.Write('*');
                    password.Append(key.KeyChar);
                }
            }

            _parameters.CertificatePassword = password.ToString();
        }
    }
}
