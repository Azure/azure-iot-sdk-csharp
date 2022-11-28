// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class X509CertificatesTests
    {
        private const string PublicKeyCertificateString =
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

        [TestMethod]
        public void X509CertificatesSucceedOnValidPrimaryX509Certificate()
        {
            // arrange
            using var primary = new X509Certificate2(System.Text.Encoding.ASCII.GetBytes(PublicKeyCertificateString));

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
            using var primary = new X509Certificate2(System.Text.Encoding.ASCII.GetBytes(PublicKeyCertificateString));
            using var secondary = new X509Certificate2(System.Text.Encoding.ASCII.GetBytes(PublicKeyCertificateString));

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
            string primary = PublicKeyCertificateString;

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
            string primary = PublicKeyCertificateString;
            string secondary = PublicKeyCertificateString;

            // act
            var x509Certificates = new X509Certificates(primary, secondary);

            // assert
            Assert.IsNotNull(x509Certificates.Primary);
            Assert.IsNotNull(x509Certificates.Secondary);
        }

        [TestMethod]
        public void X509CertificatesThrowsOnNullPrimaryX509Certificate()
        {
            Action act = () => _ = new X509Certificates((X509Certificate2)null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void X509CertificatesThrowsOnNullPrimaryString()
        {
            Action act = () => _ = new X509Certificates((string)null);
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
