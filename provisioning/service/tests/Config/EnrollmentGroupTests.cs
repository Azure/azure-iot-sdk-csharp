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
    public class EnrollmentGroupTests
    {
        private const string SampleEnrollmentGroupId = "valid-enrollment-group-id";
        private const string SampleIotHubHostName = "ContosoIoTHub.azure-devices.net";
        private const ProvisioningStatus SampleProvisioningStatus = ProvisioningStatus.Enabled;
        private const string SampleCreateDateTimeUTCString = "2017-11-14T12:34:18.123Z";
        private readonly DateTime _sampleCreateDateTimeUTC = new(2017, 11, 14, 12, 34, 18, 123, DateTimeKind.Utc);
        private const string SampleLastUpdatedDateTimeUTCString = "2017-11-14T12:34:18.321Z";
        private readonly DateTime _sampleLastUpdatedDateTimeUTC = new(2017, 11, 14, 12, 34, 18, 321, DateTimeKind.Utc);
        private const string SampleEtag = "00000000-0000-0000-0000-00000000000";

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

        private readonly X509Attestation _sampleX509RootAttestation = X509Attestation.CreateFromRootCertificates(SamplePublicKeyCertificateString);
        private readonly X509Attestation _sampleX509ClientAttestation = X509Attestation.CreateFromClientCertificates(SamplePublicKeyCertificateString);

        private static readonly string s_sampleEnrollmentGroupJson =
            "{\n" +
            "   \"enrollmentGroupId\":\"" + SampleEnrollmentGroupId + "\",\n" +
            "   \"attestation\":{\n" +
            "       \"type\":\"x509\",\n" +
            "       \"x509\":{\n" +
            "           \"signingCertificates\":{\n" +
            "               \"primary\":{\n" +
            "                   \"info\": {\n" +
            "                       \"subjectName\": \"CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US\",\n" +
            "                       \"sha1Thumbprint\": \"0000000000000000000000000000000000\",\n" +
            "                       \"sha256Thumbprint\": \"" + SampleEnrollmentGroupId + "\",\n" +
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

        [TestMethod]
        public void EnrollmentGroupConstructorSucceed()
        {
            // arrange - act
            var individualEnrollment = new EnrollmentGroup(SampleEnrollmentGroupId, _sampleX509RootAttestation);

            // assert
            Assert.AreEqual(SampleEnrollmentGroupId, individualEnrollment.EnrollmentGroupId);
            Assert.AreEqual(SamplePublicKeyCertificateString, ((X509Attestation)individualEnrollment.Attestation).RootCertificates.Primary.Certificate);
        }

        [TestMethod]
        public void EnrollmentGroupConstructorThrowsOnInvalidParameters()
        {
            // arrange - act - assert
            _ = TestAssert.Throws<ArgumentException>(() => new EnrollmentGroup(SampleEnrollmentGroupId, null));
            _ = TestAssert.Throws<InvalidOperationException>(() => new EnrollmentGroup(SampleEnrollmentGroupId, _sampleX509ClientAttestation));
        }

        [TestMethod]
        public void EnrollmentGroupConstructorJsonThrowsOnNonAttestation()
        {
            // arrange
            string invalidJson =
                "{\n" +
                "   \"enrollmentGroupId\":\"" + SampleEnrollmentGroupId + "\",\n" +
                "   \"iotHubHostName\":\"" + SampleIotHubHostName + "\",\n" +
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
            Action act = () => JsonConvert.DeserializeObject<EnrollmentGroup>(invalidJson);
            FluentAssertions.Specialized.ExceptionAssertions<DeviceProvisioningServiceException> error = act.Should().Throw<DeviceProvisioningServiceException>();
            error.And.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            error.And.IsTransient.Should().BeFalse();
        }

        [TestMethod]
        public void EnrollmentGroupConstructorJsonSucceed()
        {
            // arrange
            EnrollmentGroup enrollmentGroup = JsonConvert.DeserializeObject<EnrollmentGroup>(s_sampleEnrollmentGroupJson);

            // act - assert
            Assert.IsNotNull(enrollmentGroup);
            Assert.AreEqual(SampleEnrollmentGroupId, enrollmentGroup.EnrollmentGroupId);
            Assert.IsTrue(enrollmentGroup.Attestation is X509Attestation);
            Assert.AreEqual(SampleIotHubHostName, enrollmentGroup.IotHubHostName);
            Assert.IsNotNull(enrollmentGroup.InitialTwinState);
            Assert.AreEqual(SampleProvisioningStatus, enrollmentGroup.ProvisioningStatus);
            Assert.AreEqual(_sampleCreateDateTimeUTC, enrollmentGroup.CreatedDateTimeUtc);
            Assert.AreEqual(_sampleLastUpdatedDateTimeUTC, enrollmentGroup.LastUpdatedDateTimeUtc);
            Assert.AreEqual(SampleEtag, enrollmentGroup.ETag);
        }

        [TestMethod]
        public void EnrollmentGroupConstructorJsonSucceedOnMinimumJson()
        {
            // arrange
            string minJson =
                "{\n" +
                "   \"enrollmentGroupId\":\"" + SampleEnrollmentGroupId + "\",\n" +
                "   \"attestation\":{\n" +
                "       \"type\":\"x509\",\n" +
                "       \"x509\":{\n" +
                "           \"signingCertificates\":{\n" +
                "               \"primary\":{\n" +
                "                   \"info\": {\n" +
                "                       \"subjectName\": \"CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US\",\n" +
                "                       \"sha1Thumbprint\": \"0000000000000000000000000000000000\",\n" +
                "                       \"sha256Thumbprint\": \"" + SampleEnrollmentGroupId + "\",\n" +
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
            EnrollmentGroup enrollmentGroup = JsonConvert.DeserializeObject<EnrollmentGroup>(minJson);

            // act - assert
            Assert.IsNotNull(enrollmentGroup);
            Assert.AreEqual(SampleEnrollmentGroupId, enrollmentGroup.EnrollmentGroupId);
            Assert.IsTrue(enrollmentGroup.Attestation is X509Attestation);
            Assert.AreEqual(SampleEtag, enrollmentGroup.ETag);
        }
    }
}
