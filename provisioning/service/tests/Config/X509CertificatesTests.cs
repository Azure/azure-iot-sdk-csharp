// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class X509CertificatesTests
    {
        private const string SUBJECT_NAME = "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US";
        private const string SHA1THUMBPRINT = "0000000000000000000000000000000000";
        private const string SHA256THUMBPRINT = "validEnrollmentGroupId";
        private const string ISSUER_NAME = "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US";
        private const string NOT_BEFORE_UTC_STRING = "2017-11-14T12:34:18.123Z";
        private const string NOT_AFTER_UTC_STRING = "2017-11-14T12:34:18.321Z";
        private const string SERIAL_NUMBER = "000000000000000000";
        private const int VERSION = 3;

        private const string PUBLIC_KEY_CERTIFICATE_STRING =
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

        private string MakeCertInfoJson(
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

        /* SRS_X509_CERTIFICATES_21_001: [The constructor shall store the provided primary and secondary certificates as X509CertificateWithInfo.] */

        [TestMethod]
        public void X509CertificatesSucceedOnValidPrimaryX509Certificate()
        {
            // arrange
            using var primary = new X509Certificate2(System.Text.Encoding.ASCII.GetBytes(PUBLIC_KEY_CERTIFICATE_STRING));

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
            using var primary = new X509Certificate2(System.Text.Encoding.ASCII.GetBytes(PUBLIC_KEY_CERTIFICATE_STRING));
            using var secondary = new X509Certificate2(System.Text.Encoding.ASCII.GetBytes(PUBLIC_KEY_CERTIFICATE_STRING));

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
            string primary = PUBLIC_KEY_CERTIFICATE_STRING;

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
            string primary = PUBLIC_KEY_CERTIFICATE_STRING;
            string secondary = PUBLIC_KEY_CERTIFICATE_STRING;

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
                MakeCertInfoJson(SUBJECT_NAME, SHA1THUMBPRINT, SHA256THUMBPRINT, ISSUER_NAME, NOT_BEFORE_UTC_STRING, NOT_AFTER_UTC_STRING, SERIAL_NUMBER, VERSION) +
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
                MakeCertInfoJson(SUBJECT_NAME, SHA1THUMBPRINT, SHA256THUMBPRINT, ISSUER_NAME, NOT_BEFORE_UTC_STRING, NOT_AFTER_UTC_STRING, SERIAL_NUMBER, VERSION) +
                "," +
                "  \"secondary\": " +
                MakeCertInfoJson(SUBJECT_NAME, SHA1THUMBPRINT, SHA256THUMBPRINT, ISSUER_NAME, NOT_BEFORE_UTC_STRING, NOT_AFTER_UTC_STRING, SERIAL_NUMBER, VERSION) +
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
#pragma warning disable CA1806 // Do not ignore method results
            TestAssert.Throws<ArgumentException>(() => new X509Certificates(primary));
#pragma warning restore CA1806 // Do not ignore method results
        }

        [TestMethod]
        public void X509CertificatesThrowsOnNullPrimaryString()
        {
            // arrange
            string primary = null;

            // act - assert
#pragma warning disable CA1806 // Do not ignore method results
            TestAssert.Throws<ArgumentException>(() => new X509Certificates(primary));
#pragma warning restore CA1806 // Do not ignore method results
        }

        [TestMethod]
        public void X509CertificatesThrowsOnJsonWithoutPrimaryCertificate()
        {
            // arrange
            string json =
                "{" +
                "  \"secondary\": " +
                MakeCertInfoJson(SUBJECT_NAME, SHA1THUMBPRINT, SHA256THUMBPRINT, ISSUER_NAME, NOT_BEFORE_UTC_STRING, NOT_AFTER_UTC_STRING, SERIAL_NUMBER, VERSION) +
                "}";

            // act - assert
            TestAssert.Throws<DeviceProvisioningServiceException>(() => JsonConvert.DeserializeObject<X509Certificates>(json));
        }
    }
}
