// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    internal static class X509Certificate2Helper
    {
        internal static void GenerateSelfSignedCertificateFiles(string subject, string certificatePassword, DirectoryInfo destinationCertificateFolder, MsTestLogger logger)
        {
            GenerateSignedCertificateFiles(subject, certificatePassword, null, destinationCertificateFolder, logger);
        }

        internal static void GenerateIntermediateCertificateSignedCertificateFiles(
            string leafCertificateSubject,
            string certificatePassword,
            string intermediateCertificateSubject,
            DirectoryInfo destinationCertificateFolder,
            MsTestLogger logger)
        {
            GenerateSignedCertificateFiles(leafCertificateSubject, certificatePassword, intermediateCertificateSubject, destinationCertificateFolder, logger);
        }

        private static void GenerateSignedCertificateFiles(
            string leafCertificateSubject,
            string certificatePassword,
            string intermediateCertificateSubject,
            DirectoryInfo destinationCertificateFolder,
            MsTestLogger logger)
        {
            // Generate the private key for the self-signed certificate
            logger.Trace($"Generating the private key for the self-signed certificate with subject {leafCertificateSubject} using ...\n");
            string keyGen = $"genpkey" +
                $" -out \"{destinationCertificateFolder}\\{leafCertificateSubject}.key\"" +
                $" -algorithm RSA" +
                $" -pkeyopt rsa_keygen_bits:2048";

            logger.Trace($"openssl {keyGen}\n");
            using Process keyGenCmdProcess = CreateErrorObservantProcess("openssl", keyGen);
            keyGenCmdProcess.Start();
            keyGenCmdProcess.WaitForExit();
            keyGenCmdProcess.ExitCode.Should().Be(0, $"\"{keyGen}\" exited with error {keyGenCmdProcess.StandardError.ReadToEnd()}.");

            // Generate the certificate signing request for the self-signed certificate
            logger.Trace($"Generating the certificate signing request for the self-signed certificate with subject {leafCertificateSubject} using ...\n");
            string csrGen = $"req" +
                $" -new" +
                $" -subj /CN={leafCertificateSubject}" +
                $" -key \"{destinationCertificateFolder}\\{leafCertificateSubject}.key\"" +
                $" -out \"{destinationCertificateFolder}\\{leafCertificateSubject}.csr\"";

            logger.Trace($"openssl {csrGen}\n");
            using Process csrGenCmdProcess = CreateErrorObservantProcess("openssl", csrGen);
            csrGenCmdProcess.Start();
            csrGenCmdProcess.WaitForExit();
            csrGenCmdProcess.ExitCode.Should().Be(0, $"\"{csrGen}\" exited with error {csrGenCmdProcess.StandardError.ReadToEnd()}.");

            string signGen;

            // This is a request to generate a self-signed certificate.
            if (string.IsNullOrWhiteSpace(intermediateCertificateSubject))
            {
                // Self-sign the certificate signing request generating a file containing the public certificate information
                logger.Trace($"Self-sign the certificate with subject {leafCertificateSubject} using ...\n");
                signGen = $"x509" +
                    $" -req" +
                    $" -days 7" +
                    $" -in \"{destinationCertificateFolder}\\{leafCertificateSubject}.csr\"" +
                    $" -signkey \"{destinationCertificateFolder}\\{leafCertificateSubject}.key\"" +
                    $" -out \"{destinationCertificateFolder}\\{leafCertificateSubject}.cer\"";
            }
            // This is a request to generate a certificate signed by a verified intermediate certificate
            else
            {
                // Use the public certificate and private keys from the intermediate certificate to sign the leaf device certificate.
                logger.Trace($"Sign the certificate with subject {leafCertificateSubject} using the keys from intermediate certificate with subject {intermediateCertificateSubject} ...\n");
                signGen = $"x509" +
                    $" -req" +
                    $" -days 7" +
                    $" -in \"{destinationCertificateFolder}\\{leafCertificateSubject}.csr\"" +
                    $" -CA \"{destinationCertificateFolder}\\{intermediateCertificateSubject}.cer\"" +
                    $" -CAkey \"{destinationCertificateFolder}\\{intermediateCertificateSubject}.key\"" +
                    $" -CAcreateserial" +
                    $" -out \"{destinationCertificateFolder}\\{leafCertificateSubject}.cer\"";
            }

            logger.Trace($"openssl {signGen}\n");
            using Process signGenCmdProcess = CreateErrorObservantProcess("openssl", signGen);
            signGenCmdProcess.Start();
            signGenCmdProcess.WaitForExit();
            signGenCmdProcess.ExitCode.Should().Be(0, $"\"{signGen}\" exited with error {signGenCmdProcess.StandardError.ReadToEnd()}.");

            // Generate the pfx file containing both public certificate and private key information
            logger.Trace($"Generating {leafCertificateSubject}.pfx file using ...\n");
            string pfxGen = $"pkcs12" +
                $" -export" +
                $" -in \"{destinationCertificateFolder}\\{leafCertificateSubject}.cer\"" +
                $" -inkey \"{destinationCertificateFolder}\\{leafCertificateSubject}.key\"" +
                $" -out \"{destinationCertificateFolder}\\{leafCertificateSubject}.pfx\"" +
                $" -passout pass:{certificatePassword}";

            logger.Trace($"openssl {pfxGen}\n");
            using Process pfxGenCmdProcess = CreateErrorObservantProcess("openssl", pfxGen);
            pfxGenCmdProcess.Start();
            pfxGenCmdProcess.WaitForExit();
            pfxGenCmdProcess.ExitCode.Should().Be(0, $"\"{pfxGen}\" exited with error {pfxGenCmdProcess.StandardError.ReadToEnd()}.");
        }

        internal static string ExtractPublicCertificateAndPrivateKeyFromPfx(string pfxCertificateBase64, string certificatePassword, DirectoryInfo destinationCertificateFolder)
        {
            byte[] buff = Convert.FromBase64String(pfxCertificateBase64);

#if NET451
            var pfxCertificate = new X509Certificate2(buff, certificatePassword);
#else
            using var pfxCertificate = new X509Certificate2(buff, certificatePassword);
#endif

            File.WriteAllBytes($"{destinationCertificateFolder}\\{pfxCertificate.Subject}.pfx", buff);

            Console.WriteLine($"Extracting the private key from intermediate certificate with subject {pfxCertificate.Subject} file using ...\n");
            string extractKey = $"pkcs12" +
                $" -in \"{destinationCertificateFolder}\\{pfxCertificate.Subject}.pfx\"" +
                $" -nocerts" +
                $" -out \"{destinationCertificateFolder}\\{pfxCertificate.Subject}.key\"" +
                $" -nodes" +
                $" -passin pass:{certificatePassword}";

            Console.WriteLine($"openssl {extractKey}\n");
            using Process extractKeyCmdProcess = CreateErrorObservantProcess("openssl", extractKey);
            extractKeyCmdProcess.Start();
            extractKeyCmdProcess.WaitForExit();
            extractKeyCmdProcess.ExitCode.Should().Be(0, $"\"{extractKey}\" exited with error {extractKeyCmdProcess.StandardError.ReadToEnd()}.");

            Console.WriteLine($"Extracting the public certificate from intermediate certificate with subject {pfxCertificate.Subject} file using ...\n");
            string extractCertificate = $"pkcs12" +
                $" -in \"{destinationCertificateFolder}\\{pfxCertificate.Subject}.pfx\"" +
                $" -nokeys" +
                $" -out \"{destinationCertificateFolder}\\{pfxCertificate.Subject}.cer\"" +
                $" -passin pass:{certificatePassword}";

            Console.WriteLine($"openssl {extractCertificate}\n");
            using Process extractCertificateCmdProcess = CreateErrorObservantProcess("openssl", extractCertificate);
            extractCertificateCmdProcess.Start();
            extractCertificateCmdProcess.WaitForExit();
            extractCertificateCmdProcess.ExitCode.Should().Be(0, $"\"{extractCertificate}\" exited with error {extractCertificateCmdProcess.StandardError.ReadToEnd()}.");

            return pfxCertificate.Subject.ToString();
        }

        internal static X509Certificate2 CreateX509Certificate2FromPfxFile(string subjectName, string certificatePassword, DirectoryInfo certificateFolder)
        {
            return new X509Certificate2($"{certificateFolder.FullName}\\{subjectName}.pfx", certificatePassword);
        }

        internal static X509Certificate2 CreateX509Certificate2FromCerFile(string subjectName, DirectoryInfo certificateFolder)
        {
            return new X509Certificate2($"{certificateFolder.FullName}\\{subjectName}.cer");
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