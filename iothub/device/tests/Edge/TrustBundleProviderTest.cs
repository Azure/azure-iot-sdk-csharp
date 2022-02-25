// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography;
using Microsoft.Azure.Devices.Client.Edge;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests.Edge
{

    [TestClass]
    [TestCategory("Unit")]
    public class TrustBundleProviderTest
    {
        private const string certificatesString =
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

        string certificateStringInvalid =
                "-----BEGIN CERTIFICATE-----\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxx\n" +
                "-----END CERTIFICATE-----\n";

        [TestMethod]
        public void TestParseCertificates_Single_ShouldReturnCetificate()
        {
            var certs = TrustBundleProvider.ParseCertificates(certificatesString);

            Assert.AreEqual(certs.Count(), 1);
        }

        [TestMethod]
        public void TestParseCertificates_MultipleCerts_ShouldReturnCetificates()
        {
            var certs = TrustBundleProvider.ParseCertificates(certificatesString + certificatesString);

            Assert.AreEqual(certs.Count(), 2);
        }

        [TestMethod]
        public void TestParseCertificates_WithNonCertificatesEntries_ShouldReturnCetificates()
        {
            var certs = TrustBundleProvider.ParseCertificates(certificatesString + certificatesString + "test");

            Assert.AreEqual(certs.Count(), 2);
        }

        [TestMethod]
        public void TestParseCertificates_NoCertificatesEntries_ShouldReturnNoCetificates()
        {
            var certs = TrustBundleProvider.ParseCertificates("test");

            Assert.AreEqual(certs.Count(), 0);
        }

        [TestMethod]
        public void TestParseCertificates_InvalidCertificates_ShouldThrow()
        {
            TestAssert.Throws<CryptographicException>(() => TrustBundleProvider.ParseCertificates(certificateStringInvalid));
        }

        [TestMethod]
        public void TestParseCertificates_NullCertificates_ShouldThrow()
        {
            TestAssert.Throws<InvalidOperationException>(() => TrustBundleProvider.ParseCertificates(null));
        }

        [TestMethod]
        public void TestParseCertificates_EmptyCertificates_ShouldThrow()
        {
            TestAssert.Throws<InvalidOperationException>(() => TrustBundleProvider.ParseCertificates(string.Empty));
        }
    }
}
