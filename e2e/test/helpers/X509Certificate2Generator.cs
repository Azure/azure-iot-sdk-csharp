// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    internal static class X509Certificate2Generator
    {
        internal static void GenerateSelfSignedCertificates(string registrationId, DirectoryInfo certificateFolder, MsTestLogger logger)
        {
            string keyFile = Path.Combine(certificateFolder.FullName, $"{registrationId}.key");
            string csrFile = Path.Combine(certificateFolder.FullName, $"{registrationId}.csr");
            string crtFile = Path.Combine(certificateFolder.FullName, $"{registrationId}.crt");
            string pfxFile = Path.Combine(certificateFolder.FullName, $"{registrationId}.pfx");

            // Generate the private key for the self-signed certificate
            logger.Trace($"Generating the private key for the self-signed certificate with subject {registrationId} using ...\n");
            string keygen = $"genpkey" +
                $" -out \"{keyFile}\"" +
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
                $" -key \"{keyFile}\"" +
                $" -out \"{csrFile}\"";

            logger.Trace($"openssl {csrgen}\n");
            using var csrgenCmdProcess = Process.Start("openssl", csrgen);
            csrgenCmdProcess.WaitForExit();

            // Self-sign the certificate signing request
            logger.Trace($"Self-sign the certificate with subject {registrationId} using ...\n");
            string signgen = $"x509" +
                $" -req" +
                $" -days 7" +
                $" -in \"{csrFile}\"" +
                $" -signkey \"{keyFile}\"" +
                $" -out \"{crtFile}\"";

            logger.Trace($"openssl {signgen}\n");
            using var signgenCmdProcess = Process.Start("openssl", signgen);
            signgenCmdProcess.WaitForExit();

            // Generate the pfx file containing both public and private certificate information
            logger.Trace($"Generating {registrationId}.pfx file using ...\n");
            string pfxgen = $"pkcs12" +
                $" -export" +
                $" -in \"{crtFile}\"" +
                $" -inkey \"{keyFile}\"" +
                $" -out \"{pfxFile}\"" +
                $" -passout pass:";

            logger.Trace($"openssl {pfxgen}\n");
            using var pfxgenCmdProcess = Process.Start("openssl", pfxgen);
            pfxgenCmdProcess.WaitForExit();
        }
    }
}
