// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;

namespace Microsoft.Azure.Devices.E2ETests
{
    public static partial class Configuration
    {
        public static partial class CommonCertificates
        {
            public static X509Certificate2 GetRootCaCertificate()
            {
                const string hubCert = "X509_CHAIN_ROOT_CA_CERTIFICATE";
                X509Certificate2 cert = GetBase64EncodedCertificate(hubCert);
                cert.NotAfter.Should().NotBeBefore(DateTime.UtcNow, $"The X509 cert from {hubCert} has expired.");
                return cert;
            }

            public static X509Certificate2 GetIntermediate1Certificate()
            {
                const string hubCert = "X509_CHAIN_INTERMEDIATE1_CERTIFICATE";
                X509Certificate2 cert = GetBase64EncodedCertificate(hubCert);
                cert.NotAfter.Should().NotBeBefore(DateTime.UtcNow, $"The X509 cert from {hubCert} has expired.");
                return cert;
            }

            public static X509Certificate2 GetIntermediate2Certificate()
            {
                const string hubCert = "X509_CHAIN_INTERMEDIATE2_CERTIFICATE";
                X509Certificate2 cert = GetBase64EncodedCertificate(hubCert);
                cert.NotAfter.Should().NotBeBefore(DateTime.UtcNow, $"The X509 cert from {hubCert} has expired.");
                return cert;
            }
        }
    }
}
