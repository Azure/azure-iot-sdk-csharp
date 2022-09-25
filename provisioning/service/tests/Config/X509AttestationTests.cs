// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;


namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class X509AttestationTests
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
        private const string CA_REFERENCE_STRING = "valid-ca-reference";

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

        private string MakeX509AttestationJson(string certName, bool primaryOnly = false)
        {
            string json =
                "{" +
                "  \"" + certName + "\": {" +
                "    \"primary\": " +
                MakeCertInfoJson(SUBJECT_NAME, SHA1THUMBPRINT, SHA256THUMBPRINT, ISSUER_NAME, NOT_BEFORE_UTC_STRING, NOT_AFTER_UTC_STRING, SERIAL_NUMBER, VERSION) +
                (primaryOnly?"":
                "," +
                "    \"secondary\": " +
                MakeCertInfoJson(SUBJECT_NAME, SHA1THUMBPRINT, SHA256THUMBPRINT, ISSUER_NAME, NOT_BEFORE_UTC_STRING, NOT_AFTER_UTC_STRING, SERIAL_NUMBER, VERSION)
                ) +
                "  }" +
                "}";

            return json;
        }


        /* SRS_X509_ATTESTATION_21_001: [The factory shall throws ArgumentException if the primary certificate is null or empty.] */
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

        /* SRS_X509_ATTESTATION_21_002: [The factory shall create a new instance of the X509Certificates with the provided primary and secondary certificates.] */
        /* SRS_X509_ATTESTATION_21_003: [The factory shall create a new instance of the X509Attestation with the created X509Certificates as the ClientCertificates.] */
        [TestMethod]
        public void X509AttestationCreateFromClientCertificatesSucceedOnPrimaryCertificate()
        {
            // arrange
            var primary = new X509Certificate2(Encoding.ASCII.GetBytes(PUBLIC_KEY_CERTIFICATE_STRING));

            // act
            var attestation = X509Attestation.CreateFromClientCertificates(primary);
            primary.Dispose();

            // assert
            Assert.IsNotNull(attestation.ClientCertificates.Primary);
            Assert.IsNull(attestation.ClientCertificates.Secondary);
            Assert.IsNull(attestation.RootCertificates);
            Assert.IsNull(attestation.CAReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromClientCertificatesSucceedOnPrimaryAndSecondaryCertificates()
        {
            // arrange
            var primary = new X509Certificate2(Encoding.ASCII.GetBytes(PUBLIC_KEY_CERTIFICATE_STRING));
            var secondary = new X509Certificate2(Encoding.ASCII.GetBytes(PUBLIC_KEY_CERTIFICATE_STRING));

            // act
            var attestation = X509Attestation.CreateFromClientCertificates(primary, secondary);
            primary.Dispose();
            secondary.Dispose();

            // assert
            Assert.IsNotNull(attestation.ClientCertificates.Primary);
            Assert.IsNotNull(attestation.ClientCertificates.Secondary);
            Assert.IsNull(attestation.RootCertificates);
            Assert.IsNull(attestation.CAReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromClientCertificatesSucceedOnPrimaryAndSecondaryNullCertificates()
        {
            // arrange
            var primary = new X509Certificate2(Encoding.ASCII.GetBytes(PUBLIC_KEY_CERTIFICATE_STRING));
            X509Certificate2 secondary = null;

            // act
            var attestation = X509Attestation.CreateFromClientCertificates(primary, secondary);
            primary.Dispose();

            // assert
            Assert.IsNotNull(attestation.ClientCertificates.Primary);
            Assert.IsNull(attestation.ClientCertificates.Secondary);
            Assert.IsNull(attestation.RootCertificates);
            Assert.IsNull(attestation.CAReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromClientCertificatesSucceedOnPrimaryString()
        {
            // arrange
            string primary = PUBLIC_KEY_CERTIFICATE_STRING;

            // act
            var attestation = X509Attestation.CreateFromClientCertificates(primary);

            // assert
            Assert.IsNotNull(attestation.ClientCertificates.Primary);
            Assert.IsNull(attestation.ClientCertificates.Secondary);
            Assert.IsNull(attestation.RootCertificates);
            Assert.IsNull(attestation.CAReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromClientCertificatesSucceedOnPrimaryAndSecondaryString()
        {
            // arrange
            string primary = PUBLIC_KEY_CERTIFICATE_STRING;
            string secondary = PUBLIC_KEY_CERTIFICATE_STRING;

            // act
            var attestation = X509Attestation.CreateFromClientCertificates(primary, secondary);

            // assert
            Assert.IsNotNull(attestation.ClientCertificates.Primary);
            Assert.IsNotNull(attestation.ClientCertificates.Secondary);
            Assert.IsNull(attestation.RootCertificates);
            Assert.IsNull(attestation.CAReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromClientCertificatesSucceedOnPrimaryAndSecondaryNullString()
        {
            // arrange
            string primary = PUBLIC_KEY_CERTIFICATE_STRING;
            string secondary = null;

            // act
            var attestation = X509Attestation.CreateFromClientCertificates(primary, secondary);

            // assert
            Assert.IsNotNull(attestation.ClientCertificates.Primary);
            Assert.IsNull(attestation.ClientCertificates.Secondary);
            Assert.IsNull(attestation.RootCertificates);
            Assert.IsNull(attestation.CAReferences);
        }

        /* SRS_X509_ATTESTATION_21_004: [The factory shall create a new instance of the X509Attestation with the created X509Certificates as the RootCertificates.] */
        [TestMethod]
        public void X509AttestationCreateFromRootCertificatesSucceedOnPrimaryCertificate()
        {
            // arrange
            var primary = new X509Certificate2(Encoding.ASCII.GetBytes(PUBLIC_KEY_CERTIFICATE_STRING));

            // act
            var attestation = X509Attestation.CreateFromRootCertificates(primary);
            primary.Dispose();

            // assert
            Assert.IsNotNull(attestation.RootCertificates.Primary);
            Assert.IsNull(attestation.RootCertificates.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.CAReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromRootCertificatesSucceedOnPrimaryAndSecondaryCertificates()
        {
            // arrange
            var primary = new X509Certificate2(Encoding.ASCII.GetBytes(PUBLIC_KEY_CERTIFICATE_STRING));
            var secondary = new X509Certificate2(Encoding.ASCII.GetBytes(PUBLIC_KEY_CERTIFICATE_STRING));

            // act
            var attestation = X509Attestation.CreateFromRootCertificates(primary, secondary);
            primary.Dispose();
            secondary.Dispose();

            // assert
            Assert.IsNotNull(attestation.RootCertificates.Primary);
            Assert.IsNotNull(attestation.RootCertificates.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.CAReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromRootCertificatesSucceedOnPrimaryAndSecondaryNullCertificates()
        {
            // arrange
            var primary = new X509Certificate2(Encoding.ASCII.GetBytes(PUBLIC_KEY_CERTIFICATE_STRING));
            X509Certificate2 secondary = null;

            // act
            var attestation = X509Attestation.CreateFromRootCertificates(primary, secondary);
            primary.Dispose();

            // assert
            Assert.IsNotNull(attestation.RootCertificates.Primary);
            Assert.IsNull(attestation.RootCertificates.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.CAReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromRootCertificatesSucceedOnPrimaryString()
        {
            // arrange
            string primary = PUBLIC_KEY_CERTIFICATE_STRING;

            // act
            var attestation = X509Attestation.CreateFromRootCertificates(primary);

            // assert
            Assert.IsNotNull(attestation.RootCertificates.Primary);
            Assert.IsNull(attestation.RootCertificates.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.CAReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromRootCertificatesSucceedOnPrimaryAndSecondaryString()
        {
            // arrange
            string primary = PUBLIC_KEY_CERTIFICATE_STRING;
            string secondary = PUBLIC_KEY_CERTIFICATE_STRING;

            // act
            var attestation = X509Attestation.CreateFromRootCertificates(primary, secondary);

            // assert
            Assert.IsNotNull(attestation.RootCertificates.Primary);
            Assert.IsNotNull(attestation.RootCertificates.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.CAReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromRootCertificatesSucceedOnPrimaryAndSecondaryNullString()
        {
            // arrange
            string primary = PUBLIC_KEY_CERTIFICATE_STRING;
            string secondary = null;

            // act
            var attestation = X509Attestation.CreateFromRootCertificates(primary, secondary);

            // assert
            Assert.IsNotNull(attestation.RootCertificates.Primary);
            Assert.IsNull(attestation.RootCertificates.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.CAReferences);
        }

        /* SRS_X509_ATTESTATION_21_005: [The factory shall throws ArgumentException if the primary CA reference is null or empty.] */
        [TestMethod]
        public void X509AttestationCreateFromCAReferencesThrowsOnNullPrimaryCertificate()
        {
            // arrange
            string primaryStr = null;
            string secondaryStr = null;

            // act - assert
            TestAssert.Throws<ArgumentException>(() => X509Attestation.CreateFromCAReferences(primaryStr));
            TestAssert.Throws<ArgumentException>(() => X509Attestation.CreateFromCAReferences(primaryStr, secondaryStr));
        }

        /* SRS_X509_ATTESTATION_21_006: [The factory shall create a new instance of the X509Certificates with the provided primary and secondary CA reference.] */
        /* SRS_X509_ATTESTATION_21_007: [The factory shall create a new instance of the X509Attestation with the created X509Certificates as the caReference.] */
        [TestMethod]
        public void X509AttestationCreateFromCAReferencesSucceedOnPrimaryString()
        {
            // arrange
            string primary = CA_REFERENCE_STRING;

            // act
            var attestation = X509Attestation.CreateFromCAReferences(primary);

            // assert
            Assert.IsNotNull(attestation.CAReferences.Primary);
            Assert.IsNull(attestation.CAReferences.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.RootCertificates);
        }

        [TestMethod]
        public void X509AttestationCreateFromCAReferencesSucceedOnPrimaryAndSecondaryString()
        {
            // arrange
            string primary = CA_REFERENCE_STRING;
            string secondary = CA_REFERENCE_STRING;

            // act
            var attestation = X509Attestation.CreateFromCAReferences(primary, secondary);

            // assert
            Assert.IsNotNull(attestation.CAReferences.Primary);
            Assert.IsNotNull(attestation.CAReferences.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.RootCertificates);
        }

        [TestMethod]
        public void X509AttestationCreateFromCAReferencesSucceedOnPrimaryAndSecondaryNullString()
        {
            // arrange
            string primary = CA_REFERENCE_STRING;
            string secondary = null;

            // act
            var attestation = X509Attestation.CreateFromCAReferences(primary, secondary);

            // assert
            Assert.IsNotNull(attestation.CAReferences.Primary);
            Assert.IsNull(attestation.CAReferences.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.RootCertificates);
        }

        /* SRS_X509_ATTESTATION_21_008: [If the ClientCertificates is not null, the GetPrimaryX509CertificateInfo shall return the info in the Primary key of the ClientCertificates.] */
        /* SRS_X509_ATTESTATION_21_012: [If the ClientCertificates is not null, and it contains Secondary key, the GetSecondaryX509CertificateInfo shall return the info in the Secondary key of the ClientCertificates.] */
        /* SRS_X509_ATTESTATION_21_016: [The constructor shall store the provided `clientCertificates`, `rootCertificates`, and `caReferences`.] */
        [TestMethod]
        public void X509AttestationGetX509CertificateInfoSucceedOnPrimaryAndSecondaryClientCertificates()
        {
            // arrange
            string json = MakeX509AttestationJson("clientCertificates");
            X509Attestation attestation = Newtonsoft.Json.JsonConvert.DeserializeObject<X509Attestation>(json);

            // act - assert
            Assert.IsNotNull(attestation.GetPrimaryX509CertificateInfo());
            Assert.IsNotNull(attestation.GetSecondaryX509CertificateInfo());
        }

        [TestMethod]
        public void X509AttestationGetX509CertificateInfoSucceedOnPrimaryOnlyClientCertificates()
        {
            // arrange
            string json = MakeX509AttestationJson("clientCertificates", true);
            X509Attestation attestation = Newtonsoft.Json.JsonConvert.DeserializeObject<X509Attestation>(json);

            // act - assert
            Assert.IsNotNull(attestation.GetPrimaryX509CertificateInfo());
            Assert.IsNull(attestation.GetSecondaryX509CertificateInfo());
        }

        /* SRS_X509_ATTESTATION_21_009: [If the RootCertificates is not null, the GetPrimaryX509CertificateInfo shall return the info in the Primary key of the RootCertificates.] */
        /* SRS_X509_ATTESTATION_21_013: [If the RootCertificates is not null, and it contains Secondary key, the GetSecondaryX509CertificateInfo shall return the info in the Secondary key of the RootCertificates.] */
        [TestMethod]
        public void X509AttestationGetX509CertificateInfoSucceedOnPrimaryAndSecondaryRoottCertificates()
        {
            // arrange
            string json = MakeX509AttestationJson("signingCertificates");
            X509Attestation attestation = Newtonsoft.Json.JsonConvert.DeserializeObject<X509Attestation>(json);

            // act - assert
            Assert.IsNotNull(attestation.GetPrimaryX509CertificateInfo());
            Assert.IsNotNull(attestation.GetSecondaryX509CertificateInfo());
        }

        /* SRS_X509_ATTESTATION_21_010: [If the CAReferences is not null, the GetPrimaryX509CertificateInfo shall return null.] */
        [TestMethod]
        public void X509AttestationGetX509CertificateInfoSucceedOnPrimaryAndSecondaryCAReferences()
        {
            // arrange
            string json =
                "{" +
                "  \"caReferences\": {" +
                "    \"primary\": \"" + CA_REFERENCE_STRING + "\"," +
                "    \"secondary\": \"" + CA_REFERENCE_STRING + "\"" +
                "  }" +
                "}";
            X509Attestation attestation = Newtonsoft.Json.JsonConvert.DeserializeObject<X509Attestation>(json);

            // act - assert
            Assert.IsNull(attestation.GetPrimaryX509CertificateInfo());
            Assert.IsNull(attestation.GetSecondaryX509CertificateInfo());
        }

        /* SRS_X509_ATTESTATION_21_011: [If ClientCertificates, RootCertificates, and CAReferences are null, the GetPrimaryX509CertificateInfo shall throw ArgumentException.] */
        /*
         * Not testable! 
         */

        /* SRS_X509_ATTESTATION_21_014: [The constructor shall throws ArgumentException if `clientCertificates`, `rootCertificates`, and `caReferences` are null.] */
        [TestMethod]
        public void X509AttestationJsonConstructorTrowsOnNoCert()
        {
            // arrange
            string json = "{}";

            // act - assert
            TestAssert.Throws<DeviceProvisioningServiceException>(() => Newtonsoft.Json.JsonConvert.DeserializeObject<X509Attestation>(json));
        }

        /* SRS_X509_ATTESTATION_21_015: [The constructor shall throws ArgumentException if more than one certificate type are not null.] */
        [TestMethod]
        public void X509AttestationJsonConstructorTrowsOnClientAndRootCertificates()
        {
            // arrange
            string json =
                "{" +
                "  \"clientCertificates\": {" +
                "    \"primary\": " +
                MakeCertInfoJson(SUBJECT_NAME, SHA1THUMBPRINT, SHA256THUMBPRINT, ISSUER_NAME, NOT_BEFORE_UTC_STRING, NOT_AFTER_UTC_STRING, SERIAL_NUMBER, VERSION) +
                "  }," +
                "  \"signingCertificates\": {" +
                "    \"primary\": " +
                MakeCertInfoJson(SUBJECT_NAME, SHA1THUMBPRINT, SHA256THUMBPRINT, ISSUER_NAME, NOT_BEFORE_UTC_STRING, NOT_AFTER_UTC_STRING, SERIAL_NUMBER, VERSION) +
                "  }" +
                "}";

            // act - assert
            TestAssert.Throws<DeviceProvisioningServiceException>(() => Newtonsoft.Json.JsonConvert.DeserializeObject<X509Attestation>(json));
        }

        [TestMethod]
        public void X509AttestationJsonConstructorTrowsOnClientCertificatesAndCAReferences()
        {
            // arrange
            string json =
                "{" +
                "  \"clientCertificates\": {" +
                "    \"primary\": " +
                MakeCertInfoJson(SUBJECT_NAME, SHA1THUMBPRINT, SHA256THUMBPRINT, ISSUER_NAME, NOT_BEFORE_UTC_STRING, NOT_AFTER_UTC_STRING, SERIAL_NUMBER, VERSION) +
                "  }," +
                "  \"caReferences\": {" +
                "    \"primary\": \"" + CA_REFERENCE_STRING + "\"" +
                "  }" +
                "}";

            // act - assert
            TestAssert.Throws<DeviceProvisioningServiceException>(() => Newtonsoft.Json.JsonConvert.DeserializeObject<X509Attestation>(json));
        }

        [TestMethod]
        public void X509AttestationJsonConstructorTrowsOnRootCertificatesAndCAReferences()
        {
            // arrange
            string json =
                "{" +
                "  \"signingCertificates\": {" +
                "    \"primary\": " +
                MakeCertInfoJson(SUBJECT_NAME, SHA1THUMBPRINT, SHA256THUMBPRINT, ISSUER_NAME, NOT_BEFORE_UTC_STRING, NOT_AFTER_UTC_STRING, SERIAL_NUMBER, VERSION) +
                "  }," +
                "  \"caReferences\": {" +
                "    \"primary\": \"" + CA_REFERENCE_STRING + "\"" +
                "  }" +
                "}";

            // act - assert
            TestAssert.Throws<DeviceProvisioningServiceException>(() => Newtonsoft.Json.JsonConvert.DeserializeObject<X509Attestation>(json));
        }
    }
}
