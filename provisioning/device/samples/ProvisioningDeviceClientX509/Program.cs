// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ProvisioningDeviceClientX509
{
    class Program
    {
        // In your Device Provisioning Service please go to "Manage enrollments" and select "Individual Enrollments".
        // Select "Add" then fill in the following:
        // Mechanism: X.509
        // Certificate: 
        //    You can generate a self-signed certificate by running the GenerateTestCertificate.ps1 powershell script.
        //    Select the public key 'certificate.cer' file. ('certificate.pfx' contains the private key and is password protected.)
        //    For production code, it is advised that you install the certificate in the CurrentUser (My) store.
        // DeviceID: iothubx509device1 (must match the CN part of the certificate Subject)

        private static string s_idScope;
        private static string s_certificateFileName = "certificate.pfx";

        public static async Task RunSample(X509Certificate2 certificate)
        {
            using (var security = new SecurityClientX509(certificate))
            using (var transport = new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly))
            {
                ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(s_idScope, security, transport);

                Console.Write("ProvisioningClient RegisterAsync . . . ");
                DeviceRegistrationResult result = await provClient.RegisterAsync();

                Console.WriteLine($"{result.Status}");
                Console.WriteLine($"ProvisioningClient AssignedHub: {result.AssignedHub}; DeviceID: {result.DeviceId}");

                if (result.Status != ProvisioningRegistrationStatusType.Assigned) return;

                IAuthenticationMethod auth = new DeviceAuthenticationWithX509Certificate(result.DeviceId, certificate);
                using (DeviceClient iotClient = DeviceClient.Create(result.AssignedHub, auth))
                {
                    Console.WriteLine("DeviceClient OpenAsync.");
                    await iotClient.OpenAsync();
                    Console.WriteLine("DeviceClient SendEventAsync.");
                    await iotClient.SendEventAsync(new Message(Encoding.UTF8.GetBytes("TestMessage")));
                    Console.WriteLine("DeviceClient CloseAsync.");
                    await iotClient.CloseAsync();
                }
            }
        }

        public static void Main(string[] args)
        {
            if (string.IsNullOrWhiteSpace(s_idScope) && (args.Length < 1))
            {
                Console.WriteLine("ProvisioningDeviceClientTpm <IDScope>");
                return;
            }

            s_idScope = args[0];

            string certificatePassword = ReadCertificatePassword();
            using (var certificate = new X509Certificate2(s_certificateFileName, certificatePassword))
            {
                RunSample(certificate).GetAwaiter().GetResult();
            }
        }

        private static string ReadCertificatePassword()
        {
            var password = new StringBuilder();
            Console.WriteLine($"Enter the PFX password for {s_certificateFileName}:");

            while(true)
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

            return password.ToString();
        }
    }
}
