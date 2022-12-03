// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using Azure;
using FluentAssertions;
using FluentAssertions.Specialized;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

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
        private readonly DateTime _sampleCreateDateTimeUTC = new(2017, 11, 14, 12, 34, 18, 123, DateTimeKind.Utc);
        private const string SampleLastUpdatedDateTimeUTCString = "2017-11-14T12:34:18.321Z";
        private readonly DateTime _sampleLastUpdatedDateTimeUTC = new(2017, 11, 14, 12, 34, 18, 321, DateTimeKind.Utc);
        private static readonly ETag s_sampleEtag = new("00000000-0000-0000-0000-00000000000");
        private readonly ProvisioningTwinCapabilities _sampleEdgeCapabilityTrue = new() { IsIotEdge = true };
        private readonly ProvisioningTwinCapabilities _sampleEdgeCapabilityFalse = new() { IsIotEdge = false };

        private const string SampleEndorsementKey =
            "AToAAQALAAMAsgAgg3GXZ0SEs/gakMyNRqXXJP1S124GUgtk8qHaGzMUaaoABgCAAEMAEAgAAAAAAAEAxsj" +
            "2gUScTk1UjuioeTlfGYZrrimExB+bScH75adUMRIi2UOMxG1kw4y+9RW/IVoMl4e620VxZad0ARX2gUqVjY" +
            "O7KPVt3dyKhZS3dkcvfBisBhP1XH9B33VqHG9SHnbnQXdBUaCgKAfxome8UmBKfe+naTsE5fkvjb/do3/dD" +
            "6l4sGBwFCnKRdln4XpM03zLpoHFao8zOwt8l/uP3qUIxmCYv9A7m69Ms+5/pCkTu/rK4mRDsfhZ0QLfbzVI" +
            "6zQFOKF/rwsfBtFeWlWtcuJMKlXdD8TXWElTzgh7JS4qhFzreL0c1mI0GCj+Aws0usZh7dLIVPnlgZcBhgy" +
            "1SSDQMQ==";

        private readonly TpmAttestation _sampleTpmAttestation = new(SampleEndorsementKey);

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
        private readonly X509Attestation _sampleX509RootAttestation = X509Attestation.CreateFromRootCertificates(SamplePublicKeyCertificateString);
        private readonly X509Attestation _sampleX509ClientAttestation = X509Attestation.CreateFromClientCertificates(SamplePublicKeyCertificateString);
        private readonly X509Attestation _sampleX509CAReferenceAttestation = X509Attestation.CreateFromCaReferences(SampleCAReference);

        private static readonly string s_sampleIndividualEnrollmentJsonBody =
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
            "   \"etag\": \"" + s_sampleEtag + "\",\n";

        private readonly string _sampleIndividualEnrollmentJsonWithoutCapabilities =
            "{\n" +
                s_sampleIndividualEnrollmentJsonBody +
            "}\n";

        private readonly string _sampleIndividualEnrollmentJsonWithCapabilitiesTrue =
            "{\n" +
                s_sampleIndividualEnrollmentJsonBody +
            "   \"capabilities\": {\n" +
            "       \"iotEdge\": true \n" +
            "       },\n" +
            "}\n";

        private readonly string _sampleIndividualEnrollmentJsonWithCapabilitiesFalse =
            "{\n" +
                s_sampleIndividualEnrollmentJsonBody +
            "   \"capabilities\": {\n" +
            "       \"iotEdge\": false \n" +
            "       },\n" +
            "}\n";

        [TestMethod]
        public void IndividualEnrollmentConstructorSucceedOnTpm()
        {
            // arrange - act
            var individualEnrollment = new IndividualEnrollment(SampleRegistrationId, _sampleTpmAttestation);

            // assert
            Assert.AreEqual(SampleRegistrationId, individualEnrollment.RegistrationId);
            Assert.AreEqual(SampleEndorsementKey, ((TpmAttestation)individualEnrollment.Attestation).EndorsementKey);
        }

        [TestMethod]
        public void IndividualEnrollmentConstructorSucceedOnX509Client()
        {
            // arrange - act
            var individualEnrollment = new IndividualEnrollment(SampleRegistrationId, _sampleX509ClientAttestation);

            // assert
            Assert.AreEqual(SampleRegistrationId, individualEnrollment.RegistrationId);
            Assert.AreEqual(SamplePublicKeyCertificateString, ((X509Attestation)individualEnrollment.Attestation).ClientCertificates.Primary.Certificate);
        }

        [TestMethod]
        public void IndividualEnrollmentConstructorSucceedOnX509CAReference()
        {
            // arrange - act
            var individualEnrollment = new IndividualEnrollment(SampleRegistrationId, _sampleX509CAReferenceAttestation);

            // assert
            Assert.AreEqual(SampleRegistrationId, individualEnrollment.RegistrationId);
            Assert.AreEqual(SampleCAReference, ((X509Attestation)individualEnrollment.Attestation).CaReferences.Primary);
        }

        [TestMethod]
        public void IndividualEnrollmentConstructorThrowsOnInvalidParameters()
        {
            Action act = () => _ = new IndividualEnrollment(SampleRegistrationId, _sampleX509RootAttestation);
            act.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void IndividualEnrollmentConstructorJsonThrowsOnNonRegistrationID()
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
            "   \"etag\": \"" + s_sampleEtag + "\"\n" +
            "}";

            // act - assert
            Action act = () => JsonConvert.DeserializeObject<IndividualEnrollment>(invalidJson);
            ExceptionAssertions<InvalidOperationException> error = act.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void IndividualEnrollmentConstructorJsonThrowsOnNonAttestation()
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
            "   \"etag\": \"" + s_sampleEtag + "\"\n" +
            "}";

            // act
            Action act = () => JsonConvert.DeserializeObject<IndividualEnrollment>(invalidJson);

            // assert
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void IndividualEnrollmentConstructorJsonThrowsOnNonEtag()
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
            Action act = () => JsonConvert.DeserializeObject<IndividualEnrollment>(invalidJson);
            ExceptionAssertions<InvalidOperationException> error = act.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void IndividualEnrollmentConstructorWithoutCapabilitiesJsonSucceed()
        {
            // arrange
            IndividualEnrollment individualEnrollment = JsonConvert.DeserializeObject<IndividualEnrollment>(_sampleIndividualEnrollmentJsonWithoutCapabilities);

            // act - assert
            Assert.IsNotNull(individualEnrollment);
            Assert.AreEqual(SampleRegistrationId, individualEnrollment.RegistrationId);
            Assert.IsTrue(individualEnrollment.Attestation is X509Attestation);
            Assert.AreEqual(SampleDeviceId, individualEnrollment.DeviceId);
            Assert.AreEqual(SampleIotHubHostName, individualEnrollment.IotHubHostName);
            Assert.IsNotNull(individualEnrollment.InitialTwinState);
            Assert.AreEqual(SampleProvisioningStatus, individualEnrollment.ProvisioningStatus);
            Assert.AreEqual(_sampleCreateDateTimeUTC, individualEnrollment.CreatedOnUtc);
            Assert.AreEqual(_sampleLastUpdatedDateTimeUTC, individualEnrollment.LastUpdatedOnUtc);
            Assert.AreEqual(s_sampleEtag, individualEnrollment.ETag);
            Assert.AreEqual(null, individualEnrollment.Capabilities);
        }

        [TestMethod]
        public void IndividualEnrollmentConstructorWithCapabilitiesTrueJsonSucceed()
        {
            // arrange
            IndividualEnrollment individualEnrollment = JsonConvert.DeserializeObject<IndividualEnrollment>(_sampleIndividualEnrollmentJsonWithCapabilitiesTrue);

            // act - assert
            Assert.IsNotNull(individualEnrollment);
            Assert.AreEqual(SampleRegistrationId, individualEnrollment.RegistrationId);
            Assert.IsTrue(individualEnrollment.Attestation is X509Attestation);
            Assert.AreEqual(SampleDeviceId, individualEnrollment.DeviceId);
            Assert.AreEqual(SampleIotHubHostName, individualEnrollment.IotHubHostName);
            Assert.IsNotNull(individualEnrollment.InitialTwinState);
            Assert.AreEqual(SampleProvisioningStatus, individualEnrollment.ProvisioningStatus);
            Assert.AreEqual(_sampleCreateDateTimeUTC, individualEnrollment.CreatedOnUtc);
            Assert.AreEqual(_sampleLastUpdatedDateTimeUTC, individualEnrollment.LastUpdatedOnUtc);
            Assert.AreEqual(s_sampleEtag, individualEnrollment.ETag);
            Assert.AreEqual(_sampleEdgeCapabilityTrue.IsIotEdge, individualEnrollment.Capabilities.IsIotEdge);
        }

        [TestMethod]
        public void IndividualEnrollmentConstructorWithCapabilitiesFalseJsonSucceed()
        {
            // arrange
            IndividualEnrollment individualEnrollment = JsonConvert.DeserializeObject<IndividualEnrollment>(_sampleIndividualEnrollmentJsonWithCapabilitiesFalse);

            // act - assert
            Assert.IsNotNull(individualEnrollment);
            Assert.AreEqual(SampleRegistrationId, individualEnrollment.RegistrationId);
            Assert.IsTrue(individualEnrollment.Attestation is X509Attestation);
            Assert.AreEqual(SampleDeviceId, individualEnrollment.DeviceId);
            Assert.AreEqual(SampleIotHubHostName, individualEnrollment.IotHubHostName);
            Assert.IsNotNull(individualEnrollment.InitialTwinState);
            Assert.AreEqual(SampleProvisioningStatus, individualEnrollment.ProvisioningStatus);
            Assert.AreEqual(_sampleCreateDateTimeUTC, individualEnrollment.CreatedOnUtc);
            Assert.AreEqual(_sampleLastUpdatedDateTimeUTC, individualEnrollment.LastUpdatedOnUtc);
            Assert.AreEqual(s_sampleEtag, individualEnrollment.ETag);
            Assert.AreEqual(_sampleEdgeCapabilityFalse.IsIotEdge, individualEnrollment.Capabilities.IsIotEdge);
        }

        [TestMethod]
        public void IndividualEnrollmentConstructorJsonSucceedOnMinumum()
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
                "   \"etag\": \"" + s_sampleEtag + "\"\n" +
                "}";
            IndividualEnrollment individualEnrollment = JsonConvert.DeserializeObject<IndividualEnrollment>(minJson);

            // act - assert
            Assert.IsNotNull(individualEnrollment);
            Assert.AreEqual(SampleRegistrationId, individualEnrollment.RegistrationId);
            Assert.IsTrue(individualEnrollment.Attestation is X509Attestation);
            Assert.AreEqual(s_sampleEtag, individualEnrollment.ETag);
        }
    }
}
