// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Provisioning.Security.Samples;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    using HttpTransportSettings = Microsoft.Azure.Devices.Provisioning.Service.HttpTransportSettings;

    [TestClass]
    [TestCategory("Provisioning-E2E")]
    public class ProvisioningServiceClientE2ETests : IDisposable
    {
        public enum AttestationType
        {
            Tpm,
            x509,
            SymmetricKey
        }

        private static string ProxyServerAddress = Configuration.IoTHub.ProxyServerAddress;

        private readonly VerboseTestLogging _verboseLog = VerboseTestLogging.GetInstance();
        private readonly TestLogging _log = TestLogging.GetInstance();
        private readonly ConsoleEventListener _listener;

        public ProvisioningServiceClientE2ETests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningServiceClient_IndividualEnrollments_Query_HttpWithProxy_Ok()
        {
            await ProvisioningServiceClient_IndividualEnrollments_Query_Ok(ProxyServerAddress).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningServiceClient_Tpm_IndividualEnrollments_Create_HttpWithProxy_Ok()
        {
            await ProvisioningServiceClient_IndividualEnrollments_Create_Ok(ProxyServerAddress, AttestationType.Tpm).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningServiceClient_SymmetricKey_IndividualEnrollments_Create_HttpWithProxy_Ok()
        {
            await ProvisioningServiceClient_IndividualEnrollments_Create_Ok(ProxyServerAddress, AttestationType.SymmetricKey).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningServiceClient_Tpm_IndividualEnrollments_Create_HttpWithoutProxy_Ok()
        {
            await ProvisioningServiceClient_IndividualEnrollments_Create_Ok("", AttestationType.Tpm).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningServiceClient_SymmetricKey_IndividualEnrollments_Create_HttpWithoutProxy_Ok()
        {
            await ProvisioningServiceClient_IndividualEnrollments_Create_Ok("", AttestationType.SymmetricKey).ConfigureAwait(false);
        }
        
        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningServiceClient_SymmetricKey_GroupEnrollments_Create_HttpWithProxy_Ok()
        {
            await ProvisioningServiceClient_GroupEnrollments_Create_Ok(ProxyServerAddress, AttestationType.SymmetricKey).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningServiceClient_SymmetricKey_GroupEnrollments_Create_Http_Ok()
        {
            await ProvisioningServiceClient_GroupEnrollments_Create_Ok("", AttestationType.SymmetricKey).ConfigureAwait(false);
        }

        /// <summary>
        /// Attempts to query all enrollments using a provisioning service client instance
        /// </summary>
        /// <param name="proxyServerAddress">The address of the proxy to be used, or null/empty if no proxy should be used</param>
        /// <returns>If the query succeeded, otherwise this method will throw</returns>
        private async Task ProvisioningServiceClient_IndividualEnrollments_Query_Ok(string proxyServerAddress)
        {
            ProvisioningServiceClient provisioningServiceClient = CreateProvisioningService(proxyServerAddress);
            QuerySpecification querySpecification = new QuerySpecification("SELECT * FROM enrollments");
            using (Query query = provisioningServiceClient.CreateIndividualEnrollmentQuery(querySpecification))
            {
                while (query.HasNext())
                {
                    QueryResult queryResult = await query.NextAsync().ConfigureAwait(false);
                    Assert.AreEqual(queryResult.Type, QueryResultType.Enrollment);
                }
            }
        }

        public static async Task ProvisioningServiceClient_IndividualEnrollments_Create_Ok(string proxyServerAddress, AttestationType attestationType)
        {
            ProvisioningServiceClient provisioningServiceClient = CreateProvisioningService(proxyServerAddress);
            IndividualEnrollment individualEnrollment = await CreateIndividualEnrollment(provisioningServiceClient, attestationType).ConfigureAwait(false);
            IndividualEnrollment individualEnrollmentResult = await provisioningServiceClient.GetIndividualEnrollmentAsync(individualEnrollment.RegistrationId).ConfigureAwait(false);
            Assert.AreEqual(individualEnrollmentResult.ProvisioningStatus, ProvisioningStatus.Enabled);

            await provisioningServiceClient.DeleteIndividualEnrollmentAsync(individualEnrollment.RegistrationId).ConfigureAwait(false);
        }

        public static async Task ProvisioningServiceClient_GroupEnrollments_Create_Ok(string proxyServerAddress, AttestationType attestationType)
        {
            string groupId = "some-valid-group-id-" + attestationTypeToString(attestationType) + "-" + Guid.NewGuid();
            ProvisioningServiceClient provisioningServiceClient = CreateProvisioningService(proxyServerAddress);
            EnrollmentGroup enrollmentGroup = await CreateEnrollmentGroup(provisioningServiceClient, attestationType, groupId).ConfigureAwait(false);
            EnrollmentGroup enrollmentGroupResult = await provisioningServiceClient.GetEnrollmentGroupAsync(enrollmentGroup.EnrollmentGroupId).ConfigureAwait(false);
            Assert.AreEqual(enrollmentGroupResult.ProvisioningStatus, ProvisioningStatus.Enabled);

            await provisioningServiceClient.DeleteEnrollmentGroupAsync(enrollmentGroup.EnrollmentGroupId).ConfigureAwait(false);
        }

        public static async Task<IndividualEnrollment> CreateIndividualEnrollment(ProvisioningServiceClient provisioningServiceClient, AttestationType attestationType)
        {
            string registrationId = "some-valid-registration-id-" + attestationTypeToString(attestationType) + "-" + Guid.NewGuid();
            Attestation attestation;
            switch (attestationType)
            {
                case AttestationType.Tpm:
                    var tpmSim = new SecurityProviderTpmSimulator(registrationId);
                    string base64Ek = Convert.ToBase64String(tpmSim.GetEndorsementKey());
                    attestation = new TpmAttestation(base64Ek);
                    break;
                case AttestationType.SymmetricKey:
                    string primaryKey = CryptoKeyGenerator.GenerateKey(32);
                    string secondaryKey = CryptoKeyGenerator.GenerateKey(32);
                    attestation = new SymmetricKeyAttestation(primaryKey, secondaryKey);
                    break;
                case AttestationType.x509:
                default:
                    throw new NotSupportedException("Test code has not been written for testing this attestation type yet");
            }

            IndividualEnrollment individualEnrollment =
                    new IndividualEnrollment(
                            registrationId,
                            attestation);

            IndividualEnrollment result = await provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment).ConfigureAwait(false);
            return result;
        }

        public static async Task<EnrollmentGroup> CreateEnrollmentGroup(ProvisioningServiceClient provisioningServiceClient, AttestationType attestationType, string groupId)
        {
            Attestation attestation;
            switch (attestationType)
            {
                case AttestationType.Tpm:
                    throw new NotSupportedException("Group enrollments do not support tpm attestation");
                case AttestationType.SymmetricKey:
                    string primaryKey = CryptoKeyGenerator.GenerateKey(32);
                    string secondaryKey = CryptoKeyGenerator.GenerateKey(32);
                    attestation = new SymmetricKeyAttestation(primaryKey, secondaryKey);
                    break;
                case AttestationType.x509:
                default:
                    throw new NotSupportedException("Test code has not been written for testing this attestation type yet");
            }

            EnrollmentGroup enrollmentGroup =
                    new EnrollmentGroup(
                            groupId,
                            attestation);

            EnrollmentGroup result = await provisioningServiceClient.CreateOrUpdateEnrollmentGroupAsync(enrollmentGroup).ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Creates the provisioning service client instance
        /// </summary>
        /// <param name="proxyServerAddress">The address of the proxy to be used, or null/empty if no proxy will be used</param>
        /// <returns>the provisioning service client instance</returns>
        public static ProvisioningServiceClient CreateProvisioningService(string proxyServerAddress)
        {
            HttpTransportSettings transportSettings = new HttpTransportSettings();

            if (!string.IsNullOrWhiteSpace(proxyServerAddress))
            {
                transportSettings.Proxy = new WebProxy(proxyServerAddress);
            }

            return ProvisioningServiceClient.CreateFromConnectionString(Configuration.Provisioning.ConnectionString, transportSettings);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Returns the registrationId compliant name for the provided attestation type
        /// </summary>
        public static string attestationTypeToString(AttestationType attestationType)
        {
            switch (attestationType)
            {
                case AttestationType.Tpm:
                    return "tpm";
                case AttestationType.SymmetricKey:
                    return "symmetrickey";
                case AttestationType.x509:
                    return "x509";
                default:
                    throw new NotSupportedException("Test code has not been written for testing this attestation type yet");
            }

        }
    }
}
