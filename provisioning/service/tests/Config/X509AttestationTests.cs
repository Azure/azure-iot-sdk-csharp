// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using FluentAssertions;


namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class X509AttestationTests
    {
        private const string SubjectName = "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US";
        private const string Sha1Thumbprint = "0000000000000000000000000000000000";
        private const string Sha256Thumbprint = "validEnrollmentGroupId";
        private const string IssuerName = "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US";
        private const string NotBeforeUtcString = "2017-11-14T12:34:18.123Z";
        private static readonly DateTime s_notBeforeUtc = new(2017, 11, 14, 12, 34, 18, 123, DateTimeKind.Utc);
        private const string NotAfterUtcString = "2017-11-14T12:34:18.321Z";
        private static readonly DateTime s_notAfterUtc = new(2017, 11, 14, 12, 34, 18, 321, DateTimeKind.Utc);
        private const string SerialNumber = "000000000000000000";
        private const int Version = 3;
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
        private const string CaReferenceString = "valid-ca-reference";

        [TestMethod]
        public void X509AttestationCreateFromClientCertificatesThrowsOnNullPrimaryCertificate()
        {
            // arrange
            X509Certificate2 primaryCert = null;
            X509Certificate2 secondaryCert = null;
            string primaryStr = null;
            string secondaryStr = null;

            // act - assert
            TestAssert.Throws<ArgumentException>(() => X509Attestation.CreateFromClientCertificates(primaryCert));
            TestAssert.Throws<ArgumentException>(() => X509Attestation.CreateFromClientCertificates(primaryCert, secondaryCert));
            TestAssert.Throws<ArgumentException>(() => X509Attestation.CreateFromClientCertificates(primaryStr));
            TestAssert.Throws<ArgumentException>(() => X509Attestation.CreateFromClientCertificates(primaryStr, secondaryStr));
        }

        [TestMethod]
        public void X509AttestationCreateFromRootCertificatesThrowsOnNullPrimaryCertificate()
        {
            // arrange
            X509Certificate2 primaryCert = null;
            X509Certificate2 secondaryCert = null;
            string primaryStr = null;
            string secondaryStr = null;

            // act - assert
            TestAssert.Throws<ArgumentException>(() => X509Attestation.CreateFromRootCertificates(primaryCert));
            TestAssert.Throws<ArgumentException>(() => X509Attestation.CreateFromRootCertificates(primaryCert, secondaryCert));
            TestAssert.Throws<ArgumentException>(() => X509Attestation.CreateFromRootCertificates(primaryStr));
            TestAssert.Throws<ArgumentException>(() => X509Attestation.CreateFromRootCertificates(primaryStr, secondaryStr));
        }

        [TestMethod]
        public void X509AttestationCreateFromClientCertificatesSucceedOnPrimaryCertificate()
        {
            // arrange
            var primary = new X509Certificate2(Encoding.ASCII.GetBytes(PublicKeyCertificateString));

            // act
            var attestation = X509Attestation.CreateFromClientCertificates(primary);
            primary.Dispose();

            // assert
            Assert.IsNotNull(attestation.ClientCertificates.Primary);
            Assert.IsNull(attestation.ClientCertificates.Secondary);
            Assert.IsNull(attestation.RootCertificates);
            Assert.IsNull(attestation.CaReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromClientCertificatesSucceedOnPrimaryAndSecondaryCertificates()
        {
            // arrange
            var primary = new X509Certificate2(Encoding.ASCII.GetBytes(PublicKeyCertificateString));
            var secondary = new X509Certificate2(Encoding.ASCII.GetBytes(PublicKeyCertificateString));

            // act
            var attestation = X509Attestation.CreateFromClientCertificates(primary, secondary);
            primary.Dispose();
            secondary.Dispose();

            // assert
            Assert.IsNotNull(attestation.ClientCertificates.Primary);
            Assert.IsNotNull(attestation.ClientCertificates.Secondary);
            Assert.IsNull(attestation.RootCertificates);
            Assert.IsNull(attestation.CaReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromClientCertificatesSucceedOnPrimaryAndSecondaryNullCertificates()
        {
            // arrange
            var primary = new X509Certificate2(Encoding.ASCII.GetBytes(PublicKeyCertificateString));
            X509Certificate2 secondary = null;

            // act
            var attestation = X509Attestation.CreateFromClientCertificates(primary, secondary);
            primary.Dispose();

            // assert
            Assert.IsNotNull(attestation.ClientCertificates.Primary);
            Assert.IsNull(attestation.ClientCertificates.Secondary);
            Assert.IsNull(attestation.RootCertificates);
            Assert.IsNull(attestation.CaReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromClientCertificatesSucceedOnPrimaryString()
        {
            // arrange
            string primary = PublicKeyCertificateString;

            // act
            var attestation = X509Attestation.CreateFromClientCertificates(primary);

            // assert
            Assert.IsNotNull(attestation.ClientCertificates.Primary);
            Assert.IsNull(attestation.ClientCertificates.Secondary);
            Assert.IsNull(attestation.RootCertificates);
            Assert.IsNull(attestation.CaReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromClientCertificatesSucceedOnPrimaryAndSecondaryString()
        {
            // arrange
            string primary = PublicKeyCertificateString;
            string secondary = PublicKeyCertificateString;

            // act
            var attestation = X509Attestation.CreateFromClientCertificates(primary, secondary);

            // assert
            Assert.IsNotNull(attestation.ClientCertificates.Primary);
            Assert.IsNotNull(attestation.ClientCertificates.Secondary);
            Assert.IsNull(attestation.RootCertificates);
            Assert.IsNull(attestation.CaReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromClientCertificatesSucceedOnPrimaryAndSecondaryNullString()
        {
            // arrange
            string primary = PublicKeyCertificateString;
            string secondary = null;

            // act
            var attestation = X509Attestation.CreateFromClientCertificates(primary, secondary);

            // assert
            Assert.IsNotNull(attestation.ClientCertificates.Primary);
            Assert.IsNull(attestation.ClientCertificates.Secondary);
            Assert.IsNull(attestation.RootCertificates);
            Assert.IsNull(attestation.CaReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromRootCertificatesSucceedOnPrimaryCertificate()
        {
            // arrange
            var primary = new X509Certificate2(Encoding.ASCII.GetBytes(PublicKeyCertificateString));

            // act
            var attestation = X509Attestation.CreateFromRootCertificates(primary);
            primary.Dispose();

            // assert
            Assert.IsNotNull(attestation.RootCertificates.Primary);
            Assert.IsNull(attestation.RootCertificates.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.CaReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromRootCertificatesSucceedOnPrimaryAndSecondaryCertificates()
        {
            // arrange
            var primary = new X509Certificate2(Encoding.ASCII.GetBytes(PublicKeyCertificateString));
            var secondary = new X509Certificate2(Encoding.ASCII.GetBytes(PublicKeyCertificateString));

            // act
            var attestation = X509Attestation.CreateFromRootCertificates(primary, secondary);
            primary.Dispose();
            secondary.Dispose();

            // assert
            Assert.IsNotNull(attestation.RootCertificates.Primary);
            Assert.IsNotNull(attestation.RootCertificates.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.CaReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromRootCertificatesSucceedOnPrimaryAndSecondaryNullCertificates()
        {
            // arrange
            var primary = new X509Certificate2(Encoding.ASCII.GetBytes(PublicKeyCertificateString));
            X509Certificate2 secondary = null;

            // act
            var attestation = X509Attestation.CreateFromRootCertificates(primary, secondary);
            primary.Dispose();

            // assert
            Assert.IsNotNull(attestation.RootCertificates.Primary);
            Assert.IsNull(attestation.RootCertificates.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.CaReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromRootCertificatesSucceedOnPrimaryString()
        {
            // arrange
            string primary = PublicKeyCertificateString;

            // act
            var attestation = X509Attestation.CreateFromRootCertificates(primary);

            // assert
            Assert.IsNotNull(attestation.RootCertificates.Primary);
            Assert.IsNull(attestation.RootCertificates.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.CaReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromRootCertificatesSucceedOnPrimaryAndSecondaryString()
        {
            // arrange
            string primary = PublicKeyCertificateString;
            string secondary = PublicKeyCertificateString;

            // act
            var attestation = X509Attestation.CreateFromRootCertificates(primary, secondary);

            // assert
            Assert.IsNotNull(attestation.RootCertificates.Primary);
            Assert.IsNotNull(attestation.RootCertificates.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.CaReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromRootCertificatesSucceedOnPrimaryAndSecondaryNullString()
        {
            // arrange
            string primary = PublicKeyCertificateString;
            string secondary = null;

            // act
            var attestation = X509Attestation.CreateFromRootCertificates(primary, secondary);

            // assert
            Assert.IsNotNull(attestation.RootCertificates.Primary);
            Assert.IsNull(attestation.RootCertificates.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.CaReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromCAReferencesThrowsOnNullPrimaryCertificate()
        {
            // arrange
            string primaryStr = null;
            string secondaryStr = null;

            // act - assert
            TestAssert.Throws<ArgumentException>(() => X509Attestation.CreateFromCaReferences(primaryStr));
            TestAssert.Throws<ArgumentException>(() => X509Attestation.CreateFromCaReferences(primaryStr, secondaryStr));
        }

        [TestMethod]
        public void X509AttestationCreateFromCAReferencesSucceedOnPrimaryString()
        {
            // arrange
            string primary = CaReferenceString;

            // act
            var attestation = X509Attestation.CreateFromCaReferences(primary);

            // assert
            Assert.IsNotNull(attestation.CaReferences.Primary);
            Assert.IsNull(attestation.CaReferences.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.RootCertificates);
        }

        [TestMethod]
        public void X509AttestationCreateFromCAReferencesSucceedOnPrimaryAndSecondaryString()
        {
            // arrange
            string primary = CaReferenceString;
            string secondary = CaReferenceString;

            // act
            var attestation = X509Attestation.CreateFromCaReferences(primary, secondary);

            // assert
            Assert.IsNotNull(attestation.CaReferences.Primary);
            Assert.IsNotNull(attestation.CaReferences.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.RootCertificates);
        }

        [TestMethod]
        public void X509AttestationCreateFromCAReferencesSucceedOnPrimaryAndSecondaryNullString()
        {
            // arrange
            string primary = CaReferenceString;
            string secondary = null;

            // act
            var attestation = X509Attestation.CreateFromCaReferences(primary, secondary);

            // assert
            Assert.IsNotNull(attestation.CaReferences.Primary);
            Assert.IsNull(attestation.CaReferences.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.RootCertificates);
        }

        [TestMethod]
        public void X509AttestationGetX509CertificateInfoSucceedOnPrimaryAndSecondaryClientCertificates()
        {
            // arrange
            string json = X509AttestationTests.MakeX509AttestationJson("clientCertificates");
            X509Attestation attestation = JsonConvert.DeserializeObject<X509Attestation>(json);

            // act - assert
            Assert.IsNotNull(attestation.GetPrimaryX509CertificateInfo());
            Assert.IsNotNull(attestation.GetSecondaryX509CertificateInfo());
        }

        [TestMethod]
        public void X509AttestationGetX509CertificateInfoSucceedOnPrimaryOnlyClientCertificates()
        {
            // arrange
            string json = X509AttestationTests.MakeX509AttestationJson("clientCertificates", true);
            X509Attestation attestation = JsonConvert.DeserializeObject<X509Attestation>(json);

            // act - assert
            Assert.IsNotNull(attestation.GetPrimaryX509CertificateInfo());
            Assert.IsNull(attestation.GetSecondaryX509CertificateInfo());
        }

        [TestMethod]
        public void X509AttestationGetX509CertificateInfoSucceedOnPrimaryAndSecondaryRoottCertificates()
        {
            // arrange
            string json = X509AttestationTests.MakeX509AttestationJson("signingCertificates");
            X509Attestation attestation = JsonConvert.DeserializeObject<X509Attestation>(json);

            // act - assert
            Assert.IsNotNull(attestation.GetPrimaryX509CertificateInfo());
            Assert.IsNotNull(attestation.GetSecondaryX509CertificateInfo());
        }

        [TestMethod]
        public void X509AttestationGetX509CertificateInfoSucceedOnPrimaryAndSecondaryCAReferences()
        {
            // arrange
            string json =
                "{" +
                "  \"caReferences\": {" +
                "    \"primary\": \"" + CaReferenceString + "\"," +
                "    \"secondary\": \"" + CaReferenceString + "\"" +
                "  }" +
                "}";
            X509Attestation attestation = JsonConvert.DeserializeObject<X509Attestation>(json);

            // act - assert
            Assert.IsNull(attestation.GetPrimaryX509CertificateInfo());
            Assert.IsNull(attestation.GetSecondaryX509CertificateInfo());
        }

        [TestMethod]
        public void X509AttestationJsonConstructorThrowsOnNoCert()
        {
            // arrange
            string json = "{}";

            // act
            Action act = () => JsonConvert.DeserializeObject<X509Attestation>(json);

            // assert
            act.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void X509AttestationJsonConstructorThrowsOnClientAndRootCertificates()
        {
            // arrange
            string json =
                "{" +
                "  \"clientCertificates\": {" +
                "    \"primary\": " +
                X509AttestationTests.MakeCertInfoJson(SubjectName, Sha1Thumbprint, Sha256Thumbprint, IssuerName, NotBeforeUtcString, NotAfterUtcString, SerialNumber, Version) +
                "  }," +
                "  \"signingCertificates\": {" +
                "    \"primary\": " +
                X509AttestationTests.MakeCertInfoJson(SubjectName, Sha1Thumbprint, Sha256Thumbprint, IssuerName, NotBeforeUtcString, NotAfterUtcString, SerialNumber, Version) +
                "  }" +
                "}";

            Action act = () => JsonConvert.DeserializeObject<X509Attestation>(json);

            // assert
            act.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void X509AttestationJsonConstructorThrowsOnClientCertificatesAndCaReferences()
        {
            // arrange
            string json =
                "{" +
                "  \"clientCertificates\": {" +
                "    \"primary\": " +
                X509AttestationTests.MakeCertInfoJson(SubjectName, Sha1Thumbprint, Sha256Thumbprint, IssuerName, NotBeforeUtcString, NotAfterUtcString, SerialNumber, Version) +
                "  }," +
                "  \"caReferences\": {" +
                "    \"primary\": \"" + CaReferenceString + "\"" +
                "  }" +
                "}";

            // act
            Action act = () => JsonConvert.DeserializeObject<X509Attestation>(json);

            // assert
            act.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void X509AttestationJsonConstructorThrowsOnRootCertificatesAndCaReferences()
        {
            // arrange
            string json =
                "{" +
                "  \"signingCertificates\": {" +
                "    \"primary\": " +
                X509AttestationTests.MakeCertInfoJson(SubjectName, Sha1Thumbprint, Sha256Thumbprint, IssuerName, NotBeforeUtcString, NotAfterUtcString, SerialNumber, Version) +
                "  }," +
                "  \"caReferences\": {" +
                "    \"primary\": \"" + CaReferenceString + "\"" +
                "  }" +
                "}";

            // act - assert
            Action act = () => JsonConvert.DeserializeObject<X509Attestation>(json);

            // assert
            act.Should().Throw<InvalidOperationException>();
        }

        private static string MakeCertInfoJson(
            string subjectName,
            string sha1Thumbprint,
            string sha256Thumbprint,
            string issuerName,
            string notBeforeUtcString,
            string notAfterUtcString,
            string serialNumber,
            int version)
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

        private static string MakeX509AttestationJson(string certName, bool primaryOnly = false)
        {
            string json =
                "{" +
                "  \"" + certName + "\": {" +
                "    \"primary\": " +
                X509AttestationTests.MakeCertInfoJson(SubjectName, Sha1Thumbprint, Sha256Thumbprint, IssuerName, NotBeforeUtcString, NotAfterUtcString, SerialNumber, Version) +
                (primaryOnly ? "" :
                "," +
                "    \"secondary\": " +
                X509AttestationTests.MakeCertInfoJson(SubjectName, Sha1Thumbprint, Sha256Thumbprint, IssuerName, NotBeforeUtcString, NotAfterUtcString, SerialNumber, Version)
                ) +
                "  }" +
                "}";

            return json;
        }

    }
}
