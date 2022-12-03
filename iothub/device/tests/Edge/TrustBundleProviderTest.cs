// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Microsoft.Azure.Devices.Client.Edge;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test.Edge
{

    [TestClass]
    [TestCategory("Unit")]
    public class TrustBundleProviderTest
    {
        private const string CertificatesString =
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
        private const string CertificateStringInvalid =
                "-----BEGIN CERTIFICATE-----\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxx\n" +
                "-----END CERTIFICATE-----\n";

        [TestMethod]
        public void TestParseCertificates_Single_ShouldReturnCetificate()
        {
            IList<X509Certificate2> certs = TrustBundleProvider.ParseCertificates(CertificatesString);

            Assert.AreEqual(certs.Count, 1);
        }

        [TestMethod]
        public void TestParseCertificates_MultipleCerts_ShouldReturnCetificates()
        {
            IList<X509Certificate2> certs = TrustBundleProvider.ParseCertificates(CertificatesString + CertificatesString);

            Assert.AreEqual(certs.Count, 2);
        }

        [TestMethod]
        public void TestParseCertificates_WithNonCertificatesEntries_ShouldReturnCetificates()
        {
            IList<X509Certificate2> certs = TrustBundleProvider.ParseCertificates(CertificatesString + CertificatesString + "test");

            Assert.AreEqual(certs.Count, 2);
        }

        [TestMethod]
        public void TestParseCertificates_NoCertificatesEntries_ShouldReturnNoCetificates()
        {
            IList<X509Certificate2> certs = TrustBundleProvider.ParseCertificates("test");

            Assert.AreEqual(certs.Count, 0);
        }

        [TestMethod]
        public void TestParseCertificates_InvalidCertificates_ShouldThrow()
        {
            Action act = () => TrustBundleProvider.ParseCertificates(CertificateStringInvalid);
            act.Should().Throw<CryptographicException>();
        }

        [TestMethod]
        public void TestParseCertificates_NullCertificates_ShouldThrow()
        {
            Action act = () => TrustBundleProvider.ParseCertificates(null);
            act.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void TestParseCertificates_EmptyCertificates_ShouldThrow()
        {
            Action act = () => TrustBundleProvider.ParseCertificates(string.Empty);
            act.Should().Throw<InvalidOperationException>();
        }
    }
}
