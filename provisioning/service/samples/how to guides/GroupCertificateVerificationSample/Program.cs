// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    public class Program
    {
        private const string VerificationCertificatePath = "verificationCertificate.cer";

        public static int  Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine(
                    "\tGroupCertificateVerificationSample <certificate.pfx> <certificatepassword> <verificationCode>");
                return -1;
            }

            string certificatePath = args[0];
            string certificatePassword = args[1];
            string verificationCode = args[2];

            var certificate = new X509Certificate2(
                certificatePath, 
                certificatePassword, 
                X509KeyStorageFlags.Exportable);

            if (!certificate.HasPrivateKey)
            {
                Console.WriteLine("ERROR: The certificate does not have a private key.");
                return 1;
            }

            X509Certificate2 verificationCertificate = 
                VerificationCertificateGenerator.GenerateSignedCertificate(certificate, verificationCode);

            File.WriteAllText(
                VerificationCertificatePath, 
                Convert.ToBase64String(verificationCertificate.Export(X509ContentType.Cert)));

            Console.WriteLine(
                $"Verification certificate ({verificationCertificate.Subject}; {verificationCertificate.Thumbprint})" +
                $" was written to {VerificationCertificatePath}.");

            return 0;
        }
    }
}
