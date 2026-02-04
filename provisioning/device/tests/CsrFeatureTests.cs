// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Client.Tests
{
    [TestClass]
    public class CsrFeatureTests
    {
        private const string TestCsrBase64 = "MIHoMIGPAgEAMB0xGzAZBgNVBAMMEnRlc3QtcmVnaXN0cmF0aW9uMFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE";
        
        private static readonly IReadOnlyList<string> TestCertificateChain = new List<string>
        {
            // Mock leaf certificate (base64)
            "MIIBkTCB+wIJAKHBfpegZyst/akKMAoGCCqGSM49BAMCMB0xGzAZBgNVBAMMEnRlc3QtcmVnaXN0cmF0aW9uMB4XDTI0MDIwNDAwMDAwMFoXDTI1MDIwNDAwMDAwMFowHTEbMBkGA1UEAwwSdGVzdC1yZWdpc3RyYXRpb24wWTATBgcqhkjOPQIBBggqhkjOPQMBBwNCAAQ",
            // Mock intermediate CA certificate (base64)
            "MIIBpDCCAUoCCQDU+pQ4pHiQuzAKBggqhkjOPQQDAjBeMQswCQYDVQQGEwJVUzETMBEGA1UECAwKV2FzaGluZ3RvbjEQMA4GA1UEBwwHUmVkbW9uZDEOMAwGA1UECgwFQXp1cmUxGDAWBgNVBAMMD0F6dXJlIElvVCBSb290"
        };

        [TestMethod]
        public void ProvisioningRegistrationAdditionalData_ClientCertificateSigningRequest_CanBeSet()
        {
            // Arrange
            var additionalData = new ProvisioningRegistrationAdditionalData();

            // Act
            additionalData.ClientCertificateSigningRequest = TestCsrBase64;

            // Assert
            Assert.AreEqual(TestCsrBase64, additionalData.ClientCertificateSigningRequest);
        }

        [TestMethod]
        public void ProvisioningRegistrationAdditionalData_ClientCertificateSigningRequest_IsNullByDefault()
        {
            // Arrange & Act
            var additionalData = new ProvisioningRegistrationAdditionalData();

            // Assert
            Assert.IsNull(additionalData.ClientCertificateSigningRequest);
        }

        [TestMethod]
        public void DeviceRegistrationResult_IssuedClientCertificate_CanBeSetViaConstructor()
        {
            // Arrange & Act
            var result = new DeviceRegistrationResult(
                registrationId: "test-device",
                createdDateTimeUtc: DateTime.UtcNow,
                assignedHub: "test-hub.azure-devices.net",
                deviceId: "test-device",
                status: ProvisioningRegistrationStatusType.Assigned,
                substatus: ProvisioningRegistrationSubstatusType.InitialAssignment,
                generationId: "gen-1",
                lastUpdatedDateTimeUtc: DateTime.UtcNow,
                errorCode: 0,
                errorMessage: null,
                etag: "etag-1",
                returnData: null,
                issuedClientCertificate: TestCertificateChain);

            // Assert
            Assert.IsNotNull(result.IssuedClientCertificate);
            Assert.AreEqual(2, result.IssuedClientCertificate.Count);
            Assert.AreEqual(TestCertificateChain[0], result.IssuedClientCertificate[0]);
            Assert.AreEqual(TestCertificateChain[1], result.IssuedClientCertificate[1]);
        }

        [TestMethod]
        public void DeviceRegistrationResult_IssuedClientCertificate_IsNullWhenNotProvided()
        {
            // Arrange & Act
            var result = new DeviceRegistrationResult(
                registrationId: "test-device",
                createdDateTimeUtc: DateTime.UtcNow,
                assignedHub: "test-hub.azure-devices.net",
                deviceId: "test-device",
                status: ProvisioningRegistrationStatusType.Assigned,
                substatus: ProvisioningRegistrationSubstatusType.InitialAssignment,
                generationId: "gen-1",
                lastUpdatedDateTimeUtc: DateTime.UtcNow,
                errorCode: 0,
                errorMessage: null,
                etag: "etag-1",
                returnData: null);

            // Assert
            Assert.IsNull(result.IssuedClientCertificate);
        }

        [TestMethod]
        public void CertificateHelper_ConvertToPem_ReturnsPemFormattedString()
        {
            // Act
            string pemResult = CertificateHelper.ConvertToPem(TestCertificateChain);

            // Assert
            Assert.IsNotNull(pemResult);
            Assert.IsTrue(pemResult.Contains("-----BEGIN CERTIFICATE-----"));
            Assert.IsTrue(pemResult.Contains("-----END CERTIFICATE-----"));
            Assert.IsTrue(pemResult.Contains(TestCertificateChain[0]));
            Assert.IsTrue(pemResult.Contains(TestCertificateChain[1]));
        }

        [TestMethod]
        public void CertificateHelper_ConvertToPem_ThrowsOnNull()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => CertificateHelper.ConvertToPem(null));
        }

        [TestMethod]
        public void CertificateHelper_ConvertToPem_HandlesEmptyList()
        {
            // Arrange
            IReadOnlyList<string> emptyChain = new List<string>().AsReadOnly();

            // Act
            string pemResult = CertificateHelper.ConvertToPem(emptyChain);

            // Assert
            Assert.AreEqual(string.Empty, pemResult);
        }

        [TestMethod]
        public void CertificateHelper_ConvertToPem_HandlesSingleCertificate()
        {
            // Arrange
            var singleCertChain = new List<string> { TestCertificateChain[0] };

            // Act
            string pemResult = CertificateHelper.ConvertToPem(singleCertChain);

            // Assert
            Assert.IsNotNull(pemResult);
            
            // Count occurrences of BEGIN CERTIFICATE
            int beginCount = pemResult.Split(new[] { "-----BEGIN CERTIFICATE-----" }, StringSplitOptions.None).Length - 1;
            Assert.AreEqual(1, beginCount);
        }
    }
}
