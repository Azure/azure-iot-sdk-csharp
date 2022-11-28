// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class X509CertificateWithInfoTests
    {
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

        [TestMethod]
        public void X509CertificateWithInfoConstructorThrowsOnNullX509Certificate()
        {
            // arrange
            const X509Certificate2 certificateNull = null;
#pragma warning disable SYSLIB0026 // Type or member is obsolete - Parameterless constructor is obsolete in NET6.0
            using var certificateEmpty = new X509Certificate2();
#pragma warning restore SYSLIB0026 // Type or member is obsolete - Parameterless constructor is obsolete in NET6.0
            const string certificateString = null;
            const string certificateStringEmpty = "";
            const string certificateStringInvalid =
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
            act.Should().Throw<ArgumentNullException>();

            act = () => _ = new X509CertificateWithInfo(certificateString);
            act.Should().Throw<ArgumentNullException>();

            act = () => _ = new X509CertificateWithInfo(certificateEmpty);
            var error = act.Should().Throw<ProvisioningServiceException>();
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
            using var certificate = new X509Certificate2(Encoding.ASCII.GetBytes(PublicKeyCertificate));

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
    }
}
