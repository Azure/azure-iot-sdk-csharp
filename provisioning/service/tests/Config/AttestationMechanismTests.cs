﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class AttestationMechanismTests
    {
        private const string SampleEndorsementKey = "AToAAQALAAMAsgAgg3GXZ0SEs/gakMyNRqXXJP1S124GUgtk8qHaGzMUaaoABgCAAEMAEAgAAAAAAAEAxsj" +
            "2gUScTk1UjuioeTlfGYZrrimExB+bScH75adUMRIi2UOMxG1kw4y+9RW/IVoMl4e620VxZad0ARX2gUqVjYO7KPVt3dyKhZS3dkcvfBisB" +
            "hP1XH9B33VqHG9SHnbnQXdBUaCgKAfxome8UmBKfe+naTsE5fkvjb/do3/dD6l4sGBwFCnKRdln4XpM03zLpoHFao8zOwt8l/uP3qUIxmC" +
            "Yv9A7m69Ms+5/pCkTu/rK4mRDsfhZ0QLfbzVI6zQFOKF/rwsfBtFeWlWtcuJMKlXdD8TXWElTzgh7JS4qhFzreL0c1mI0GCj+Aws0usZh7" +
            "dLIVPnlgZcBhgy1SSDQMQ==";
        TpmAttestation SampleTpmAttestation = new TpmAttestation(SampleEndorsementKey);
        private const string SampleId = "valid-id";
        private const string SamplePublicKeyCertificateString =
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
        private X509Attestation SampleX509RootAttestation = X509Attestation.CreateFromRootCertificates(SamplePublicKeyCertificateString);

        private string SampleX509AttestationJson =
            "{\n" +
            "   \"type\":\"x509\",\n" +
            "   \"x509\":{\n" +
            "       \"signingCertificates\":{\n" +
            "           \"primary\":{\n" +
            "               \"info\": {\n" +
            "                   \"subjectName\": \"CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US\",\n" +
            "                   \"sha1Thumbprint\": \"0000000000000000000000000000000000\",\n" +
            "                   \"sha256Thumbprint\": \"" + SampleId + "\",\n" +
            "                   \"issuerName\": \"CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US\",\n" +
            "                   \"notBeforeUtc\": \"2017-11-14T12:34:18Z\",\n" +
            "                   \"notAfterUtc\": \"2017-11-20T12:34:18Z\",\n" +
            "                   \"serialNumber\": \"000000000000000000\",\n" +
            "                   \"version\": 3\n" +
            "               }\n" +
            "           }\n" +
            "       }\n" +
            "   }\n" +
            "}";

        private string SampleTpmAttestationJson =
            "{\n" +
            "   \"type\":\"tpm\",\n" +
            "   \"tpm\":{\n" +
            "       \"endorsementKey\":\"" + SampleEndorsementKey + "\"\n" +
            "   }\n" +
            "}";

        private sealed class UnknownAttestation : Attestation
        {

        }


        /* SRS_ATTESTATION_MECHANISM_21_001: [The constructor shall throw ArgumentNullException if the provided attestation is null.] */
        [TestMethod]
        public void AttestationMechanismConstructorThrowsOnAttestationNull()
        {
            // arrange - act - assert
            TestAssert.Throws<ArgumentNullException>(() => new AttestationMechanism(null));
        }

        /* SRS_ATTESTATION_MECHANISM_21_002: [If the provided attestation is instance of TpmAttestation, the constructor shall store the provided TPM keys.] */
        /* SRS_ATTESTATION_MECHANISM_21_003: [If the provided attestation is instance of TpmAttestation, the constructor shall set the attestation type as TPM.] */
        /* SRS_ATTESTATION_MECHANISM_21_004: [If the provided attestation is instance of TpmAttestation, the constructor shall set the x508 as null.] */
        /* SRS_ATTESTATION_MECHANISM_21_010: [If the type is `TPM`, the getAttestation shall return the stored TpmAttestation.] */
        [TestMethod]
        public void AttestationMechanismConstructorSucceedOnTPMAttestation()
        {
            // arrange - act
            var attestationMechanism = new AttestationMechanism(SampleTpmAttestation);

            // assert
            Assert.IsNotNull(attestationMechanism);
            Assert.AreEqual(SampleEndorsementKey, ((TpmAttestation)attestationMechanism.GetAttestation()).EndorsementKey);
            Assert.AreEqual(AttestationMechanismType.Tpm, attestationMechanism.Type);
        }

        /* SRS_ATTESTATION_MECHANISM_21_005: [The constructor shall throw ArgumentException if the provided attestation is unknown.] */
        [TestMethod]
        public void AttestationMechanismConstructorThrowsOnUnknownAttestation()
        {
            // arrange
            var unknownAttestation = new UnknownAttestation();

            // act - assert
            TestAssert.Throws<ArgumentException>(() => new AttestationMechanism(unknownAttestation));
        }

        /* SRS_ATTESTATION_MECHANISM_21_006: [If the provided attestation is instance of X509Attestation, the constructor shall store the provided x509 certificates.] */
        /* SRS_ATTESTATION_MECHANISM_21_007: [If the provided attestation is instance of X509Attestation, the constructor shall set the attestation type as X509.] */
        /* SRS_ATTESTATION_MECHANISM_21_008: [If the provided attestation is instance of X509Attestation, the constructor shall set the TPM as null.] */
        /* SRS_ATTESTATION_MECHANISM_21_011: [If the type is `X509`, the getAttestation shall return the stored X509Attestation.] */
        [TestMethod]
        public void AttestationMechanismConstructorSucceedOnX509Attestation()
        {
            // arrange - act
            var attestationMechanism = new AttestationMechanism(SampleX509RootAttestation);

            // assert
            Assert.IsNotNull(attestationMechanism);
            Assert.AreEqual(SamplePublicKeyCertificateString, ((X509Attestation)attestationMechanism.GetAttestation()).RootCertificates.Primary.Certificate);
            Assert.AreEqual(AttestationMechanismType.X509, attestationMechanism.Type);
        }

        /* SRS_ATTESTATION_MECHANISM_21_012: [If the type is not `X509` or `TPM`, the getAttestation shall throw ProvisioningServiceClientException.] */
        //Requirement not testable.

        /* SRS_ATTESTATION_MECHANISM_21_013: [The constructor shall throw ProvisioningServiceClientException if the provided AttestationMechanismType 
                                                is `TPM` but the TPM attestation is null.] */
        [TestMethod]
        public void AttestationMechanismConstructorJSONThrowsOnTypeTPMWithX509Attestation()
        {
            // arrange
            string invalidJson =
                "{\n" +
                "   \"type\":\"tpm\",\n" +
                "   \"x509\":{\n" +
                "       \"signingCertificates\":{\n" +
                "           \"primary\":{\n" +
                "               \"info\": {\n" +
                "                   \"subjectName\": \"CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US\",\n" +
                "                   \"sha1Thumbprint\": \"0000000000000000000000000000000000\",\n" +
                "                   \"sha256Thumbprint\": \"" + SampleId + "\",\n" +
                "                   \"issuerName\": \"CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US\",\n" +
                "                   \"notBeforeUtc\": \"2017-11-14T12:34:18Z\",\n" +
                "                   \"notAfterUtc\": \"2017-11-20T12:34:18Z\",\n" +
                "                   \"serialNumber\": \"000000000000000000\",\n" +
                "                   \"version\": 3\n" +
                "               }\n" +
                "           }\n" +
                "       }\n" +
                "   }\n" +
                "}";

            // act - assert
            TestAssert.Throws<ProvisioningServiceClientException>(() => JsonConvert.DeserializeObject<AttestationMechanism>(invalidJson));
        }

        /* SRS_ATTESTATION_MECHANISM_21_014: [If the provided AttestationMechanismType is `TPM`, the constructor shall store the provided TPM attestation.] */
        [TestMethod]
        public void AttestationMechanismConstructorJSONSucceedForTPM()
        {
            // arrange
            AttestationMechanism attestationMechanism = JsonConvert.DeserializeObject<AttestationMechanism>(SampleTpmAttestationJson);

            // act - assert
            Assert.IsNotNull(attestationMechanism);
            Assert.AreEqual(AttestationMechanismType.Tpm, attestationMechanism.Type);
            Assert.IsTrue(attestationMechanism.GetAttestation() is TpmAttestation);
        }

        /* SRS_ATTESTATION_MECHANISM_21_015: [The constructor shall throw ProvisioningServiceClientException if the provided 
                                             AttestationMechanismType is `x509` but the x509 attestation is null.] */
        [TestMethod]
        public void AttestationMechanismConstructorJSONThrowsOnTypeX509WithTPMAttestation()
        {
            // arrange
            string invalidJson =
            "{\n" +
            "   \"type\":\"x509\",\n" +
            "   \"tpm\":{\n" +
            "       \"endorsementKey\":\"" + SampleEndorsementKey + "\"\n" +
            "   }\n" +
            "}";

            // act - assert
            TestAssert.Throws<ProvisioningServiceClientException>(() => JsonConvert.DeserializeObject<AttestationMechanism>(invalidJson));
        }


        /* SRS_ATTESTATION_MECHANISM_21_016: [If the provided AttestationMechanismType is `x509`, the constructor shall store the provided x509 attestation.] */
        [TestMethod]
        public void AttestationMechanismConstructorJSONSucceedForX509()
        {
            // arrange
            AttestationMechanism attestationMechanism = JsonConvert.DeserializeObject<AttestationMechanism>(SampleX509AttestationJson);

            // act - assert
            Assert.IsNotNull(attestationMechanism);
            Assert.AreEqual(AttestationMechanismType.X509, attestationMechanism.Type);
            Assert.IsTrue(attestationMechanism.GetAttestation() is X509Attestation);
        }

        [TestMethod]
        public void AttestationMechanismConstructorJSONSucceedOnNoneType()
        {
            // arrange
            string typeNoneJson =
            "{\n" +
            "   \"type\":\"none\",\n" +
            "   \"tpm\":{\n" +
            "       \"endorsementKey\":\"" + SampleEndorsementKey + "\"\n" +
            "   }\n" +
            "}";

            // act - assert
            Assert.IsNotNull(JsonConvert.DeserializeObject<AttestationMechanism>(typeNoneJson));
        }

        [TestMethod]
        public void AttestationMechanismConstructorJSONSucceedOnSymmetricKeyType()
        {
            // arrange
            string samplePrimaryKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("000000000000000000"));
            string sampleSecondaryKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("111111111111111111"));
            string symmetricKeyJson =
            "{\n" +
            "   \"type\":\"symmetricKey\",\n" +
            "   \"symmetricKey\":{\n" +
            "       \"primaryKey\":\"" + samplePrimaryKey + "\",\n" +
            "       \"secondaryKey\":\"" + sampleSecondaryKey + "\"\n" +
            "   }\n" +
            "}";

            // act
            AttestationMechanism attestationMechanism = JsonConvert.DeserializeObject<AttestationMechanism>(symmetricKeyJson);
            
            //assert
            Assert.IsNotNull(attestationMechanism);
            Assert.IsTrue(attestationMechanism.Type == AttestationMechanismType.SymmetricKey);
            Assert.IsTrue(attestationMechanism.GetAttestation() is SymmetricKeyAttestation);
            var symmetricKeyAttestation = (SymmetricKeyAttestation) attestationMechanism.GetAttestation();

            Assert.AreEqual(samplePrimaryKey, symmetricKeyAttestation.PrimaryKey);
            Assert.AreEqual(sampleSecondaryKey, symmetricKeyAttestation.SecondaryKey);
        }

    }
}
