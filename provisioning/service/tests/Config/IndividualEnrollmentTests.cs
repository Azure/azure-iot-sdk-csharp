﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class IndividualEnrollmentTests
    {
        private const string SampleRegistrationId = "valid-registration-id";
        private const string SampleDeviceId = "valid-device-id";
        private const string SampleIotHubHostName = "ContosoIoTHub.azure-devices.net";
        private const ProvisioningStatus SampleProvisioningStatus = ProvisioningStatus.Enabled;
        private const string SampleCreateDateTimeUTCString = "2017-11-14T12:34:18.123Z";
        private DateTime SampleCreateDateTimeUTC = new DateTime(2017, 11, 14, 12, 34, 18, 123, DateTimeKind.Utc);
        private const string SampleLastUpdatedDateTimeUTCString = "2017-11-14T12:34:18.321Z";
        private DateTime SampleLastUpdatedDateTimeUTC = new DateTime(2017, 11, 14, 12, 34, 18, 321, DateTimeKind.Utc);
        private const string SampleEtag = "00000000-0000-0000-0000-00000000000";
        private DeviceCapabilities SampleEdgeCapabilityTrue = new DeviceCapabilities { IotEdge = true };
        private DeviceCapabilities SampleEdgeCapabilityFalse = new DeviceCapabilities { IotEdge = false };
        private const string SampleEndorsementKey =
            "AToAAQALAAMAsgAgg3GXZ0SEs/gakMyNRqXXJP1S124GUgtk8qHaGzMUaaoABgCAAEMAEAgAAAAAAAEAxsj" +
            "2gUScTk1UjuioeTlfGYZrrimExB+bScH75adUMRIi2UOMxG1kw4y+9RW/IVoMl4e620VxZad0ARX2gUqVjY" +
            "O7KPVt3dyKhZS3dkcvfBisBhP1XH9B33VqHG9SHnbnQXdBUaCgKAfxome8UmBKfe+naTsE5fkvjb/do3/dD" +
            "6l4sGBwFCnKRdln4XpM03zLpoHFao8zOwt8l/uP3qUIxmCYv9A7m69Ms+5/pCkTu/rK4mRDsfhZ0QLfbzVI" +
            "6zQFOKF/rwsfBtFeWlWtcuJMKlXdD8TXWElTzgh7JS4qhFzreL0c1mI0GCj+Aws0usZh7dLIVPnlgZcBhgy" +
            "1SSDQMQ==";
        private TpmAttestation SampleTpmAttestation = new TpmAttestation(SampleEndorsementKey);
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
        private const string SampleCAReference = "valid/ca/reference";
        private X509Attestation SampleX509RootAttestation = X509Attestation.CreateFromRootCertificates(SamplePublicKeyCertificateString);
        private X509Attestation SampleX509ClientAttestation = X509Attestation.CreateFromClientCertificates(SamplePublicKeyCertificateString);
        private X509Attestation SampleX509CAReferenceAttestation = X509Attestation.CreateFromCAReferences(SampleCAReference);
        private static string SampleIndividualEnrollmentJsonBody =
            "   \"registrationId\":\"" + SampleRegistrationId + "\",\n" +
            "   \"attestation\":{\n" +
            "       \"type\":\"x509\",\n" +
            "       \"x509\":{\n" +
            "           \"clientCertificates\":{\n" +
            "               \"primary\":{\n" +
            "                   \"info\": {\n" +
            "                       \"subjectName\": \"CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US\",\n" +
            "                       \"sha1Thumbprint\": \"0000000000000000000000000000000000\",\n" +
            "                       \"sha256Thumbprint\": \"" + SampleRegistrationId + "\",\n" +
            "                       \"issuerName\": \"CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US\",\n" +
            "                       \"notBeforeUtc\": \"2017-11-14T12:34:18Z\",\n" +
            "                       \"notAfterUtc\": \"2017-11-20T12:34:18Z\",\n" +
            "                       \"serialNumber\": \"000000000000000000\",\n" +
            "                       \"version\": 3\n" +
            "                   }\n" +
            "               }\n" +
            "           }\n" +
            "       }\n" +
            "   },\n" +
            "   \"iotHubHostName\":\"" + SampleIotHubHostName + "\",\n" +
            "   \"deviceId\":\"" + SampleDeviceId + "\",\n" +
            "   \"initialTwin\":{\n" +
            "       \"tags\":{\n" +
            "           \"tag1\":\"val1\",\n" +
            "       },\n" +
            "   },\n" +
            "   \"provisioningStatus\":\"" + SampleProvisioningStatus + "\",\n" +
            "   \"createdDateTimeUtc\": \"" + SampleCreateDateTimeUTCString + "\",\n" +
            "   \"lastUpdatedDateTimeUtc\": \"" + SampleLastUpdatedDateTimeUTCString + "\",\n" +
            "   \"etag\": \"" + SampleEtag + "\",\n";

        private string SampleIndividualEnrollmentJsonWithoutCapabilities =
            "{\n" +
                SampleIndividualEnrollmentJsonBody + 
            "}\n";

        private string SampleIndividualEnrollmentJsonWithCapabilitiesTrue =
            "{\n" +
                SampleIndividualEnrollmentJsonBody +
            "   \"capabilities\": {\n" +
            "       \"iotEdge\": true \n" +
            "       },\n" +
            "}\n";

        private string SampleIndividualEnrollmentJsonWithCapabilitiesFalse =
            "{\n" +
                SampleIndividualEnrollmentJsonBody +
            "   \"capabilities\": {\n" +
            "       \"iotEdge\": false \n" +
            "       },\n" +
            "}\n";

        /* SRS_DEVICE_ENROLLMENT_21_001: [The constructor shall store the provided parameters.] */
        [TestMethod]
        public void IndividualEnrollmentConstructorSucceedOnTPM()
        {
            // arrange - act
            var individualEnrollment = new IndividualEnrollment(SampleRegistrationId, SampleTpmAttestation);

            // assert
            Assert.AreEqual(SampleRegistrationId, individualEnrollment.RegistrationId);
            Assert.AreEqual(SampleEndorsementKey, ((TpmAttestation)individualEnrollment.Attestation).EndorsementKey);
        }

        [TestMethod]
        public void IndividualEnrollmentConstructorSucceedOnX509Client()
        {
            // arrange - act
            var individualEnrollment = new IndividualEnrollment(SampleRegistrationId, SampleX509ClientAttestation);

            // assert
            Assert.AreEqual(SampleRegistrationId, individualEnrollment.RegistrationId);
            Assert.AreEqual(SamplePublicKeyCertificateString, ((X509Attestation)individualEnrollment.Attestation).ClientCertificates.Primary.Certificate);
        }

        [TestMethod]
        public void IndividualEnrollmentConstructorSucceedOnX509CAReference()
        {
            // arrange - act
            var individualEnrollment = new IndividualEnrollment(SampleRegistrationId, SampleX509CAReferenceAttestation);

            // assert
            Assert.AreEqual(SampleRegistrationId, individualEnrollment.RegistrationId);
            Assert.AreEqual(SampleCAReference, ((X509Attestation)individualEnrollment.Attestation).CAReferences.Primary);
        }

        /* SRS_DEVICE_ENROLLMENT_21_002: [The constructor shall throws ArgumentException if one of the provided parameters is null.] */
        [TestMethod]
        public void IndividualEnrollmentConstructorThrowsOnInvalidParameters()
        {
            // arrange - act - assert
            TestAssert.Throws<ArgumentException>(() => new IndividualEnrollment(SampleRegistrationId, null));
            TestAssert.Throws<ArgumentException>(() => new IndividualEnrollment(SampleRegistrationId, SampleX509RootAttestation));
        }

        /* SRS_INDIVIDUAL_ENROLLMENT_21_003: [The constructor shall throws ProvisioningServiceClientException if one of the 
                                                provided parameters in JSON is not valid.] */
        [TestMethod]
        public void IndividualEnrollmentConstructorJSONThrowsOnNonRegistrationID()
        {
            // arrange
            string invalidJson =
            "{\n" +
            "   \"attestation\":{\n" +
            "       \"type\":\"x509\",\n" +
            "       \"x509\":{\n" +
            "           \"signingCertificates\":{\n" +
            "               \"primary\":{\n" +
            "                   \"info\": {\n" +
            "                       \"subjectName\": \"CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US\",\n" +
            "                       \"sha1Thumbprint\": \"0000000000000000000000000000000000\",\n" +
            "                       \"sha256Thumbprint\": \"" + SampleRegistrationId + "\",\n" +
            "                       \"issuerName\": \"CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US\",\n" +
            "                       \"notBeforeUtc\": \"2017-11-14T12:34:18Z\",\n" +
            "                       \"notAfterUtc\": \"2017-11-20T12:34:18Z\",\n" +
            "                       \"serialNumber\": \"000000000000000000\",\n" +
            "                       \"version\": 3\n" +
            "                   }\n" +
            "               }\n" +
            "           }\n" +
            "       }\n" +
            "   },\n" +
            "   \"iotHubHostName\":\"" + SampleIotHubHostName + "\",\n" +
            "   \"deviceId\":\"" + SampleDeviceId + "\",\n" +
            "   \"initialTwin\":{\n" +
            "       \"tags\":{\n" +
            "           \"tag1\":\"val1\",\n" +
            "       },\n" +
            "   },\n" +
            "   \"provisioningStatus\":\"" + SampleProvisioningStatus + "\",\n" +
            "   \"createdDateTimeUtc\": \"" + SampleCreateDateTimeUTCString + "\",\n" +
            "   \"lastUpdatedDateTimeUtc\": \"" + SampleLastUpdatedDateTimeUTCString + "\",\n" +
            "   \"etag\": \"" + SampleEtag + "\"\n" +
            "}";

            // act - assert
            TestAssert.Throws<ProvisioningServiceClientException>(() => Newtonsoft.Json.JsonConvert.DeserializeObject<IndividualEnrollment>(invalidJson));
        }

        [TestMethod]
        public void IndividualEnrollmentConstructorJSONThrowsOnNonAttestation()
        {
            // arrange
            string invalidJson =
            "{\n" +
            "   \"registrationId\":\"" + SampleRegistrationId + "\",\n" +
            "   \"iotHubHostName\":\"" + SampleIotHubHostName + "\",\n" +
            "   \"deviceId\":\"" + SampleDeviceId + "\",\n" +
            "   \"initialTwin\":{\n" +
            "       \"tags\":{\n" +
            "           \"tag1\":\"val1\",\n" +
            "       },\n" +
            "   },\n" +
            "   \"provisioningStatus\":\"" + SampleProvisioningStatus + "\",\n" +
            "   \"createdDateTimeUtc\": \"" + SampleCreateDateTimeUTCString + "\",\n" +
            "   \"lastUpdatedDateTimeUtc\": \"" + SampleLastUpdatedDateTimeUTCString + "\",\n" +
            "   \"etag\": \"" + SampleEtag + "\"\n" +
            "}";

            // act - assert
            TestAssert.Throws<ProvisioningServiceClientException>(() => Newtonsoft.Json.JsonConvert.DeserializeObject<IndividualEnrollment>(invalidJson));
        }

        [TestMethod]
        public void IndividualEnrollmentConstructorJSONThrowsOnNonEtag()
        {
            // arrange
            string invalidJson =
            "{\n" +
            "   \"registrationId\":\"" + SampleRegistrationId + "\",\n" +
            "   \"attestation\":{\n" +
            "       \"type\":\"x509\",\n" +
            "       \"x509\":{\n" +
            "           \"signingCertificates\":{\n" +
            "               \"primary\":{\n" +
            "                   \"info\": {\n" +
            "                       \"subjectName\": \"CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US\",\n" +
            "                       \"sha1Thumbprint\": \"0000000000000000000000000000000000\",\n" +
            "                       \"sha256Thumbprint\": \"" + SampleRegistrationId + "\",\n" +
            "                       \"issuerName\": \"CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US\",\n" +
            "                       \"notBeforeUtc\": \"2017-11-14T12:34:18Z\",\n" +
            "                       \"notAfterUtc\": \"2017-11-20T12:34:18Z\",\n" +
            "                       \"serialNumber\": \"000000000000000000\",\n" +
            "                       \"version\": 3\n" +
            "                   }\n" +
            "               }\n" +
            "           }\n" +
            "       }\n" +
            "   },\n" +
            "   \"iotHubHostName\":\"" + SampleIotHubHostName + "\",\n" +
            "   \"deviceId\":\"" + SampleDeviceId + "\",\n" +
            "   \"initialTwin\":{\n" +
            "       \"tags\":{\n" +
            "           \"tag1\":\"val1\",\n" +
            "       },\n" +
            "   },\n" +
            "   \"provisioningStatus\":\"" + SampleProvisioningStatus + "\",\n" +
            "   \"createdDateTimeUtc\": \"" + SampleCreateDateTimeUTCString + "\",\n" +
            "   \"lastUpdatedDateTimeUtc\": \"" + SampleLastUpdatedDateTimeUTCString + "\"\n" +
            "}";

            // act - assert
            TestAssert.Throws<ProvisioningServiceClientException>(() => Newtonsoft.Json.JsonConvert.DeserializeObject<IndividualEnrollment>(invalidJson));
        }

        /* SRS_INDIVIDUAL_ENROLLMENT_21_004: [The constructor shall store all parameters in the JSON.] */
        [TestMethod]
        public void IndividualEnrollmentConstructorWithoutCapabilitiesJSONSucceed()
        {
            // arrange
            IndividualEnrollment individualEnrollment = Newtonsoft.Json.JsonConvert.DeserializeObject<IndividualEnrollment>(SampleIndividualEnrollmentJsonWithoutCapabilities);

            // act - assert
            Assert.IsNotNull(individualEnrollment);
            Assert.AreEqual(SampleRegistrationId, individualEnrollment.RegistrationId);
            Assert.IsTrue(individualEnrollment.Attestation is X509Attestation);
            Assert.AreEqual(SampleDeviceId, individualEnrollment.DeviceId);
            Assert.AreEqual(SampleIotHubHostName, individualEnrollment.IotHubHostName);
            Assert.IsNotNull(individualEnrollment.InitialTwinState);
            Assert.AreEqual(SampleProvisioningStatus, individualEnrollment.ProvisioningStatus);
            Assert.AreEqual(SampleCreateDateTimeUTC, individualEnrollment.CreatedDateTimeUtc);
            Assert.AreEqual(SampleLastUpdatedDateTimeUTC, individualEnrollment.LastUpdatedDateTimeUtc);
            Assert.AreEqual(SampleEtag, individualEnrollment.ETag);
            Assert.AreEqual(null, individualEnrollment.Capabilities);
        }

        [TestMethod]
        public void IndividualEnrollmentConstructorWithCapabilitiesTrueJSONSucceed()
        {
            // arrange
            IndividualEnrollment individualEnrollment = Newtonsoft.Json.JsonConvert.DeserializeObject<IndividualEnrollment>(SampleIndividualEnrollmentJsonWithCapabilitiesTrue);

            // act - assert
            Assert.IsNotNull(individualEnrollment);
            Assert.AreEqual(SampleRegistrationId, individualEnrollment.RegistrationId);
            Assert.IsTrue(individualEnrollment.Attestation is X509Attestation);
            Assert.AreEqual(SampleDeviceId, individualEnrollment.DeviceId);
            Assert.AreEqual(SampleIotHubHostName, individualEnrollment.IotHubHostName);
            Assert.IsNotNull(individualEnrollment.InitialTwinState);
            Assert.AreEqual(SampleProvisioningStatus, individualEnrollment.ProvisioningStatus);
            Assert.AreEqual(SampleCreateDateTimeUTC, individualEnrollment.CreatedDateTimeUtc);
            Assert.AreEqual(SampleLastUpdatedDateTimeUTC, individualEnrollment.LastUpdatedDateTimeUtc);
            Assert.AreEqual(SampleEtag, individualEnrollment.ETag);
            Assert.AreEqual(SampleEdgeCapabilityTrue.IotEdge, individualEnrollment.Capabilities.IotEdge);
        }

        [TestMethod]
        public void IndividualEnrollmentConstructorWithCapabilitiesFalseJSONSucceed()
        {
            // arrange
            IndividualEnrollment individualEnrollment = Newtonsoft.Json.JsonConvert.DeserializeObject<IndividualEnrollment>(SampleIndividualEnrollmentJsonWithCapabilitiesFalse);

            // act - assert
            Assert.IsNotNull(individualEnrollment);
            Assert.AreEqual(SampleRegistrationId, individualEnrollment.RegistrationId);
            Assert.IsTrue(individualEnrollment.Attestation is X509Attestation);
            Assert.AreEqual(SampleDeviceId, individualEnrollment.DeviceId);
            Assert.AreEqual(SampleIotHubHostName, individualEnrollment.IotHubHostName);
            Assert.IsNotNull(individualEnrollment.InitialTwinState);
            Assert.AreEqual(SampleProvisioningStatus, individualEnrollment.ProvisioningStatus);
            Assert.AreEqual(SampleCreateDateTimeUTC, individualEnrollment.CreatedDateTimeUtc);
            Assert.AreEqual(SampleLastUpdatedDateTimeUTC, individualEnrollment.LastUpdatedDateTimeUtc);
            Assert.AreEqual(SampleEtag, individualEnrollment.ETag);
            Assert.AreEqual(SampleEdgeCapabilityFalse.IotEdge, individualEnrollment.Capabilities.IotEdge);
        }

        [TestMethod]
        public void IndividualEnrollmentConstructorJSONSucceedOnMinumum()
        {
            // arrange
            string minJson =
                "{\n" +
                "   \"registrationId\":\"" + SampleRegistrationId + "\",\n" +
                "   \"attestation\":{\n" +
                "       \"type\":\"x509\",\n" +
                "       \"x509\":{\n" +
                "           \"clientCertificates\":{\n" +
                "               \"primary\":{\n" +
                "                   \"info\": {\n" +
                "                       \"subjectName\": \"CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US\",\n" +
                "                       \"sha1Thumbprint\": \"0000000000000000000000000000000000\",\n" +
                "                       \"sha256Thumbprint\": \"" + SampleRegistrationId + "\",\n" +
                "                       \"issuerName\": \"CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US\",\n" +
                "                       \"notBeforeUtc\": \"2017-11-14T12:34:18Z\",\n" +
                "                       \"notAfterUtc\": \"2017-11-20T12:34:18Z\",\n" +
                "                       \"serialNumber\": \"000000000000000000\",\n" +
                "                       \"version\": 3\n" +
                "                   }\n" +
                "               }\n" +
                "           }\n" +
                "       }\n" +
                "   },\n" +
                "   \"etag\": \"" + SampleEtag + "\"\n" +
                "}";
            IndividualEnrollment individualEnrollment = Newtonsoft.Json.JsonConvert.DeserializeObject<IndividualEnrollment>(minJson);

            // act - assert
            Assert.IsNotNull(individualEnrollment);
            Assert.AreEqual(SampleRegistrationId, individualEnrollment.RegistrationId);
            Assert.IsTrue(individualEnrollment.Attestation is X509Attestation);
            Assert.AreEqual(SampleEtag, individualEnrollment.ETag);
        }
    }
}
