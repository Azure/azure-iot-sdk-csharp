// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    /// <summary>
    /// Tests for the DPS service API versions 2026-11-01 (GA) and 2026-11-02-preview.
    /// </summary>
    /// <remarks>
    /// These tests encode the spec for the two new service API versions:
    /// - GA (2026-11-01) adds optional namespaceName, certificateAuthorityName and certificatePolicyName
    ///   to both enrollment models.
    /// - Preview (2026-11-02-preview) additionally adds optional deviceTypeRefs (max one) to both enrollment
    ///   models and read-only connectionProfile to the registration state.
    /// - deviceTypeRefs must be serialized only for the preview API version (version-gated wire behavior).
    /// - credentialPolicyName is unsupported by the service and must never appear in either target version.
    /// - The bulk wire value updateIfMatchETag must be preserved exactly.
    /// </remarks>
    [TestClass]
    [TestCategory("Unit")]
    public class Dps2026ApiVersionTests
    {
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
        // GA (2026-11-01) new fields: namespaceName, certificateAuthorityName, certificatePolicyName
        // ---------------------------------------------------------------------

        [TestMethod]
        public void IndividualEnrollment_GaFields_RoundTrip()
        {
            // arrange
            IndividualEnrollment enrollment = CreateSampleIndividualEnrollment();
            enrollment.NamespaceName = SampleNamespaceName;
            enrollment.CertificateAuthorityName = SampleCertificateAuthorityName;
            enrollment.CertificatePolicyName = SampleCertificatePolicyName;

            // act
            string json = JsonConvert.SerializeObject(
                enrollment,
                JsonSerializerSettingsInitializer.GetJsonSerializerSettings(ServiceVersion.V2026_11_01));
            IndividualEnrollment roundTripped = JsonConvert.DeserializeObject<IndividualEnrollment>(json);

            // assert
            Assert.IsTrue(json.Contains("namespaceName"));
            Assert.IsTrue(json.Contains("certificateAuthorityName"));
            Assert.IsTrue(json.Contains("certificatePolicyName"));
            Assert.AreEqual(SampleNamespaceName, roundTripped.NamespaceName);
            Assert.AreEqual(SampleCertificateAuthorityName, roundTripped.CertificateAuthorityName);
            Assert.AreEqual(SampleCertificatePolicyName, roundTripped.CertificatePolicyName);
        }

        [TestMethod]
        public void EnrollmentGroup_GaFields_RoundTrip()
        {
            // arrange
            EnrollmentGroup enrollment = CreateSampleEnrollmentGroup();
            enrollment.NamespaceName = SampleNamespaceName;
            enrollment.CertificateAuthorityName = SampleCertificateAuthorityName;
            enrollment.CertificatePolicyName = SampleCertificatePolicyName;

            // act
            string json = JsonConvert.SerializeObject(
                enrollment,
                JsonSerializerSettingsInitializer.GetJsonSerializerSettings(ServiceVersion.V2026_11_01));
            EnrollmentGroup roundTripped = JsonConvert.DeserializeObject<EnrollmentGroup>(json);

            // assert
            Assert.IsTrue(json.Contains("namespaceName"));
            Assert.IsTrue(json.Contains("certificateAuthorityName"));
            Assert.IsTrue(json.Contains("certificatePolicyName"));
            Assert.AreEqual(SampleNamespaceName, roundTripped.NamespaceName);
            Assert.AreEqual(SampleCertificateAuthorityName, roundTripped.CertificateAuthorityName);
            Assert.AreEqual(SampleCertificatePolicyName, roundTripped.CertificatePolicyName);
        }

        // ---------------------------------------------------------------------
        // Preview (2026-11-02-preview) deviceTypeRefs round-trip
        // ---------------------------------------------------------------------

        [TestMethod]
        public void IndividualEnrollment_DeviceTypeRefs_RoundTripUnderPreview()
        {
            // arrange
            IndividualEnrollment enrollment = CreateSampleIndividualEnrollment();
            enrollment.DeviceTypeRefs = new List<string> { SampleDeviceTypeRef };

            // act
            string json = JsonConvert.SerializeObject(
                enrollment,
                JsonSerializerSettingsInitializer.GetJsonSerializerSettings(ServiceVersion.V2026_11_02_Preview));
            IndividualEnrollment roundTripped = JsonConvert.DeserializeObject<IndividualEnrollment>(json);

            // assert
            Assert.IsTrue(json.Contains("deviceTypeRefs"));
            Assert.IsNotNull(roundTripped.DeviceTypeRefs);
            Assert.AreEqual(1, roundTripped.DeviceTypeRefs.Count);
            Assert.AreEqual(SampleDeviceTypeRef, roundTripped.DeviceTypeRefs.First());
        }

        [TestMethod]
        public void EnrollmentGroup_DeviceTypeRefs_RoundTripUnderPreview()
        {
            // arrange
            EnrollmentGroup enrollment = CreateSampleEnrollmentGroup();
            enrollment.DeviceTypeRefs = new List<string> { SampleDeviceTypeRef };

            // act
            string json = JsonConvert.SerializeObject(
                enrollment,
                JsonSerializerSettingsInitializer.GetJsonSerializerSettings(ServiceVersion.V2026_11_02_Preview));
            EnrollmentGroup roundTripped = JsonConvert.DeserializeObject<EnrollmentGroup>(json);

            // assert
            Assert.IsTrue(json.Contains("deviceTypeRefs"));
            Assert.IsNotNull(roundTripped.DeviceTypeRefs);
            Assert.AreEqual(1, roundTripped.DeviceTypeRefs.Count);
            Assert.AreEqual(SampleDeviceTypeRef, roundTripped.DeviceTypeRefs.First());
        }

        // ---------------------------------------------------------------------
        // Version-gated wire behavior: deviceTypeRefs must NOT serialize under GA
        // ---------------------------------------------------------------------

        [TestMethod]
        public void IndividualEnrollment_DeviceTypeRefs_NotSerializedUnderGa()
        {
            // arrange
            IndividualEnrollment enrollment = CreateSampleIndividualEnrollment();
            enrollment.DeviceTypeRefs = new List<string> { SampleDeviceTypeRef };

            // act
            string gaJson = JsonConvert.SerializeObject(
                enrollment,
                JsonSerializerSettingsInitializer.GetJsonSerializerSettings(ServiceVersion.V2026_11_01));
            string previewJson = JsonConvert.SerializeObject(
                enrollment,
                JsonSerializerSettingsInitializer.GetJsonSerializerSettings(ServiceVersion.V2026_11_02_Preview));

            // assert
            Assert.IsFalse(gaJson.Contains("deviceTypeRefs"), "GA (2026-11-01) must not serialize preview-only deviceTypeRefs.");
            Assert.IsTrue(previewJson.Contains("deviceTypeRefs"), "Preview (2026-11-02-preview) must serialize deviceTypeRefs.");
        }

        [TestMethod]
        public void EnrollmentGroup_DeviceTypeRefs_NotSerializedUnderGa()
        {
            // arrange
            EnrollmentGroup enrollment = CreateSampleEnrollmentGroup();
            enrollment.DeviceTypeRefs = new List<string> { SampleDeviceTypeRef };

            // act
            string gaJson = JsonConvert.SerializeObject(
                enrollment,
                JsonSerializerSettingsInitializer.GetJsonSerializerSettings(ServiceVersion.V2026_11_01));
            string previewJson = JsonConvert.SerializeObject(
                enrollment,
                JsonSerializerSettingsInitializer.GetJsonSerializerSettings(ServiceVersion.V2026_11_02_Preview));

            // assert
            Assert.IsFalse(gaJson.Contains("deviceTypeRefs"), "GA (2026-11-01) must not serialize preview-only deviceTypeRefs.");
            Assert.IsTrue(previewJson.Contains("deviceTypeRefs"), "Preview (2026-11-02-preview) must serialize deviceTypeRefs.");
        }

        [TestMethod]
        public void IndividualEnrollment_ToString_DoesNotLeakPreviewOnlyDeviceTypeRefs()
        {
            // arrange
            IndividualEnrollment enrollment = CreateSampleIndividualEnrollment();
            enrollment.DeviceTypeRefs = new List<string> { SampleDeviceTypeRef };

            // act - ToString() is version-agnostic diagnostic output
            string text = enrollment.ToString();

            // assert - preview-only property must not leak into the default diagnostic string
            Assert.IsFalse(text.Contains("deviceTypeRefs"), "ToString() must not emit preview-only deviceTypeRefs.");
        }

        [TestMethod]
        public void EnrollmentGroup_ToString_DoesNotLeakPreviewOnlyDeviceTypeRefs()
        {
            // arrange
            EnrollmentGroup enrollment = CreateSampleEnrollmentGroup();
            enrollment.DeviceTypeRefs = new List<string> { SampleDeviceTypeRef };

            // act - ToString() is version-agnostic diagnostic output
            string text = enrollment.ToString();

            // assert - preview-only property must not leak into the default diagnostic string
            Assert.IsFalse(text.Contains("deviceTypeRefs"), "ToString() must not emit preview-only deviceTypeRefs.");
        }

        [TestMethod]
        public void IndividualEnrollment_GaFields_SerializedUnderBothVersions()
        {
            // arrange
            IndividualEnrollment enrollment = CreateSampleIndividualEnrollment();
            enrollment.NamespaceName = SampleNamespaceName;

            // act
            string gaJson = JsonConvert.SerializeObject(
                enrollment,
                JsonSerializerSettingsInitializer.GetJsonSerializerSettings(ServiceVersion.V2026_11_01));
            string previewJson = JsonConvert.SerializeObject(
                enrollment,
                JsonSerializerSettingsInitializer.GetJsonSerializerSettings(ServiceVersion.V2026_11_02_Preview));

            // assert - GA fields are common to both target versions
            Assert.IsTrue(gaJson.Contains("namespaceName"));
            Assert.IsTrue(previewJson.Contains("namespaceName"));
        }

        // ---------------------------------------------------------------------
        // connectionProfile on DeviceRegistrationState (preview, read-only, extensible string)
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

        // ---------------------------------------------------------------------
        // Bulk operation: preserve exact wire value updateIfMatchETag and gate deviceTypeRefs
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
        public void BulkEnrollmentOperation_DeviceTypeRefs_GatedByVersion()
        {
            // arrange
            IndividualEnrollment enrollment = CreateSampleIndividualEnrollment();
            enrollment.DeviceTypeRefs = new List<string> { SampleDeviceTypeRef };
            var enrollments = new List<IndividualEnrollment> { enrollment };

            // act
            string gaJson = BulkEnrollmentOperation.ToJson(BulkOperationMode.Create, enrollments, ServiceVersion.V2026_11_01);
            string previewJson = BulkEnrollmentOperation.ToJson(BulkOperationMode.Create, enrollments, ServiceVersion.V2026_11_02_Preview);

            // assert
            Assert.IsFalse(gaJson.Contains("deviceTypeRefs"), "GA bulk serialization must not emit preview-only deviceTypeRefs.");
            Assert.IsTrue(previewJson.Contains("deviceTypeRefs"), "Preview bulk serialization must emit deviceTypeRefs.");
        }

        // ---------------------------------------------------------------------
        // Negative: credentialPolicyName must never appear in the target-version artifacts
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
            string gaJson = JsonConvert.SerializeObject(
                enrollment,
                JsonSerializerSettingsInitializer.GetJsonSerializerSettings(ServiceVersion.V2026_11_01));
            string previewJson = JsonConvert.SerializeObject(
                enrollment,
                JsonSerializerSettingsInitializer.GetJsonSerializerSettings(ServiceVersion.V2026_11_02_Preview));

            // assert
            Assert.IsFalse(gaJson.Contains("credentialPolicyName"));
            Assert.IsFalse(previewJson.Contains("credentialPolicyName"));
        }

        [TestMethod]
        public void EnrollmentModels_CredentialPolicyName_PropertyDoesNotExist()
        {
            // assert - the property must not exist on either target-version model
            Assert.IsNull(typeof(IndividualEnrollment).GetProperty("CredentialPolicyName"));
            Assert.IsNull(typeof(EnrollmentGroup).GetProperty("CredentialPolicyName"));
        }

        // ---------------------------------------------------------------------
        // api-version query string mapping and options default
        // ---------------------------------------------------------------------

        [TestMethod]
        public void SdkUtils_GetApiVersionQueryString_MapsEachVersion()
        {
            Assert.AreEqual("api-version=2019-03-31", SdkUtils.GetApiVersionQueryString(ServiceVersion.V2019_03_31));
            Assert.AreEqual("api-version=2026-11-01", SdkUtils.GetApiVersionQueryString(ServiceVersion.V2026_11_01));
            Assert.AreEqual("api-version=2026-11-02-preview", SdkUtils.GetApiVersionQueryString(ServiceVersion.V2026_11_02_Preview));
        }

        [TestMethod]
        public void ProvisioningServiceClientOptions_DefaultsToLegacyStable()
        {
            // Both 2026 versions are explicit opt-in only. The default MUST remain the legacy 2019-03-31
            // so existing callers see no wire change on SDK upgrade.
            var options = new ProvisioningServiceClientOptions();
            Assert.AreEqual(ServiceVersion.V2019_03_31, options.Version);
        }

        [TestMethod]
        public void ProvisioningServiceClient_DefaultOptions_UseLegacyApiVersionOnWire()
        {
            // A client built with default options must target api-version=2019-03-31, proving the
            // package upgrade does not silently move existing callers onto a newer api-version.
            var options = new ProvisioningServiceClientOptions();
            Assert.AreEqual(
                "api-version=2019-03-31",
                SdkUtils.GetApiVersionQueryString(options.Version));
        }

        [TestMethod]
        public void BulkEnrollmentOperation_LegacyOverload_SuppressesPreviewFields()
        {
            // The version-less ToJson overload must behave as legacy (2019-03-31) and never emit
            // preview-only fields.
            IndividualEnrollment enrollment = CreateSampleIndividualEnrollment();
            enrollment.DeviceTypeRefs = new List<string> { SampleDeviceTypeRef };
            var enrollments = new List<IndividualEnrollment> { enrollment };

            string legacyJson = BulkEnrollmentOperation.ToJson(BulkOperationMode.Create, enrollments);

            Assert.IsFalse(
                legacyJson.Contains("deviceTypeRefs"),
                "Legacy (version-less) bulk serialization must not emit preview-only deviceTypeRefs.");
        }
    }
}
