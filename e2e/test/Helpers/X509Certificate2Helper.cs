// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    /// <summary>
    /// An X509Certificate2 helper class for generating self-signed and CA-signed certificates.
    /// This class uses openssl for certificate generation since <see cref="X509Certificate2"/> class currently doesn't have certificate generation APIs.
    /// </summary>
    internal static class X509Certificate2Helper
    {
        internal static void GenerateSelfSignedCertificateFiles(string subject, DirectoryInfo destinationCertificateFolder)
        {
            GenerateSignedCertificateFiles(subject, null, destinationCertificateFolder);
        }

        internal static void GenerateIntermediateCertificateSignedCertificateFiles(
            string leafCertificateSubject,
            string intermediateCertificateSubject,
            DirectoryInfo destinationCertificateFolder)
        {
            GenerateSignedCertificateFiles(leafCertificateSubject, intermediateCertificateSubject, destinationCertificateFolder);
        }

        private static void GenerateSignedCertificateFiles(
            string leafCertificateSubject,
            string signingIntermediateCertificateSubject,
            DirectoryInfo destinationCertificateFolder)
        {
            string signingCertificateKeyFile = Path.Combine(destinationCertificateFolder.FullName, $"{signingIntermediateCertificateSubject}.key");
            string signingCertificateCerFile = Path.Combine(destinationCertificateFolder.FullName, $"{signingIntermediateCertificateSubject}.cer");
            string leafCertificateKeyFile = Path.Combine(destinationCertificateFolder.FullName, $"{leafCertificateSubject}.key");
            string leafCertificateCsrFile = Path.Combine(destinationCertificateFolder.FullName, $"{leafCertificateSubject}.csr");
            string leafCertificateCerFile = Path.Combine(destinationCertificateFolder.FullName, $"{leafCertificateSubject}.cer");
            string leafCertificatePfxFile = Path.Combine(destinationCertificateFolder.FullName, $"{leafCertificateSubject}.pfx");

            // Generate the private key for the certificate
            Console.WriteLine($"Generating the private key for the certificate with subject {leafCertificateSubject} using ...\n");
            string keyGen = $"genpkey" +
                $" -out \"{leafCertificateKeyFile}\"" +
                $" -algorithm RSA" +
                $" -pkeyopt rsa_keygen_bits:2048";

            Console.WriteLine($"openssl {keyGen}\n");
            using Process keyGenCmdProcess = CreateErrorObservantProcess("openssl", keyGen);
            keyGenCmdProcess.Start();
            keyGenCmdProcess.WaitForExit();
            keyGenCmdProcess.ExitCode.Should().Be(0, $"\"{keyGen}\" exited with error {keyGenCmdProcess.StandardError.ReadToEnd()}.");

            // Generate the certificate signing request for the certificate
            Console.WriteLine($"Generating the certificate signing request for the certificate with subject {leafCertificateSubject} using ...\n");
            string csrGen = $"req" +
                $" -new" +
                $" -subj /CN={leafCertificateSubject}" +
                $" -key \"{leafCertificateKeyFile}\"" +
                $" -out \"{leafCertificateCsrFile}\"";

            Console.WriteLine($"openssl {csrGen}\n");
            using Process csrGenCmdProcess = CreateErrorObservantProcess("openssl", csrGen);
            csrGenCmdProcess.Start();
            csrGenCmdProcess.WaitForExit();
            csrGenCmdProcess.ExitCode.Should().Be(0, $"\"{csrGen}\" exited with error {csrGenCmdProcess.StandardError.ReadToEnd()}.");

            string signGen;

            // This is a request to generate a self-signed certificate.
            if (string.IsNullOrWhiteSpace(signingIntermediateCertificateSubject))
            {
                // Self-sign the certificate signing request generating a file containing the public certificate information
                Console.WriteLine($"Self-sign the certificate with subject {leafCertificateSubject} using ...\n");
                signGen = $"x509" +
                    $" -req" +
                    $" -days 7" +
                    $" -in \"{leafCertificateCsrFile}\"" +
                    $" -signkey \"{leafCertificateKeyFile}\"" +
                    $" -out \"{leafCertificateCerFile}\"";
            }
            // This is a request to generate a certificate signed by a verified intermediate certificate
            else
            {
                // Use the public certificate and private keys from the intermediate certificate to sign the leaf device certificate.
                Console.WriteLine($"Sign the certificate with subject {leafCertificateSubject} using the keys from intermediate certificate with subject {signingIntermediateCertificateSubject} ...\n");
                signGen = $"x509" +
                    $" -req" +
                    $" -days 7" +
                    $" -in \"{leafCertificateCsrFile}\"" +
                    $" -CA \"{signingCertificateCerFile}\"" +
                    $" -CAkey \"{signingCertificateKeyFile}\"" +
                    $" -CAcreateserial" +
                    $" -out \"{leafCertificateCerFile}\"";
            }

            Console.WriteLine($"openssl {signGen}\n");
            using Process signGenCmdProcess = CreateErrorObservantProcess("openssl", signGen);
            signGenCmdProcess.Start();
            signGenCmdProcess.WaitForExit();
            signGenCmdProcess.ExitCode.Should().Be(0, $"\"{signGen}\" exited with error {signGenCmdProcess.StandardError.ReadToEnd()}.");

            // Generate the pfx file containing both public certificate and private key information
            Console.WriteLine($"Generating {leafCertificateSubject}.pfx file using ...\n");
            string pfxGen = $"pkcs12" +
                $" -export" +
                $" -in \"{leafCertificateCerFile}\"" +
                $" -inkey \"{leafCertificateKeyFile}\"" +
                $" -out \"{leafCertificatePfxFile}\"" +
                $" -passout pass:";

            Console.WriteLine($"openssl {pfxGen}\n");
            using Process pfxGenCmdProcess = CreateErrorObservantProcess("openssl", pfxGen);
            pfxGenCmdProcess.Start();
            pfxGenCmdProcess.WaitForExit();
            pfxGenCmdProcess.ExitCode.Should().Be(0, $"\"{pfxGen}\" exited with error {pfxGenCmdProcess.StandardError.ReadToEnd()}.");
        }

        internal static string ExtractPublicCertificateAndPrivateKeyFromPfxAndReturnSubject(string pfxCertificateBase64, string certificatePassword, DirectoryInfo destinationCertificateFolder)
        {
            byte[] buff = Convert.FromBase64String(pfxCertificateBase64);

#if NET451
            var pfxCertificate = new X509Certificate2(buff, certificatePassword);
#else
            using var pfxCertificate = new X509Certificate2(buff, certificatePassword);
#endif

            string pfxFile = Path.Combine(destinationCertificateFolder.FullName, $"{pfxCertificate.Subject}.pfx");
            string keyFile = Path.Combine(destinationCertificateFolder.FullName, $"{pfxCertificate.Subject}.key");
            string cerFile = Path.Combine(destinationCertificateFolder.FullName, $"{pfxCertificate.Subject}.cer");

            File.WriteAllBytes(pfxFile, buff);

            Console.WriteLine($"Extracting the private key from intermediate certificate with subject {pfxCertificate.Subject} file using ...\n");
            string extractKey = $"pkcs12" +
                $" -in \"{pfxFile}\"" +
                $" -nocerts" +
                $" -out \"{keyFile}\"" +
                $" -nodes" +
                $" -passin pass:{certificatePassword}";

            Console.WriteLine($"openssl {extractKey}\n");
            using Process extractKeyCmdProcess = CreateErrorObservantProcess("openssl", extractKey);
            extractKeyCmdProcess.Start();
            extractKeyCmdProcess.WaitForExit();
            extractKeyCmdProcess.ExitCode.Should().Be(0, $"\"{extractKey}\" exited with error {extractKeyCmdProcess.StandardError.ReadToEnd()}.");

            Console.WriteLine($"Extracting the public certificate from intermediate certificate with subject {pfxCertificate.Subject} file using ...\n");
            string extractCertificate = $"pkcs12" +
                $" -in \"{pfxFile}\"" +
                $" -nokeys" +
                $" -out \"{cerFile}\"" +
                $" -passin pass:{certificatePassword}";

            Console.WriteLine($"openssl {extractCertificate}\n");
            using Process extractCertificateCmdProcess = CreateErrorObservantProcess("openssl", extractCertificate);
            extractCertificateCmdProcess.Start();
            extractCertificateCmdProcess.WaitForExit();
            extractCertificateCmdProcess.ExitCode.Should().Be(0, $"\"{extractCertificate}\" exited with error {extractCertificateCmdProcess.StandardError.ReadToEnd()}.");

            return pfxCertificate.Subject.ToString();
        }

        internal static X509Certificate2 CreateX509Certificate2FromPfxFile(string subjectName, DirectoryInfo certificateFolder)
        {
            return new X509Certificate2(Path.Combine(certificateFolder.FullName, $"{subjectName}.pfx"));
        }

        internal static X509Certificate2 CreateX509Certificate2FromCerFile(string subjectName, DirectoryInfo certificateFolder)
        {
            return new X509Certificate2(Path.Combine(certificateFolder.FullName, $"{subjectName}.cer"));
        }

        private static Process CreateErrorObservantProcess(string processName, string arguments)
        {
            var processStartInfo = new ProcessStartInfo(processName, arguments)
            {
                RedirectStandardError = true,
                UseShellExecute = false
            };

            return new Process
            {
                StartInfo = processStartInfo
            };
        }
    }
}
