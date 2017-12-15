// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    public class X509CertificateWithInfoTests
    {
        private const string SUBJECT_NAME = "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US";
        private const string SHA1THUMBPRINT = "0000000000000000000000000000000000";
        private const string SHA256THUMBPRINT = "validEnrollmentGroupId";
        private const string ISSUER_NAME = "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US";
        private const string NOT_BEFORE_UTC_STRING = "2017-11-14T12:34:18.123Z";
        private DateTime NOT_BEFORE_UTC = new DateTime(2017, 11, 14, 12, 34, 18, 123, DateTimeKind.Utc);
        private const string NOT_AFTER_UTC_STRING = "2017-11-14T12:34:18.321Z";
        private DateTime NOT_AFTER_UTC = new DateTime(2017, 11, 14, 12, 34, 18, 321, DateTimeKind.Utc);
        private const string SERIAL_NUMBER = "000000000000000000";
        private const int VERSION = 3;
        private const string PUBLIC_KEY_CERTIFICATE_STRING =
            @"MIIBiDCCAS2gAwIBAgIFWks8LR4wCgYIKoZIzj0EAwIwNjEUMBIGA1UEAwwLcmlv" +
            "dGNvcmVuZXcxETAPBgNVBAoMCE1TUl9URVNUMQswCQYDVQQGEwJVUzAgFw0xNzAx" +
            "MDEwMDAwMDBaGA8zNzAxMDEzMTIzNTk1OVowNjEUMBIGA1UEAwwLcmlvdGNvcmVu" +
            "ZXcxETAPBgNVBAoMCE1TUl9URVNUMQswCQYDVQQGEwJVUzBZMBMGByqGSM49AgEG" +
            "CCqGSM49AwEHA0IABLVS6bK+QMm+HZ0247Nm+JmnERuickBXTj6rydcP3WzVQNBN" +
            "pvcQ/4YVrPp60oiYRxZbsPyBtHt2UCAC00vEXy+jJjAkMA4GA1UdDwEB/wQEAwIH" +
            "gDASBgNVHRMBAf8ECDAGAQH/AgECMAoGCCqGSM49BAMCA0kAMEYCIQDEjs2PoZEi" +
            "/yAQNj2Vji9RthQ33HG/QdL12b1ABU5UXgIhAPJujG/c/S+7vcREWI7bQcCb31JI" +
            "BDhWZbt4eyCvXZtZ";

        private string makeJson(
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


        /* SRS_X509_CERTIFICATE_WITH_INFO_21_001: [The public constructor shall throws ArgumentException if the provided certificate is null or InvalidOperationException if it is invalid.] */
        [TestMethod]
        [TestCategory("DevService")]
        public void X509CertificateWithInfo_Constructor_ThrowsOnNullX509Certificate()
        {
            // arrange
            X509Certificate2 certificateNull = null;
            X509Certificate2 certificateEmpty = new X509Certificate2();
            string certificateString = null;
            string certificateStringEmpty = "";
            string certificateStringInvalid =
                @"-----BEGIN CERTIFICATE-----" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" +
                "xxxxxxxxxxxxxxxx" +
                "-----END CERTIFICATE-----";

            // act - assert
            TestAssert.Throws<ArgumentException>(() => new X509CertificateWithInfo(certificateNull));
            TestAssert.Throws<CryptographicException>(() => new X509CertificateWithInfo(certificateEmpty));
            TestAssert.Throws<ArgumentException>(() => new X509CertificateWithInfo(certificateString));
            TestAssert.Throws<CryptographicException>(() => new X509CertificateWithInfo(certificateStringEmpty));
            TestAssert.Throws<CryptographicException>(() => new X509CertificateWithInfo(certificateStringInvalid));
        }

        /* SRS_X509_CERTIFICATE_WITH_INFO_21_002: [The public constructor shall store the provided certificate as Base64 string.] */
        /* SRS_X509_CERTIFICATE_WITH_INFO_21_003: [The public constructor shall set the Info to null.] */
        [TestMethod]
        [TestCategory("DevService")]
        public void X509CertificateWithInfo_Constructor_SucceedOnValidX509Certificate()
        {
            // arrange
            X509Certificate2 certificate = new X509Certificate2(System.Text.Encoding.ASCII.GetBytes(PUBLIC_KEY_CERTIFICATE_STRING));

            // act
            X509CertificateWithInfo x509CertificateWithInfo = new X509CertificateWithInfo(certificate);

            // assert
            Assert.AreEqual(PUBLIC_KEY_CERTIFICATE_STRING, x509CertificateWithInfo.Certificate);
            Assert.IsNull(x509CertificateWithInfo.Info);
        }

        [TestMethod]
        [TestCategory("DevService")]
        public void X509CertificateWithInfo_Constructor_SucceedOnValidX509CertificateString()
        {
            // arrange
            string certificate = PUBLIC_KEY_CERTIFICATE_STRING;

            // act
            X509CertificateWithInfo x509CertificateWithInfo = new X509CertificateWithInfo(certificate);

            // assert
            Assert.AreEqual(PUBLIC_KEY_CERTIFICATE_STRING, x509CertificateWithInfo.Certificate);
            Assert.IsNull(x509CertificateWithInfo.Info);
        }

        /* SRS_X509_CERTIFICATE_WITH_INFO_21_005: [The constructor for JSON shall store the provided Info.] */
        /* SRS_X509_CERTIFICATE_WITH_INFO_21_006: [The constructor for JSON shall store the provided certificate as X509Certificate2.] */
        [TestMethod]
        [TestCategory("DevService")]
        public void X509CertificateWithInfo_SucceedOnJsonWithInfo()
        {
            // arrange
            string json = makeJson(SUBJECT_NAME, SHA1THUMBPRINT, SHA256THUMBPRINT, ISSUER_NAME, NOT_BEFORE_UTC_STRING, NOT_AFTER_UTC_STRING, SERIAL_NUMBER, VERSION);

            // act
            X509CertificateWithInfo x509CertificateWithInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<X509CertificateWithInfo>(json);

            // assert
            Assert.IsNotNull(x509CertificateWithInfo.Info);
            Assert.AreEqual(SUBJECT_NAME, x509CertificateWithInfo.Info.SubjectName);
            Assert.AreEqual(SHA1THUMBPRINT, x509CertificateWithInfo.Info.SHA1Thumbprint);
            Assert.AreEqual(SHA256THUMBPRINT, x509CertificateWithInfo.Info.SHA256Thumbprint);
            Assert.AreEqual(ISSUER_NAME, x509CertificateWithInfo.Info.IssuerName);
            Assert.AreEqual(NOT_BEFORE_UTC, x509CertificateWithInfo.Info.NotBeforeUtc);
            Assert.AreEqual(NOT_AFTER_UTC, x509CertificateWithInfo.Info.NotAfterUtc);
            Assert.AreEqual(SERIAL_NUMBER, x509CertificateWithInfo.Info.SerialNumber);
            Assert.AreEqual(VERSION, x509CertificateWithInfo.Info.Version);
        }
    }
}
