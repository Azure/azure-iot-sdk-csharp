// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using FluentAssertions;
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
        private const string SampleEtag = "00000000-0000-0000-0000-00000000000";
        private readonly DeviceCapabilities _sampleEdgeCapabilityTrue = new() { IotEdge = true };
        private readonly DeviceCapabilities _sampleEdgeCapabilityFalse = new() { IotEdge = false };

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
        private readonly X509Attestation _sampleX509CAReferenceAttestation = X509Attestation.CreateFromCAReferences(SampleCAReference);

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
            "   \"etag\": \"" + SampleEtag + "\",\n";

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

        private string SampleIndividualEnrollmentJsonWithCapabilitiesFalse =
            "{\n" +
                s_sampleIndividualEnrollmentJsonBody +
            "   \"capabilities\": {\n" +
            "       \"iotEdge\": false \n" +
            "       },\n" +
            "}\n";

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
            Assert.AreEqual(SampleCAReference, ((X509Attestation)individualEnrollment.Attestation).CAReferences.Primary);
        }

        [TestMethod]
        public void IndividualEnrollmentConstructorThrowsOnInvalidParameters()
        {
            // arrange - act - assert
            TestAssert.Throws<ArgumentNullException>(() => new IndividualEnrollment(SampleRegistrationId, null));
            TestAssert.Throws<InvalidOperationException>(() => new IndividualEnrollment(SampleRegistrationId, _sampleX509RootAttestation));
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
            "   \"etag\": \"" + SampleEtag + "\"\n" +
            "}";

            // act - assert
            Action act = () => JsonConvert.DeserializeObject<IndividualEnrollment>(invalidJson);
            var error = act.Should().Throw<InvalidOperationException>();
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
            "   \"etag\": \"" + SampleEtag + "\"\n" +
            "}";

            // act - assert
            Action act = () => JsonConvert.DeserializeObject<IndividualEnrollment>(invalidJson);
            var error = act.Should().Throw<DeviceProvisioningServiceException>();
            error.And.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            error.And.IsTransient.Should().BeFalse();
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
            var error = act.Should().Throw<InvalidOperationException>();
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
            Assert.AreEqual(_sampleCreateDateTimeUTC, individualEnrollment.CreatedDateTimeUtc);
            Assert.AreEqual(_sampleLastUpdatedDateTimeUTC, individualEnrollment.LastUpdatedDateTimeUtc);
            Assert.AreEqual(SampleEtag, individualEnrollment.ETag);
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
            Assert.AreEqual(_sampleCreateDateTimeUTC, individualEnrollment.CreatedDateTimeUtc);
            Assert.AreEqual(_sampleLastUpdatedDateTimeUTC, individualEnrollment.LastUpdatedDateTimeUtc);
            Assert.AreEqual(SampleEtag, individualEnrollment.ETag);
            Assert.AreEqual(_sampleEdgeCapabilityTrue.IotEdge, individualEnrollment.Capabilities.IotEdge);
        }

        [TestMethod]
        public void IndividualEnrollmentConstructorWithCapabilitiesFalseJsonSucceed()
        {
            // arrange
            IndividualEnrollment individualEnrollment = JsonConvert.DeserializeObject<IndividualEnrollment>(SampleIndividualEnrollmentJsonWithCapabilitiesFalse);

            // act - assert
            Assert.IsNotNull(individualEnrollment);
            Assert.AreEqual(SampleRegistrationId, individualEnrollment.RegistrationId);
            Assert.IsTrue(individualEnrollment.Attestation is X509Attestation);
            Assert.AreEqual(SampleDeviceId, individualEnrollment.DeviceId);
            Assert.AreEqual(SampleIotHubHostName, individualEnrollment.IotHubHostName);
            Assert.IsNotNull(individualEnrollment.InitialTwinState);
            Assert.AreEqual(SampleProvisioningStatus, individualEnrollment.ProvisioningStatus);
            Assert.AreEqual(_sampleCreateDateTimeUTC, individualEnrollment.CreatedDateTimeUtc);
            Assert.AreEqual(_sampleLastUpdatedDateTimeUTC, individualEnrollment.LastUpdatedDateTimeUtc);
            Assert.AreEqual(SampleEtag, individualEnrollment.ETag);
            Assert.AreEqual(_sampleEdgeCapabilityFalse.IotEdge, individualEnrollment.Capabilities.IotEdge);
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
                "   \"etag\": \"" + SampleEtag + "\"\n" +
                "}";
            IndividualEnrollment individualEnrollment = JsonConvert.DeserializeObject<IndividualEnrollment>(minJson);

            // act - assert
            Assert.IsNotNull(individualEnrollment);
            Assert.AreEqual(SampleRegistrationId, individualEnrollment.RegistrationId);
            Assert.IsTrue(individualEnrollment.Attestation is X509Attestation);
            Assert.AreEqual(SampleEtag, individualEnrollment.ETag);
        }
    }
}
