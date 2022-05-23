// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    internal static class X509Certificate2Generator
    {
        internal static string GenerateClientCertKeyPairAndCsr(string registrationId, DirectoryInfo certificateFolder, MsTestLogger logger)
        {
            // Generate keypair
            logger.Trace($"Generating ECC P-256 {registrationId}.key file using ...\n");
            string keygen = $"ecparam" +
                $" -genkey" +
                $" -name prime256v1" +
                $" -out {certificateFolder}\\{registrationId}.key";

            logger.Trace($"openssl {keygen}\n");
            using var keygenCmdProcess = Process.Start("openssl", keygen);
            keygenCmdProcess.WaitForExit();

            logger.Trace("Certificates generated:");
            ListAllFiles(certificateFolder.FullName, logger);

            // Generate csr
            logger.Trace($"Generating {registrationId}.csr file using ...\n");
            string csrgen = $"req" +
                $" -new" +
                $" -key {certificateFolder}\\{registrationId}.key" +
                $" -out {certificateFolder}\\{registrationId}.csr" +
                $" -subj /CN={registrationId}";

            logger.Trace($"openssl {csrgen}\n");
            using var csrgenCmdProcess = Process.Start("openssl", csrgen);
            csrgenCmdProcess.WaitForExit();

            logger.Trace("Certificates generated:");
            ListAllFiles(certificateFolder.FullName, logger);

            return File.ReadAllText($"{certificateFolder}\\{registrationId}.csr");
        }

        internal static X509Certificate2 GenerateOperationalCertificateFromIssuedCertificate(string registrationId, string issuedCertificate, DirectoryInfo certificateFolder, MsTestLogger logger)
        {
            // Write the issued certificate to disk
            File.WriteAllText($"{certificateFolder}\\{registrationId}.cer", issuedCertificate);

            logger.Trace($"Generating {registrationId}.pfx file using ...\n");
            string pfxgen = $"pkcs12" +
                $" -export" +
                $" -out {certificateFolder}\\{registrationId}.pfx" +
                $" -inkey {certificateFolder}\\{registrationId}.key" +
                $" -in {certificateFolder}\\{registrationId}.cer" +
                $" -passout pass:";

            logger.Trace($"openssl {pfxgen}\n");
            using var pfxgenCmdProcess = Process.Start("openssl", pfxgen);
            pfxgenCmdProcess.WaitForExit();

            logger.Trace("Certificates generated:");
            ListAllFiles(certificateFolder.FullName, logger);

            return new X509Certificate2($"{certificateFolder}\\{registrationId}.pfx");
        }

        internal static void GenerateSelfSignedCertificates(string registrationId, DirectoryInfo certificateFolder, MsTestLogger logger)
        {
            // Generate the private key for the self-signed certificate
            logger.Trace($"Generating the private key for the self-signed certificate with subject {registrationId} using ...\n");
            string keygen = $"genpkey" +
                $" -out {certificateFolder}\\{registrationId}.key" +
                $" -algorithm RSA" +
                $" -pkeyopt rsa_keygen_bits:2048";

            logger.Trace($"openssl {keygen}\n");
            using var keygenCmdProcess = Process.Start("openssl", keygen);
            keygenCmdProcess.WaitForExit();

            // Generate the certificate signing request for the self-signed certificate
            logger.Trace($"Generating the certificate signing request for the self-signed certificate with subject {registrationId} using ...\n");
            string csrgen = $"req" +
                $" -new" +
                $" -subj /CN={registrationId}" +
                $" -key {certificateFolder}\\{registrationId}.key" +
                $" -out {certificateFolder}\\{registrationId}.csr";

            logger.Trace($"openssl {csrgen}\n");
            using var csrgenCmdProcess = Process.Start("openssl", csrgen);
            csrgenCmdProcess.WaitForExit();

            // Self-sign the certificate signing request
            logger.Trace($"Self-sign the certificate with subject {registrationId} using ...\n");
            string signgen = $"x509" +
                $" -req" +
                $" -days 7" +
                $" -in {certificateFolder}\\{registrationId}.csr" +
                $" -signkey {certificateFolder}\\{registrationId}.key" +
                $" -out {certificateFolder}\\{registrationId}.crt";

            logger.Trace($"openssl {signgen}\n");
            using var signgenCmdProcess = Process.Start("openssl", signgen);
            signgenCmdProcess.WaitForExit();

            // Generate the pfx file containing both public and private certificate information
            logger.Trace($"Generating {registrationId}.pfx file using ...\n");
            string pfxgen = $"pkcs12" +
                $" -export" +
                $" -in {certificateFolder}\\{registrationId}.crt" +
                $" -inkey {certificateFolder}\\{registrationId}.key" +
                $" -out {certificateFolder}\\{registrationId}.pfx" +
                $" -passout pass:{TestConfiguration.Provisioning.CertificatePassword}";

            logger.Trace($"openssl {pfxgen}\n");
            using var pfxgenCmdProcess = Process.Start("openssl", pfxgen);
            pfxgenCmdProcess.WaitForExit();
        }

        private static void ListAllFiles(string path, MsTestLogger logger)
        {
            logger.Trace($"Listing files in: {path}");
            foreach (string fileName in Directory.GetFiles(path))
            {
                logger.Trace(fileName);
            }
        }
    }
}
