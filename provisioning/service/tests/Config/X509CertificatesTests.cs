// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using FluentAssertions.Specialized;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class X509CertificatesTests
    {
        private const string SubjectName = "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US";
        private const string Sha1Thumbprint = "0000000000000000000000000000000000";
        private const string Sha256Thumbprint = "validEnrollmentGroupId";
        private const string IssuerName = "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US";
        private const string NotBeforeUtcString = "2017-11-14T12:34:18.123Z";
        private const string NotAfterUtcString = "2017-11-14T12:34:18.321Z";
        private const string SerialNumber = "000000000000000000";
        private const int Version = 3;

        private const string PublicKeyCertificate =
            "-----BEGIN CERTIFICATE-----\n" +
            "MIIBiDCCAS2gAwIBAgIFWks8LR4wCgYIKoZIzj0EAwIwNjEUMBIGA1UEAwwLcmlv\n" +
            "dGNvcmVuZXcxETAPBgNVBAoMCE1TUl9URVNUMQswCQYDVQQGEwJVUzAgFw0xNzAx\n" +
            "MDEwMDAwMDBaGA8zNzAxMDEzMTIzNTk1OVowNjEUMBIGA1UEAwwLcmlvdGNvcmVu\n" +
            "ZXcxETAPBgNVBAoMCE1TUl9URVNUMQswCQYDVQQGEwJVUzBZMBMGByqGSM49AgEG\n" +
            "CCqGSM49AwEHA0IABLVS6bK+QMm+HZ0247Nm+JmnERuickBXTj6rydcP3WzVQNBN\n" +
            "pvcQ/4YVrPp60oiYRxZbsPyBtHt2UCAC00vEXy+jJjAkMA4GA1UdDwEB/wQEAwIH\n" +
            "gDASBgNVHRMBAf8ECDAGAQH/AgECMAoGCCqGSM49BAMCA0kAMEYCIQDEjs2PoZEi\n" +
            "/yAQNj2Vji9RthQ33HG/QdL12b1ABU5UXgIhAPJujG/c/S+7vcREWI7bQcCb31JI\n" +
            "BDhWZbt4eyCvXZtZ\n" +
            "-----END CERTIFICATE-----\n";

        private static string MakeCertInfoJson(
            string subjectName, string sha1Thumbprint, string sha256Thumbprint,
            string issuerName, string notBeforeUtcString, string notAfterUtcString, string serialNumber, int version)
        {
            string json =
                "{" +
                "  \"certificate\":\"\"," +
                "  \"info\": {" +
                (subjectName == null ? "" : "    \"subjectName\": \"" + subjectName + "\",") +
                (sha1Thumbprint == null ? "" : "    \"sha1Thumbprint\": \"" + sha1Thumbprint + "\",") +
                (sha256Thumbprint == null ? "" : "    \"sha256Thumbprint\": \"" + sha256Thumbprint + "\",") +
                (issuerName == null ? "" : "    \"issuerName\": \"" + issuerName + "\",") +
                (notBeforeUtcString == null ? "" : "    \"notBeforeUtc\": \"" + notBeforeUtcString + "\",") +
                (notAfterUtcString == null ? "" : "    \"notAfterUtc\": \"" + notAfterUtcString + "\",") +
                (serialNumber == null ? "" : "    \"serialNumber\": \"" + serialNumber + "\",") +
                "    \"version\": " + version +
                "  }" +
                "}";

            return json;
        }

        [TestMethod]
        public void X509CertificatesSucceedOnValidPrimaryX509Certificate()
        {
            // arrange
            using var primary = new X509Certificate2(System.Text.Encoding.ASCII.GetBytes(PublicKeyCertificate));

            // act
            var x509Certificates = new X509Certificates(primary);

            // assert
            Assert.IsNotNull(x509Certificates.Primary);
            Assert.IsNull(x509Certificates.Secondary);
        }

        [TestMethod]
        public void X509CertificatesSucceedOnValidPrimaryAndSecondaryX509Certificate()
        {
            // arrange
            using var primary = new X509Certificate2(System.Text.Encoding.ASCII.GetBytes(PublicKeyCertificate));
            using var secondary = new X509Certificate2(System.Text.Encoding.ASCII.GetBytes(PublicKeyCertificate));

            // act
            var x509Certificates = new X509Certificates(primary, secondary);

            // assert
            Assert.IsNotNull(x509Certificates.Primary);
            Assert.IsNotNull(x509Certificates.Secondary);
        }

        [TestMethod]
        public void X509CertificatesSucceedOnValidPrimaryString()
        {
            // arrange
            string primary = PublicKeyCertificate;

            // act
            var x509Certificates = new X509Certificates(primary);

            // assert
            Assert.IsNotNull(x509Certificates.Primary);
            Assert.IsNull(x509Certificates.Secondary);
        }

        [TestMethod]
        public void X509CertificatesSucceedOnValidPrimaryAndSecondaryX509CertificateWithInfo()
        {
            // arrange
            string primary = PublicKeyCertificate;
            string secondary = PublicKeyCertificate;

            // act
            var x509Certificates = new X509Certificates(primary, secondary);

            // assert
            Assert.IsNotNull(x509Certificates.Primary);
            Assert.IsNotNull(x509Certificates.Secondary);
        }

        [TestMethod]
        public void X509CertificatesSucceedOnJsonWithPrimaryCertificate()
        {
            // arrange
            string json =
                "{" +
                "  \"primary\": " +
                MakeCertInfoJson(SubjectName, Sha1Thumbprint, Sha256Thumbprint, IssuerName, NotBeforeUtcString, NotAfterUtcString, SerialNumber, Version) +
                "}";

            // act
            X509Certificates x509Certificates = JsonConvert.DeserializeObject<X509Certificates>(json);

            // assert
            Assert.IsNotNull(x509Certificates.Primary);
            Assert.IsNull(x509Certificates.Secondary);
        }

        [TestMethod]
        public void X509CertificatesSucceedOnJsonWithPrimaryAndSecondaryCertificate()
        {
            // arrange
            string json =
                "{" +
                "  \"primary\": " +
                MakeCertInfoJson(SubjectName, Sha1Thumbprint, Sha256Thumbprint, IssuerName, NotBeforeUtcString, NotAfterUtcString, SerialNumber, Version) +
                "," +
                "  \"secondary\": " +
                MakeCertInfoJson(SubjectName, Sha1Thumbprint, Sha256Thumbprint, IssuerName, NotBeforeUtcString, NotAfterUtcString, SerialNumber, Version) +
                "}";

            // act
            X509Certificates x509Certificates = JsonConvert.DeserializeObject<X509Certificates>(json);

            // assert
            Assert.IsNotNull(x509Certificates.Primary);
            Assert.IsNotNull(x509Certificates.Secondary);
        }

        /* SRS_X509_CERTIFICATES_21_002: [The constructor shall throw ArgumentException if the provided primary certificates is invalid.] */

        [TestMethod]
        public void X509CertificatesThrowsOnNullPrimaryX509Certificate()
        {
            // arrange
            X509Certificate2 primary = null;

            // act - assert
            Action act = () => _ = new X509Certificates(primary);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void X509CertificatesThrowsOnNullPrimaryString()
        {
            // arrange
            string primary = null;

            // act - assert
            Action act = () => _ = new X509Certificates(primary);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void X509CertificatesThrowsOnJsonWithoutPrimaryCertificate()
        {
            // arrange
            string json =
                "{" +
                "  \"secondary\": " +
                MakeCertInfoJson(SubjectName, Sha1Thumbprint, Sha256Thumbprint, IssuerName, NotBeforeUtcString, NotAfterUtcString, SerialNumber, Version) +
                "}";

            // act - assert
            Action act = () => JsonConvert.DeserializeObject<X509Certificates>(json);
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
