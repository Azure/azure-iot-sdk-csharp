// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    /// <summary>
    /// Tests for the single-version DPS preview service package pinned to 2026-11-02-preview.
    /// </summary>
    /// <remarks>
    /// This package/branch supports only the preview service API version 2026-11-02-preview,
    /// pinned unconditionally. There is no multi-version surface:
    /// - No <c>ServiceVersion</c> enum and no <c>ProvisioningServiceClientOptions</c> type.
    /// - The mature public factory signatures are preserved and issue api-version=2026-11-02-preview.
    /// - Preview fields (namespaceName, certificateAuthorityName, certificatePolicyName, deviceTypeRefs,
    ///   read-only connectionProfile) are ordinary fields and serialize by default when populated.
    /// - credentialPolicyName is unsupported by the service and must never appear.
    /// - The bulk wire value updateIfMatchETag must be preserved exactly.
    /// </remarks>
    [TestClass]
    [TestCategory("Unit")]
    public class Dps2026ApiVersionTests
    {
        private const string ExpectedApiVersionQueryString = "api-version=2026-11-02-preview";

        private const string SampleRegistrationId = "valid-registration-id";
        private const string SampleEnrollmentGroupId = "valid-enrollment-group-id";
        private const string SampleNamespaceName = "sample-namespace";
        private const string SampleCertificateAuthorityName = "sample-ca-name";
        private const string SampleCertificatePolicyName = "sample-cert-policy";
        private const string SampleDeviceTypeRef = "sample-device-type";

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

        private static IndividualEnrollment CreateSampleIndividualEnrollment()
        {
            X509Attestation attestation = X509Attestation.CreateFromClientCertificates(SamplePublicKeyCertificateString);
            return new IndividualEnrollment(SampleRegistrationId, attestation);
        }

        private static EnrollmentGroup CreateSampleEnrollmentGroup()
        {
            X509Attestation attestation = X509Attestation.CreateFromRootCertificates(SamplePublicKeyCertificateString);
            return new EnrollmentGroup(SampleEnrollmentGroupId, attestation);
        }

        // ---------------------------------------------------------------------
        // Pinned api-version: every request path issues 2026-11-02-preview.
        // ---------------------------------------------------------------------

        [TestMethod]
        public void SdkUtils_ApiVersionQueryString_PinnedToPreview()
        {
            Assert.AreEqual(ExpectedApiVersionQueryString, SdkUtils.ApiVersionQueryString);
        }

        // ---------------------------------------------------------------------
        // Multi-version surface is removed: no ServiceVersion, no options type.
        // ---------------------------------------------------------------------

        [TestMethod]
        public void MultiVersion_PublicTypes_DoNotExist()
        {
            System.Reflection.Assembly assembly = typeof(IndividualEnrollment).Assembly;
            Assert.IsNull(
                assembly.GetType("Microsoft.Azure.Devices.Provisioning.Service.ServiceVersion"),
                "The ServiceVersion enum must not exist in a single-version preview package.");
            Assert.IsNull(
                assembly.GetType("Microsoft.Azure.Devices.Provisioning.Service.ProvisioningServiceClientOptions"),
                "ProvisioningServiceClientOptions must not exist in a single-version preview package.");
            Assert.IsNull(
                assembly.GetType("Microsoft.Azure.Devices.Provisioning.Service.PreviewApiOnlyAttribute"),
                "PreviewApiOnlyAttribute must not exist; there is no version-gated serialization.");
        }

        // ---------------------------------------------------------------------
        // Mature public factory signatures are preserved.
        // ---------------------------------------------------------------------

        [TestMethod]
        public void ProvisioningServiceClient_LegacyFactorySignatures_Preserved()
        {
            System.Type clientType = typeof(ProvisioningServiceClient);

            Assert.IsNotNull(
                clientType.GetMethod("CreateFromConnectionString", new[] { typeof(string) }),
                "CreateFromConnectionString(string) overload must be preserved.");
            Assert.IsNotNull(
                clientType.GetMethod("CreateFromConnectionString", new[] { typeof(string), typeof(HttpTransportSettings) }),
                "CreateFromConnectionString(string, HttpTransportSettings) overload must be preserved.");
        }

        // ---------------------------------------------------------------------
        // Preview fields round-trip and serialize by default (no gating).
        // ---------------------------------------------------------------------

        [TestMethod]
        public void IndividualEnrollment_PreviewFields_RoundTrip()
        {
            // arrange
            IndividualEnrollment enrollment = CreateSampleIndividualEnrollment();
            enrollment.NamespaceName = SampleNamespaceName;
            enrollment.CertificateAuthorityName = SampleCertificateAuthorityName;
            enrollment.CertificatePolicyName = SampleCertificatePolicyName;
            enrollment.DeviceTypeRefs = new List<string> { SampleDeviceTypeRef };

            // act
            string json = JsonConvert.SerializeObject(
                enrollment,
                JsonSerializerSettingsInitializer.GetJsonSerializerSettings());
            IndividualEnrollment roundTripped = JsonConvert.DeserializeObject<IndividualEnrollment>(json);

            // assert
            Assert.IsTrue(json.Contains("namespaceName"));
            Assert.IsTrue(json.Contains("certificateAuthorityName"));
            Assert.IsTrue(json.Contains("certificatePolicyName"));
            Assert.IsTrue(json.Contains("deviceTypeRefs"));
            Assert.AreEqual(SampleNamespaceName, roundTripped.NamespaceName);
            Assert.AreEqual(SampleCertificateAuthorityName, roundTripped.CertificateAuthorityName);
            Assert.AreEqual(SampleCertificatePolicyName, roundTripped.CertificatePolicyName);
            Assert.IsNotNull(roundTripped.DeviceTypeRefs);
            Assert.AreEqual(1, roundTripped.DeviceTypeRefs.Count);
            Assert.AreEqual(SampleDeviceTypeRef, roundTripped.DeviceTypeRefs.First());
        }

        [TestMethod]
        public void EnrollmentGroup_PreviewFields_RoundTrip()
        {
            // arrange
            EnrollmentGroup enrollment = CreateSampleEnrollmentGroup();
            enrollment.NamespaceName = SampleNamespaceName;
            enrollment.CertificateAuthorityName = SampleCertificateAuthorityName;
            enrollment.CertificatePolicyName = SampleCertificatePolicyName;
            enrollment.DeviceTypeRefs = new List<string> { SampleDeviceTypeRef };

            // act
            string json = JsonConvert.SerializeObject(
                enrollment,
                JsonSerializerSettingsInitializer.GetJsonSerializerSettings());
            EnrollmentGroup roundTripped = JsonConvert.DeserializeObject<EnrollmentGroup>(json);

            // assert
            Assert.IsTrue(json.Contains("namespaceName"));
            Assert.IsTrue(json.Contains("certificateAuthorityName"));
            Assert.IsTrue(json.Contains("certificatePolicyName"));
            Assert.IsTrue(json.Contains("deviceTypeRefs"));
            Assert.AreEqual(SampleNamespaceName, roundTripped.NamespaceName);
            Assert.AreEqual(SampleCertificateAuthorityName, roundTripped.CertificateAuthorityName);
            Assert.AreEqual(SampleCertificatePolicyName, roundTripped.CertificatePolicyName);
            Assert.IsNotNull(roundTripped.DeviceTypeRefs);
            Assert.AreEqual(1, roundTripped.DeviceTypeRefs.Count);
            Assert.AreEqual(SampleDeviceTypeRef, roundTripped.DeviceTypeRefs.First());
        }

        [TestMethod]
        public void IndividualEnrollment_DeviceTypeRefs_SerializedByDefault()
        {
            // arrange
            IndividualEnrollment enrollment = CreateSampleIndividualEnrollment();
            enrollment.DeviceTypeRefs = new List<string> { SampleDeviceTypeRef };

            // act - the default settings must serialize deviceTypeRefs (no version gating).
            string json = JsonConvert.SerializeObject(
                enrollment,
                JsonSerializerSettingsInitializer.GetJsonSerializerSettings());

            // assert
            Assert.IsTrue(
                json.Contains("deviceTypeRefs"),
                "deviceTypeRefs must serialize by default in the single-version preview package.");
        }

        [TestMethod]
        public void EnrollmentGroup_DeviceTypeRefs_SerializedByDefault()
        {
            // arrange
            EnrollmentGroup enrollment = CreateSampleEnrollmentGroup();
            enrollment.DeviceTypeRefs = new List<string> { SampleDeviceTypeRef };

            // act
            string json = JsonConvert.SerializeObject(
                enrollment,
                JsonSerializerSettingsInitializer.GetJsonSerializerSettings());

            // assert
            Assert.IsTrue(
                json.Contains("deviceTypeRefs"),
                "deviceTypeRefs must serialize by default in the single-version preview package.");
        }

        [TestMethod]
        public void IndividualEnrollment_ToString_IncludesDeviceTypeRefs()
        {
            // arrange
            IndividualEnrollment enrollment = CreateSampleIndividualEnrollment();
            enrollment.DeviceTypeRefs = new List<string> { SampleDeviceTypeRef };

            // act - ToString() uses the single default serializer; deviceTypeRefs is an ordinary field.
            string text = enrollment.ToString();

            // assert
            Assert.IsTrue(
                text.Contains("deviceTypeRefs"),
                "ToString() must include deviceTypeRefs; there is no preview gating in this package.");
        }

        [TestMethod]
        public void EnrollmentGroup_ToString_IncludesDeviceTypeRefs()
        {
            // arrange
            EnrollmentGroup enrollment = CreateSampleEnrollmentGroup();
            enrollment.DeviceTypeRefs = new List<string> { SampleDeviceTypeRef };

            // act
            string text = enrollment.ToString();

            // assert
            Assert.IsTrue(
                text.Contains("deviceTypeRefs"),
                "ToString() must include deviceTypeRefs; there is no preview gating in this package.");
        }

        // ---------------------------------------------------------------------
        // connectionProfile on DeviceRegistrationState (response-only, extensible string).
        // ---------------------------------------------------------------------

        [TestMethod]
        public void DeviceRegistrationState_ConnectionProfile_KnownValueDeserializes()
        {
            // arrange
            string json =
                "{\n" +
                "   \"registrationId\":\"" + SampleRegistrationId + "\",\n" +
                "   \"status\":\"assigned\",\n" +
                "   \"connectionProfile\":\"mqttV5\"\n" +
                "}";

            // act
            DeviceRegistrationState state = JsonConvert.DeserializeObject<DeviceRegistrationState>(json);

            // assert
            Assert.IsNotNull(state);
            Assert.AreEqual("mqttV5", state.ConnectionProfile);
        }

        [TestMethod]
        public void DeviceRegistrationState_ConnectionProfile_UnknownValueTolerated()
        {
            // arrange - an unknown future value must be tolerated and surfaced as-is
            string json =
                "{\n" +
                "   \"registrationId\":\"" + SampleRegistrationId + "\",\n" +
                "   \"status\":\"assigned\",\n" +
                "   \"connectionProfile\":\"someFutureProfile\"\n" +
                "}";

            // act
            DeviceRegistrationState state = JsonConvert.DeserializeObject<DeviceRegistrationState>(json);

            // assert
            Assert.IsNotNull(state);
            Assert.AreEqual("someFutureProfile", state.ConnectionProfile);
        }

        [TestMethod]
        public void DeviceRegistrationState_ConnectionProfile_AbsentIsNull()
        {
            // arrange - when the service does not emit connectionProfile it resolves to classic semantically;
            // the SDK surfaces null rather than synthesizing "classic".
            string json =
                "{\n" +
                "   \"registrationId\":\"" + SampleRegistrationId + "\",\n" +
                "   \"status\":\"assigned\"\n" +
                "}";

            // act
            DeviceRegistrationState state = JsonConvert.DeserializeObject<DeviceRegistrationState>(json);

            // assert
            Assert.IsNotNull(state);
            Assert.IsNull(state.ConnectionProfile);
        }

        [TestMethod]
        public void DeviceRegistrationState_ConnectionProfile_IsReadOnly()
        {
            // The response-only property must not expose a public setter.
            System.Reflection.PropertyInfo property =
                typeof(DeviceRegistrationState).GetProperty("ConnectionProfile");
            Assert.IsNotNull(property, "ConnectionProfile property must exist.");
            Assert.IsNull(
                property.GetSetMethod(),
                "ConnectionProfile must be response-only (no public setter).");
        }

        // ---------------------------------------------------------------------
        // Bulk operation: preserve exact wire value updateIfMatchETag; deviceTypeRefs serializes.
        // ---------------------------------------------------------------------

        [TestMethod]
        public void BulkEnrollmentOperation_UpdateIfMatchETag_WireValuePreserved()
        {
            // arrange
            var enrollments = new List<IndividualEnrollment> { CreateSampleIndividualEnrollment() };

            // act
            string json = BulkEnrollmentOperation.ToJson(BulkOperationMode.UpdateIfMatchETag, enrollments);

            // assert
            Assert.IsTrue(json.Contains("updateIfMatchETag"), "Bulk wire value updateIfMatchETag must be preserved exactly.");
        }

        [TestMethod]
        public void BulkEnrollmentOperation_DeviceTypeRefs_SerializedByDefault()
        {
            // arrange
            IndividualEnrollment enrollment = CreateSampleIndividualEnrollment();
            enrollment.DeviceTypeRefs = new List<string> { SampleDeviceTypeRef };
            var enrollments = new List<IndividualEnrollment> { enrollment };

            // act - single (version-less) overload; deviceTypeRefs is an ordinary field.
            string json = BulkEnrollmentOperation.ToJson(BulkOperationMode.Create, enrollments);

            // assert
            Assert.IsTrue(
                json.Contains("deviceTypeRefs"),
                "Bulk serialization must emit deviceTypeRefs by default in the preview package.");
        }

        // ---------------------------------------------------------------------
        // Negative: credentialPolicyName must never appear.
        // ---------------------------------------------------------------------

        [TestMethod]
        public void IndividualEnrollment_CredentialPolicyName_NeverSerializes()
        {
            // arrange
            IndividualEnrollment enrollment = CreateSampleIndividualEnrollment();
            enrollment.NamespaceName = SampleNamespaceName;
            enrollment.CertificateAuthorityName = SampleCertificateAuthorityName;
            enrollment.CertificatePolicyName = SampleCertificatePolicyName;
            enrollment.DeviceTypeRefs = new List<string> { SampleDeviceTypeRef };

            // act
            string json = JsonConvert.SerializeObject(
                enrollment,
                JsonSerializerSettingsInitializer.GetJsonSerializerSettings());

            // assert
            Assert.IsFalse(json.Contains("credentialPolicyName"));
        }

        [TestMethod]
        public void EnrollmentModels_CredentialPolicyName_PropertyDoesNotExist()
        {
            // assert - the property must not exist on either model
            Assert.IsNull(typeof(IndividualEnrollment).GetProperty("CredentialPolicyName"));
            Assert.IsNull(typeof(EnrollmentGroup).GetProperty("CredentialPolicyName"));
        }
    }
}
