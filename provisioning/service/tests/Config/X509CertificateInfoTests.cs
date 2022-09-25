// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class X509CertificateInfoTests
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

        private string[] FAIL_STRINGS = { null, String.Empty, "InvalidUTF8:\uD802" };
        private string[] FAIL_DATETIME = { null, String.Empty };

        private string makeJson(
            string subjectName, string sha1Thumbprint, string sha256Thumbprint,
            string issuerName, string notBeforeUtcString, string notAfterUtcString, string serialNumber, int version)
        {
            string json =
                "{" +
                (subjectName == null ? "" : "    \"subjectName\": \"" + subjectName + "\",") +
                (sha1Thumbprint == null ? "" : "    \"sha1Thumbprint\": \"" + sha1Thumbprint + "\",") +
                (sha256Thumbprint == null ? "" : "    \"sha256Thumbprint\": \"" + sha256Thumbprint + "\",") +
                (issuerName == null ? "" : "    \"issuerName\": \"" + issuerName + "\",") +
                (notBeforeUtcString == null ? "" : "    \"notBeforeUtc\": \"" + notBeforeUtcString + "\",") +
                (notAfterUtcString == null ? "" : "    \"notAfterUtc\": \"" + notAfterUtcString + "\",") +
                (serialNumber == null ? "" : "    \"serialNumber\": \"" + serialNumber + "\",") +
                "    \"version\": " + version +
                "}";

            return json;
        }

        [TestMethod]
        public void X509CertificateInfoThrowsOnInvalidNotBeforeUtc()
        {
            foreach (string failDateTime in FAIL_DATETIME)
            {
                // arrange
                string json = makeJson(SUBJECT_NAME, SHA1THUMBPRINT, SHA256THUMBPRINT, ISSUER_NAME, failDateTime, NOT_AFTER_UTC_STRING, SERIAL_NUMBER, VERSION);

                // act - assert
                TestAssert.Throws<DeviceProvisioningServiceException>(() => Newtonsoft.Json.JsonConvert.DeserializeObject<X509CertificateInfo>(json));
            }
        }

        [TestMethod]
        public void X509CertificateInfoThrowsOnInvalidNotAfterUtc()
        {
            foreach (string failDateTime in FAIL_DATETIME)
            {
                // arrange
                string json = makeJson(SUBJECT_NAME, SHA1THUMBPRINT, SHA256THUMBPRINT, ISSUER_NAME, NOT_BEFORE_UTC_STRING, failDateTime, SERIAL_NUMBER, VERSION);

                // act - assert
                TestAssert.Throws<DeviceProvisioningServiceException>(() => Newtonsoft.Json.JsonConvert.DeserializeObject<X509CertificateInfo>(json));
            }
        }

        [TestMethod]
        public void X509CertificateInfoThrowsOnInvalidVersion()
        {
            // arrange
            string json =
                "{" +
                "    \"subjectName\": \"" + SUBJECT_NAME + "\"," +
                "    \"sha1Thumbprint\": \"" + SHA1THUMBPRINT + "\"," +
                "    \"sha256Thumbprint\": \"" + SHA256THUMBPRINT + "\"," +
                "    \"issuerName\": \"" + ISSUER_NAME + "\"," +
                "    \"notBeforeUtc\": \"" + NOT_BEFORE_UTC_STRING + "\"," +
                "    \"notAfterUtc\": \"" + NOT_AFTER_UTC_STRING + "\"," +
                "    \"serialNumber\": \"" + SERIAL_NUMBER + "\"" +
                "}";

            // act - assert
            TestAssert.Throws<DeviceProvisioningServiceException>(() => Newtonsoft.Json.JsonConvert.DeserializeObject<X509CertificateInfo>(json));
        }

        [TestMethod]
        public void X509CertificateInfoSucceedOnDeserialization()
        {
            // arrange
            string json = makeJson(SUBJECT_NAME, SHA1THUMBPRINT, SHA256THUMBPRINT, ISSUER_NAME, NOT_BEFORE_UTC_STRING, NOT_AFTER_UTC_STRING, SERIAL_NUMBER, VERSION);

            // act
            X509CertificateInfo x509CertificateInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<X509CertificateInfo>(json);

            // assert
            Assert.AreEqual(SUBJECT_NAME, x509CertificateInfo.SubjectName);
            Assert.AreEqual(SHA1THUMBPRINT, x509CertificateInfo.SHA1Thumbprint);
            Assert.AreEqual(SHA256THUMBPRINT, x509CertificateInfo.SHA256Thumbprint);
            Assert.AreEqual(ISSUER_NAME, x509CertificateInfo.IssuerName);
            Assert.AreEqual(NOT_BEFORE_UTC, x509CertificateInfo.NotBeforeUtc);
            Assert.AreEqual(NOT_AFTER_UTC, x509CertificateInfo.NotAfterUtc);
            Assert.AreEqual(SERIAL_NUMBER, x509CertificateInfo.SerialNumber);
            Assert.AreEqual(VERSION, x509CertificateInfo.Version);
        }
    }
}
