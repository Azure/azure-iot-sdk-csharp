// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using FluentAssertions.Specialized;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class X509CertificateWithInfoTests
    {
        private const string SubjectName = "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US";
        private const string Sha1Thumbprint = "0000000000000000000000000000000000";
        private const string Sha256Thumbprint = "validEnrollmentGroupId";
        private const string IssuerName = "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US";
        private const string NotBeforeUtcString = "2017-11-14T12:34:18.123Z";
        private readonly DateTime _notBeforeUtc = new(2017, 11, 14, 12, 34, 18, 123, DateTimeKind.Utc);
        private const string NotAfterUtcString = "2017-11-14T12:34:18.321Z";
        private readonly DateTime _notAfterUtc = new(2017, 11, 14, 12, 34, 18, 321, DateTimeKind.Utc);
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

        private const string PublicKeyCertificateString =
            @"MIIBiDCCAS2gAwIBAgIFWks8LR4wCgYIKoZIzj0EAwIwNjEUMBIGA1UEAwwLcmlv" +
            @"dGNvcmVuZXcxETAPBgNVBAoMCE1TUl9URVNUMQswCQYDVQQGEwJVUzAgFw0xNzAx" +
            @"MDEwMDAwMDBaGA8zNzAxMDEzMTIzNTk1OVowNjEUMBIGA1UEAwwLcmlvdGNvcmVu" +
            @"ZXcxETAPBgNVBAoMCE1TUl9URVNUMQswCQYDVQQGEwJVUzBZMBMGByqGSM49AgEG" +
            @"CCqGSM49AwEHA0IABLVS6bK+QMm+HZ0247Nm+JmnERuickBXTj6rydcP3WzVQNBN" +
            @"pvcQ/4YVrPp60oiYRxZbsPyBtHt2UCAC00vEXy+jJjAkMA4GA1UdDwEB/wQEAwIH" +
            @"gDASBgNVHRMBAf8ECDAGAQH/AgECMAoGCCqGSM49BAMCA0kAMEYCIQDEjs2PoZEi" +
            @"/yAQNj2Vji9RthQ33HG/QdL12b1ABU5UXgIhAPJujG/c/S+7vcREWI7bQcCb31JI" +
            @"BDhWZbt4eyCvXZtZ";

        private static string MakeJson(
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
        public void X509CertificateWithInfoConstructorThrowsOnNullX509Certificate()
        {
            // arrange
            X509Certificate2 certificateNull = null;
#pragma warning disable SYSLIB0026 // Type or member is obsolete - Parameterless constructor is obsolete in NET6.0
            using var certificateEmpty = new X509Certificate2();
#pragma warning restore SYSLIB0026 // Type or member is obsolete - Parameterless constructor is obsolete in NET6.0
            string certificateString = null;
            string certificateStringEmpty = "";
            string certificateStringInvalid =
                "-----BEGIN CERTIFICATE-----\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxx\n" +
                "-----END CERTIFICATE-----\n";

            // act - assert
            Action act = () => _ = new X509CertificateWithInfo(certificateNull);
            act.Should().Throw<ArgumentException>();

            act = () => _ = new X509CertificateWithInfo(certificateString);
            act.Should().Throw<ArgumentException>();

            act = () => _ = new X509CertificateWithInfo(certificateEmpty);
            ExceptionAssertions<ProvisioningServiceException> error = act.Should().Throw<ProvisioningServiceException>();
            error.And.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            error.And.IsTransient.Should().BeFalse();

            act = () => _ = new X509CertificateWithInfo(certificateStringEmpty);
            error = act.Should().Throw<ProvisioningServiceException>();
            error.And.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            error.And.IsTransient.Should().BeFalse();

            act = () => _ = new X509CertificateWithInfo(certificateStringInvalid);
            error = act.Should().Throw<ProvisioningServiceException>();
            error.And.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            error.And.IsTransient.Should().BeFalse();
        }

        [TestMethod]
        public void X509CertificateWithInfoConstructorSucceedOnValidX509Certificate()
        {
            // arrange
            using var certificate = new X509Certificate2(System.Text.Encoding.ASCII.GetBytes(PublicKeyCertificate));

            // act
            var x509CertificateWithInfo = new X509CertificateWithInfo(certificate);

            // assert
            Assert.AreEqual(PublicKeyCertificateString, x509CertificateWithInfo.Certificate);
            Assert.IsNull(x509CertificateWithInfo.Info);
        }

        [TestMethod]
        public void X509CertificateWithInfoConstructorSucceedOnValidX509CertificateString()
        {
            // arrange
            string certificate = PublicKeyCertificate;

            // act
            var x509CertificateWithInfo = new X509CertificateWithInfo(certificate);

            // assert
            Assert.AreEqual(PublicKeyCertificate, x509CertificateWithInfo.Certificate);
            Assert.IsNull(x509CertificateWithInfo.Info);
        }


        [TestMethod]
        public void X509CertificateWithInfoSucceedOnJsonWithInfo()
        {
            // arrange
            string json = X509CertificateWithInfoTests.MakeJson(SubjectName, Sha1Thumbprint, Sha256Thumbprint, IssuerName, NotBeforeUtcString, NotAfterUtcString, SerialNumber, Version);

            // act
            X509CertificateWithInfo x509CertificateWithInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<X509CertificateWithInfo>(json);

            // assert
            Assert.IsNotNull(x509CertificateWithInfo.Info);
            Assert.AreEqual(SubjectName, x509CertificateWithInfo.Info.SubjectName);
            Assert.AreEqual(Sha1Thumbprint, x509CertificateWithInfo.Info.Sha1Thumbprint);
            Assert.AreEqual(Sha256Thumbprint, x509CertificateWithInfo.Info.Sha256Thumbprint);
            Assert.AreEqual(IssuerName, x509CertificateWithInfo.Info.IssuerName);
            Assert.AreEqual(_notBeforeUtc, x509CertificateWithInfo.Info.NotBeforeUtc);
            Assert.AreEqual(_notAfterUtc, x509CertificateWithInfo.Info.NotAfterUtc);
            Assert.AreEqual(SerialNumber, x509CertificateWithInfo.Info.SerialNumber);
            Assert.AreEqual(Version, x509CertificateWithInfo.Info.Version);
        }
    }
}
