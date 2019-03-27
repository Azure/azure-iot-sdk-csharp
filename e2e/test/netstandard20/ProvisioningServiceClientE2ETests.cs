// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Provisioning.Security.Samples;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Shared;
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

        private static readonly string IdPrefix = $"e2e-{nameof(ProvisioningServiceClientE2ETests).ToLower()}-";

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

        [TestMethod]
        public async Task ProvisioningServiceClient_SymmetricKey_GroupEnrollments_Create_Http_Ok_WithReprovisioningFields()
        {
            //This webhook won't actually work for reprovisioning, but this test is only testing that the field is accepted by the service
            CustomAllocationDefinition customAllocationDefinition = new CustomAllocationDefinition() { ApiVersion = "2018-11-01", WebhookUrl = "https://www.microsoft.com" };
            ReprovisionPolicy reprovisionPolicy = new ReprovisionPolicy() { MigrateDeviceData = false, UpdateHubAssignment = true };
            AllocationPolicy allocationPolicy = AllocationPolicy.GeoLatency;

            await ProvisioningServiceClient_GroupEnrollments_Create_Ok("", AttestationType.SymmetricKey, reprovisionPolicy, allocationPolicy, customAllocationDefinition, null).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningServiceClient_SymmetricKey_IndividualEnrollment_Create_Http_Ok_WithReprovisioningFields()
        {
            //This webhook won't actually work for reprovisioning, but this test is only testing that the field is accepted by the service
            CustomAllocationDefinition customAllocationDefinition = new CustomAllocationDefinition() { ApiVersion = "2018-11-01", WebhookUrl = "https://www.microsoft.com" };
            ReprovisionPolicy reprovisionPolicy = new ReprovisionPolicy() { MigrateDeviceData = false, UpdateHubAssignment = true };
            AllocationPolicy allocationPolicy = AllocationPolicy.GeoLatency;

            await ProvisioningServiceClient_IndividualEnrollments_Create_Ok("", AttestationType.SymmetricKey, reprovisionPolicy, allocationPolicy, customAllocationDefinition, null).ConfigureAwait(false);
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
            await ProvisioningServiceClient_IndividualEnrollments_Create_Ok(proxyServerAddress, attestationType, null, AllocationPolicy.Hashed, null, null).ConfigureAwait(false);
        }

        public static async Task ProvisioningServiceClient_IndividualEnrollments_Create_Ok(string proxyServerAddress, AttestationType attestationType, ReprovisionPolicy reprovisionPolicy, AllocationPolicy allocationPolicy, CustomAllocationDefinition customAllocationDefinition, ICollection<string> iotHubsToProvisionTo)
        {
            using (ProvisioningServiceClient provisioningServiceClient = CreateProvisioningService(proxyServerAddress))
            {
                IndividualEnrollment individualEnrollment = await CreateIndividualEnrollment(provisioningServiceClient, attestationType, reprovisionPolicy, allocationPolicy, customAllocationDefinition, iotHubsToProvisionTo, null).ConfigureAwait(false);
                IndividualEnrollment individualEnrollmentResult = await provisioningServiceClient.GetIndividualEnrollmentAsync(individualEnrollment.RegistrationId).ConfigureAwait(false);
                Assert.AreEqual(individualEnrollmentResult.ProvisioningStatus, ProvisioningStatus.Enabled);

                if (reprovisionPolicy != null)
                {
                    Assert.AreEqual(reprovisionPolicy.UpdateHubAssignment, individualEnrollmentResult.ReprovisionPolicy.UpdateHubAssignment);
                    Assert.AreEqual(reprovisionPolicy.MigrateDeviceData, individualEnrollmentResult.ReprovisionPolicy.MigrateDeviceData);
                }

                if (customAllocationDefinition != null)
                {
                    Assert.AreEqual(customAllocationDefinition.WebhookUrl, individualEnrollmentResult.CustomAllocationDefinition.WebhookUrl);
                    Assert.AreEqual(customAllocationDefinition.ApiVersion, individualEnrollmentResult.CustomAllocationDefinition.ApiVersion);
                }

                //allocation policy is never null
                Assert.AreEqual(allocationPolicy, individualEnrollmentResult.AllocationPolicy);

                await provisioningServiceClient.DeleteIndividualEnrollmentAsync(individualEnrollment.RegistrationId).ConfigureAwait(false);
            }
        }

        public static async Task ProvisioningServiceClient_GroupEnrollments_Create_Ok(string proxyServerAddress, AttestationType attestationType)
        {
            await ProvisioningServiceClient_GroupEnrollments_Create_Ok(proxyServerAddress, attestationType, null, AllocationPolicy.Hashed, null, null).ConfigureAwait(false);
        }

        public static async Task ProvisioningServiceClient_GroupEnrollments_Create_Ok(string proxyServerAddress, AttestationType attestationType, ReprovisionPolicy reprovisionPolicy, AllocationPolicy allocationPolicy, CustomAllocationDefinition customAllocationDefinition, ICollection<string> iothubs)
        {
            string groupId = IdPrefix + AttestationTypeToString(attestationType) + "-" + Guid.NewGuid();
            using (ProvisioningServiceClient provisioningServiceClient = CreateProvisioningService(proxyServerAddress))
            {
                EnrollmentGroup enrollmentGroup = await CreateEnrollmentGroup(provisioningServiceClient, attestationType, groupId, reprovisionPolicy, allocationPolicy, customAllocationDefinition, iothubs, null).ConfigureAwait(false);
                EnrollmentGroup enrollmentGroupResult = await provisioningServiceClient.GetEnrollmentGroupAsync(enrollmentGroup.EnrollmentGroupId).ConfigureAwait(false);
                Assert.AreEqual(enrollmentGroupResult.ProvisioningStatus, ProvisioningStatus.Enabled);

                if (reprovisionPolicy != null)
                {
                    Assert.AreEqual(reprovisionPolicy.MigrateDeviceData, enrollmentGroupResult.ReprovisionPolicy.MigrateDeviceData);
                    Assert.AreEqual(reprovisionPolicy.UpdateHubAssignment, enrollmentGroupResult.ReprovisionPolicy.UpdateHubAssignment);
                }

                if (customAllocationDefinition != null)
                {
                    Assert.AreEqual(customAllocationDefinition.WebhookUrl, enrollmentGroupResult.CustomAllocationDefinition.WebhookUrl);
                    Assert.AreEqual(customAllocationDefinition.ApiVersion, enrollmentGroupResult.CustomAllocationDefinition.ApiVersion);
                }

                Assert.AreEqual(allocationPolicy, enrollmentGroup.AllocationPolicy);

                await provisioningServiceClient.DeleteEnrollmentGroupAsync(enrollmentGroup.EnrollmentGroupId).ConfigureAwait(false);
            }
        }

        public static async Task<IndividualEnrollment> CreateIndividualEnrollment(ProvisioningServiceClient provisioningServiceClient, AttestationType attestationType, ReprovisionPolicy reprovisionPolicy, AllocationPolicy allocationPolicy, CustomAllocationDefinition customAllocationDefinition, ICollection<string> iotHubsToProvisionTo, DeviceCapabilities capabilities)
        {
            string registrationId = AttestationTypeToString(attestationType) + "-registration-id-" + Guid.NewGuid();
            Attestation attestation;
            IndividualEnrollment individualEnrollment;
            switch (attestationType)
            {
                case AttestationType.Tpm:
                    using (var tpmSim = new SecurityProviderTpmSimulator(registrationId))
                    {
                        string base64Ek = Convert.ToBase64String(tpmSim.GetEndorsementKey());
                        var provisioningService = ProvisioningServiceClient.CreateFromConnectionString(Configuration.Provisioning.ConnectionString);
                        individualEnrollment = new IndividualEnrollment(registrationId, new TpmAttestation(base64Ek));
                        individualEnrollment.Capabilities = capabilities;
                        individualEnrollment.AllocationPolicy = allocationPolicy;
                        individualEnrollment.ReprovisionPolicy = reprovisionPolicy;
                        individualEnrollment.CustomAllocationDefinition = customAllocationDefinition;
                        individualEnrollment.IotHubs = iotHubsToProvisionTo;

                        IndividualEnrollment enrollment = await provisioningService.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment).ConfigureAwait(false);
                        attestation = new TpmAttestation(base64Ek);
                        enrollment.Attestation = attestation;
                        return await provisioningService.CreateOrUpdateIndividualEnrollmentAsync(enrollment).ConfigureAwait(false);
                    }
                case AttestationType.SymmetricKey:
                    string primaryKey = CryptoKeyGenerator.GenerateKey(32);
                    string secondaryKey = CryptoKeyGenerator.GenerateKey(32);
                    attestation = new SymmetricKeyAttestation(primaryKey, secondaryKey);
                    break;
                case AttestationType.x509:
                default:
                    throw new NotSupportedException("Test code has not been written for testing this attestation type yet");
            }

            individualEnrollment = new IndividualEnrollment(registrationId, attestation);
            individualEnrollment.Capabilities = capabilities;
            individualEnrollment.CustomAllocationDefinition = customAllocationDefinition;
            individualEnrollment.ReprovisionPolicy = reprovisionPolicy;
            individualEnrollment.IotHubs = iotHubsToProvisionTo;
            individualEnrollment.AllocationPolicy = allocationPolicy;
            return await provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment).ConfigureAwait(false);
        }

        public static async Task<EnrollmentGroup> CreateEnrollmentGroup(ProvisioningServiceClient provisioningServiceClient, AttestationType attestationType, string groupId, ReprovisionPolicy reprovisionPolicy, AllocationPolicy allocationPolicy, CustomAllocationDefinition customAllocationDefinition, ICollection<string> iothubs, DeviceCapabilities capabilities)
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

            EnrollmentGroup enrollmentGroup = new EnrollmentGroup(groupId, attestation);
            enrollmentGroup.Capabilities = capabilities;
            enrollmentGroup.ReprovisionPolicy = reprovisionPolicy;
            enrollmentGroup.AllocationPolicy = allocationPolicy;
            enrollmentGroup.CustomAllocationDefinition = customAllocationDefinition;
            enrollmentGroup.IotHubs = iothubs;

            return await provisioningServiceClient.CreateOrUpdateEnrollmentGroupAsync(enrollmentGroup).ConfigureAwait(false);
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
        public static string AttestationTypeToString(AttestationType attestationType)
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
