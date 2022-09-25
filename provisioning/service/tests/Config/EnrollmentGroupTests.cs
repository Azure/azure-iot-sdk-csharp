// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        private DateTime SampleCreateDateTimeUTC = new DateTime(2017, 11, 14, 12, 34, 18, 123, DateTimeKind.Utc);
        private const string SampleLastUpdatedDateTimeUTCString = "2017-11-14T12:34:18.321Z";
        private DateTime SampleLastUpdatedDateTimeUTC = new DateTime(2017, 11, 14, 12, 34, 18, 321, DateTimeKind.Utc);
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
        private X509Attestation SampleX509RootAttestation = X509Attestation.CreateFromRootCertificates(SamplePublicKeyCertificateString);
        private X509Attestation SampleX509ClientAttestation = X509Attestation.CreateFromClientCertificates(SamplePublicKeyCertificateString);
        private const string SampleEndorsementKey =
            "AToAAQALAAMAsgAgg3GXZ0SEs/gakMyNRqXXJP1S124GUgtk8qHaGzMUaaoABgCAAEMAEAgAAAAAAAEAxsj" +
            "2gUScTk1UjuioeTlfGYZrrimExB+bScH75adUMRIi2UOMxG1kw4y+9RW/IVoMl4e620VxZad0ARX2gUqVjY" +
            "O7KPVt3dyKhZS3dkcvfBisBhP1XH9B33VqHG9SHnbnQXdBUaCgKAfxome8UmBKfe+naTsE5fkvjb/do3/dD" +
            "6l4sGBwFCnKRdln4XpM03zLpoHFao8zOwt8l/uP3qUIxmCYv9A7m69Ms+5/pCkTu/rK4mRDsfhZ0QLfbzVI" +
            "6zQFOKF/rwsfBtFeWlWtcuJMKlXdD8TXWElTzgh7JS4qhFzreL0c1mI0GCj+Aws0usZh7dLIVPnlgZcBhgy" +
            "1SSDQMQ==";
        private TpmAttestation SampleTpmAttestation = new TpmAttestation(SampleEndorsementKey);
        private string SampleEnrollmentGroupJson =
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

        /* SRS_ENROLLMENT_GROUP_21_001: [The constructor shall store the provided parameters.] */
        [TestMethod]
        public void EnrollmentGroupConstructorSucceed()
        {
            // arrange - act
            var individualEnrollment = new EnrollmentGroup(SampleEnrollmentGroupId, SampleX509RootAttestation);

            // assert
            Assert.AreEqual(SampleEnrollmentGroupId, individualEnrollment.EnrollmentGroupId);
            Assert.AreEqual(SamplePublicKeyCertificateString, ((X509Attestation)individualEnrollment.Attestation).RootCertificates.Primary.Certificate);
        }

        /* SRS_ENROLLMENT_GROUP_21_002: [The constructor shall throws ArgumentException if one of the provided parameters is null.] */
        [TestMethod]
        public void EnrollmentGroupConstructorThrowsOnInvalidParameters()
        {
            // arrange - act - assert
            TestAssert.Throws<ArgumentException>(() => new EnrollmentGroup(SampleEnrollmentGroupId, null));
            TestAssert.Throws<ArgumentException>(() => new EnrollmentGroup(SampleEnrollmentGroupId, SampleTpmAttestation));
            TestAssert.Throws<ArgumentException>(() => new EnrollmentGroup(SampleEnrollmentGroupId, SampleX509ClientAttestation));
        }

        [TestMethod]
        public void EnrollmentGroupConstructorJSONThrowsOnNonAttestation()
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
            TestAssert.Throws<DeviceProvisioningServiceException>(() => Newtonsoft.Json.JsonConvert.DeserializeObject<EnrollmentGroup>(invalidJson));
        }

        /* SRS_ENROLLMENT_GROUP_21_004: [The constructor shall store all parameters in the JSON.] */
        [TestMethod]
        public void EnrollmentGroupConstructorJSONSucceed()
        {
            // arrange
            EnrollmentGroup enrollmentGroup = Newtonsoft.Json.JsonConvert.DeserializeObject<EnrollmentGroup>(SampleEnrollmentGroupJson);

            // act - assert
            Assert.IsNotNull(enrollmentGroup);
            Assert.AreEqual(SampleEnrollmentGroupId, enrollmentGroup.EnrollmentGroupId);
            Assert.IsTrue(enrollmentGroup.Attestation is X509Attestation);
            Assert.AreEqual(SampleIotHubHostName, enrollmentGroup.IotHubHostName);
            Assert.IsNotNull(enrollmentGroup.InitialTwinState);
            Assert.AreEqual(SampleProvisioningStatus, enrollmentGroup.ProvisioningStatus);
            Assert.AreEqual(SampleCreateDateTimeUTC, enrollmentGroup.CreatedDateTimeUtc);
            Assert.AreEqual(SampleLastUpdatedDateTimeUTC, enrollmentGroup.LastUpdatedDateTimeUtc);
            Assert.AreEqual(SampleEtag, enrollmentGroup.ETag);
        }

        [TestMethod]
        public void EnrollmentGroupConstructorJSONSucceedOnMinimumJSON()
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
            EnrollmentGroup enrollmentGroup = Newtonsoft.Json.JsonConvert.DeserializeObject<EnrollmentGroup>(minJson);

            // act - assert
            Assert.IsNotNull(enrollmentGroup);
            Assert.AreEqual(SampleEnrollmentGroupId, enrollmentGroup.EnrollmentGroupId);
            Assert.IsTrue(enrollmentGroup.Attestation is X509Attestation);
            Assert.AreEqual(SampleEtag, enrollmentGroup.ETag);
        }
    }
}
