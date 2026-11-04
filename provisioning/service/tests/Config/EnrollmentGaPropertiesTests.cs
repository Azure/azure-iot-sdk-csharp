// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    // GA-only contract: stable/2026-11-01/service.json, IndividualEnrollment and EnrollmentGroup.
    // These optional strings must retain their exact camel-case wire names and values.
    [TestClass]
    [TestCategory("Unit")]
    public class EnrollmentGaPropertiesTests
    {
        [DataTestMethod]
        [DataRow(typeof(IndividualEnrollment), "NamespaceName", "namespaceName", "test-namespace")]
        [DataRow(typeof(IndividualEnrollment), "CertificateAuthorityName", "certificateAuthorityName", "Test-Authority")]
        [DataRow(typeof(IndividualEnrollment), "CertificatePolicyName", "certificatePolicyName", "Test-Policy")]
        [DataRow(typeof(EnrollmentGroup), "NamespaceName", "namespaceName", "test-namespace")]
        [DataRow(typeof(EnrollmentGroup), "CertificateAuthorityName", "certificateAuthorityName", "Test-Authority")]
        [DataRow(typeof(EnrollmentGroup), "CertificatePolicyName", "certificatePolicyName", "Test-Policy")]
        public void SerializeObject_WithGaProperty_EmitsExactWireNameAndValue(
            Type enrollmentType, string propertyName, string wireName, string value)
        {
            // Arrange
            object enrollment = CreateEnrollment(enrollmentType);
            PropertyInfo property = GetGaProperty(enrollmentType, propertyName);
            property.SetValue(enrollment, value);

            // Act
            JObject json = JObject.Parse(JsonConvert.SerializeObject(
                enrollment, JsonSerializerSettingsInitializer.GetJsonSerializerSettings()));

            // Assert
            Assert.AreEqual(value, (string)json[wireName]);
        }

        [DataTestMethod]
        [DataRow(typeof(IndividualEnrollment), "NamespaceName", "namespaceName", "test-namespace")]
        [DataRow(typeof(IndividualEnrollment), "CertificateAuthorityName", "certificateAuthorityName", "Test-Authority")]
        [DataRow(typeof(IndividualEnrollment), "CertificatePolicyName", "certificatePolicyName", "Test-Policy")]
        [DataRow(typeof(EnrollmentGroup), "NamespaceName", "namespaceName", "test-namespace")]
        [DataRow(typeof(EnrollmentGroup), "CertificateAuthorityName", "certificateAuthorityName", "Test-Authority")]
        [DataRow(typeof(EnrollmentGroup), "CertificatePolicyName", "certificatePolicyName", "Test-Policy")]
        public void DeserializeObject_WithGaProperty_PopulatesPublicStringProperty(
            Type enrollmentType, string propertyName, string wireName, string value)
        {
            // Arrange
            JObject json = CreateEnrollmentJson(enrollmentType);
            json[wireName] = value;
            PropertyInfo property = GetGaProperty(enrollmentType, propertyName);

            // Act
            object enrollment = JsonConvert.DeserializeObject(
                json.ToString(), enrollmentType, JsonSerializerSettingsInitializer.GetJsonSerializerSettings());

            // Assert
            Assert.AreEqual(value, property.GetValue(enrollment));
        }

        [DataTestMethod]
        [DataRow(typeof(IndividualEnrollment), "namespaceName", "test-namespace")]
        [DataRow(typeof(IndividualEnrollment), "certificateAuthorityName", "Test-Authority")]
        [DataRow(typeof(IndividualEnrollment), "certificatePolicyName", "Test-Policy")]
        [DataRow(typeof(EnrollmentGroup), "namespaceName", "test-namespace")]
        [DataRow(typeof(EnrollmentGroup), "certificateAuthorityName", "Test-Authority")]
        [DataRow(typeof(EnrollmentGroup), "certificatePolicyName", "Test-Policy")]
        public void SerializeObject_AfterDeserializingGaProperty_PreservesWireValue(
            Type enrollmentType, string wireName, string value)
        {
            // Arrange
            JObject response = CreateEnrollmentJson(enrollmentType);
            response[wireName] = value;
            object enrollment = JsonConvert.DeserializeObject(
                response.ToString(), enrollmentType, JsonSerializerSettingsInitializer.GetJsonSerializerSettings());

            // Act
            JObject roundTrip = JObject.Parse(JsonConvert.SerializeObject(
                enrollment, JsonSerializerSettingsInitializer.GetJsonSerializerSettings()));

            // Assert
            Assert.AreEqual(value, (string)roundTrip[wireName]);
        }

        [DataTestMethod]
        [DataRow(typeof(IndividualEnrollment), "NamespaceName")]
        [DataRow(typeof(IndividualEnrollment), "CertificateAuthorityName")]
        [DataRow(typeof(IndividualEnrollment), "CertificatePolicyName")]
        [DataRow(typeof(EnrollmentGroup), "NamespaceName")]
        [DataRow(typeof(EnrollmentGroup), "CertificateAuthorityName")]
        [DataRow(typeof(EnrollmentGroup), "CertificatePolicyName")]
        public void DeserializeObject_WithGaPropertyOmitted_LeavesPropertyNull(
            Type enrollmentType, string propertyName)
        {
            // Arrange
            JObject json = CreateEnrollmentJson(enrollmentType);
            PropertyInfo property = GetGaProperty(enrollmentType, propertyName);

            // Act
            object enrollment = JsonConvert.DeserializeObject(
                json.ToString(), enrollmentType, JsonSerializerSettingsInitializer.GetJsonSerializerSettings());

            // Assert
            Assert.IsNull(property.GetValue(enrollment));
        }

        [DataTestMethod]
        [DataRow(typeof(IndividualEnrollment), "NamespaceName", "namespaceName")]
        [DataRow(typeof(IndividualEnrollment), "CertificateAuthorityName", "certificateAuthorityName")]
        [DataRow(typeof(IndividualEnrollment), "CertificatePolicyName", "certificatePolicyName")]
        [DataRow(typeof(EnrollmentGroup), "NamespaceName", "namespaceName")]
        [DataRow(typeof(EnrollmentGroup), "CertificateAuthorityName", "certificateAuthorityName")]
        [DataRow(typeof(EnrollmentGroup), "CertificatePolicyName", "certificatePolicyName")]
        public void SerializeObject_WithGaPropertyUnset_OmitsOptionalWireProperty(
            Type enrollmentType, string propertyName, string wireName)
        {
            // Arrange
            object enrollment = CreateEnrollment(enrollmentType);
            PropertyInfo property = GetGaProperty(enrollmentType, propertyName);
            property.SetValue(enrollment, null);

            // Act
            JObject json = JObject.Parse(JsonConvert.SerializeObject(
                enrollment, JsonSerializerSettingsInitializer.GetJsonSerializerSettings()));

            // Assert
            Assert.IsNull(json.Property(wireName));
        }

        private static object CreateEnrollment(Type enrollmentType)
        {
            var attestation = new SymmetricKeyAttestation(
                "dGVzdC1wcmltYXJ5LWtleQ==", "dGVzdC1zZWNvbmRhcnkta2V5");
            return enrollmentType == typeof(IndividualEnrollment)
                ? (object)new IndividualEnrollment("test-registration", attestation)
                : new EnrollmentGroup("test-group", attestation);
        }

        private static JObject CreateEnrollmentJson(Type enrollmentType)
        {
            // Fixed, minimal service payload, independent of the SDK's serializer.
            string idName = enrollmentType == typeof(IndividualEnrollment) ? "registrationId" : "enrollmentGroupId";
            return new JObject
            {
                [idName] = "test-enrollment",
                ["attestation"] = new JObject
                {
                    ["type"] = "symmetricKey",
                    ["symmetricKey"] = new JObject
                    {
                        ["primaryKey"] = "dGVzdC1wcmltYXJ5LWtleQ==",
                        ["secondaryKey"] = "dGVzdC1zZWNvbmRhcnkta2V5",
                    },
                },
            };
        }

        private static PropertyInfo GetGaProperty(Type enrollmentType, string propertyName)
        {
            // Reflection keeps the red suite executable before the public GA properties exist.
            PropertyInfo property = enrollmentType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(property, $"{enrollmentType.Name}.{propertyName} must expose the GA property.");
            Assert.AreEqual(typeof(string), property.PropertyType);
            Assert.IsTrue(property.GetMethod?.IsPublic == true && property.SetMethod?.IsPublic == true,
                $"{enrollmentType.Name}.{propertyName} must have public get and set accessors.");
            return property;
        }
    }
}
