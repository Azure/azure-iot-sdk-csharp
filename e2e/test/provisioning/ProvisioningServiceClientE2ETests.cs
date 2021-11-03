// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Provisioning.Security.Samples;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Provisioning
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("DPS")]
    public class ProvisioningServiceClientE2ETests : E2EMsTestBase
    {
        private static readonly string s_proxyServerAddress = TestConfiguration.IoTHub.ProxyServerAddress;
        private static readonly string s_devicePrefix = $"E2E_{nameof(ProvisioningServiceClientE2ETests)}_";

#pragma warning disable CA1823
        private readonly VerboseTestLogger _verboseLog = VerboseTestLogger.GetInstance();
#pragma warning restore CA1823

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        public async Task ProvisioningServiceClient_IndividualEnrollments_Query_HttpWithProxy_Ok()
        {
            await ProvisioningServiceClient_IndividualEnrollments_Query_Ok(s_proxyServerAddress).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        public async Task ProvisioningServiceClient_Tpm_IndividualEnrollments_Create_HttpWithProxy_Ok()
        {
            await ProvisioningServiceClient_IndividualEnrollments_Create_Ok(s_proxyServerAddress, AttestationMechanismType.Tpm).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        public async Task ProvisioningServiceClient_SymmetricKey_IndividualEnrollments_Create_HttpWithProxy_Ok()
        {
            await ProvisioningServiceClient_IndividualEnrollments_Create_Ok(s_proxyServerAddress, AttestationMechanismType.SymmetricKey).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningServiceClient_Tpm_IndividualEnrollments_Create_HttpWithoutProxy_Ok()
        {
            await ProvisioningServiceClient_IndividualEnrollments_Create_Ok("", AttestationMechanismType.Tpm).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningServiceClient_SymmetricKey_IndividualEnrollments_Create_HttpWithoutProxy_Ok()
        {
            await ProvisioningServiceClient_IndividualEnrollments_Create_Ok("", AttestationMechanismType.SymmetricKey).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        public async Task ProvisioningServiceClient_SymmetricKey_GroupEnrollments_Create_HttpWithProxy_Ok()
        {
            await ProvisioningServiceClient_GroupEnrollments_Create_Ok(s_proxyServerAddress, AttestationMechanismType.SymmetricKey).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningServiceClient_SymmetricKey_GroupEnrollments_Create_Http_Ok()
        {
            await ProvisioningServiceClient_GroupEnrollments_Create_Ok("", AttestationMechanismType.SymmetricKey).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningServiceClient_SymmetricKey_GroupEnrollments_Create_Http_Ok_WithReprovisioningFields()
        {
            //This webhook won't actually work for reprovisioning, but this test is only testing that the field is accepted by the service
            var customAllocationDefinition = new CustomAllocationDefinition { ApiVersion = "2019-03-31", WebhookUrl = "https://www.microsoft.com" };
            var reprovisionPolicy = new ReprovisionPolicy { MigrateDeviceData = false, UpdateHubAssignment = true };
            var allocationPolicy = AllocationPolicy.GeoLatency;

            await ProvisioningServiceClient_GroupEnrollments_Create_Ok("", AttestationMechanismType.SymmetricKey, reprovisionPolicy, allocationPolicy, customAllocationDefinition, null).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningServiceClient_SymmetricKey_IndividualEnrollment_Create_Http_Ok_WithReprovisioningFields()
        {
            //This webhook won't actually work for reprovisioning, but this test is only testing that the field is accepted by the service
            var customAllocationDefinition = new CustomAllocationDefinition() { ApiVersion = "2019-03-31", WebhookUrl = "https://www.microsoft.com" };
            var reprovisionPolicy = new ReprovisionPolicy() { MigrateDeviceData = false, UpdateHubAssignment = true };
            AllocationPolicy allocationPolicy = AllocationPolicy.GeoLatency;

            await ProvisioningServiceClient_IndividualEnrollments_Create_Ok("", AttestationMechanismType.SymmetricKey, reprovisionPolicy, allocationPolicy, customAllocationDefinition, null).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningServiceClient_GetIndividualEnrollmentAttestation_SymmetricKey()
        {
            await ProvisioningServiceClient_GetIndividualEnrollmentAttestation(AttestationMechanismType.SymmetricKey);
        }

        [LoggedTestMethod]
        public async Task ProvisioningServiceClient_GetIndividualEnrollmentAttestation_Tpm()
        {
            await ProvisioningServiceClient_GetIndividualEnrollmentAttestation(AttestationMechanismType.Tpm);
        }

        [LoggedTestMethod]
        public async Task ProvisioningServiceClient_GetEnrollmentGroupAttestation_SymmetricKey()
        {
            await ProvisioningServiceClient_GetEnrollmentGroupAttestation(AttestationMechanismType.SymmetricKey);
        }

        public async Task ProvisioningServiceClient_GetIndividualEnrollmentAttestation(AttestationMechanismType attestationType)
        {
            using ProvisioningServiceClient provisioningServiceClient = ProvisioningServiceClient.CreateFromConnectionString(TestConfiguration.Provisioning.ConnectionString);
            IndividualEnrollment individualEnrollment = await CreateIndividualEnrollment(provisioningServiceClient, attestationType, null, AllocationPolicy.Static, null, null, null);

            AttestationMechanism attestationMechanism = await provisioningServiceClient.GetIndividualEnrollmentAttestationAsync(individualEnrollment.RegistrationId);

            if (attestationType == AttestationMechanismType.SymmetricKey)
            {
                Assert.AreEqual(AttestationMechanismType.SymmetricKey, attestationMechanism.Type);
                SymmetricKeyAttestation symmetricKeyAttestation = (SymmetricKeyAttestation)attestationMechanism.GetAttestation();
                Assert.AreEqual(((SymmetricKeyAttestation)individualEnrollment.Attestation).PrimaryKey, symmetricKeyAttestation.PrimaryKey);
                Assert.AreEqual(((SymmetricKeyAttestation)individualEnrollment.Attestation).SecondaryKey, symmetricKeyAttestation.SecondaryKey);
            }
            else if (attestationType == AttestationMechanismType.X509)
            {
                Assert.AreEqual(AttestationMechanismType.X509, attestationMechanism.Type);
                X509Attestation x509Attestation = (X509Attestation)attestationMechanism.GetAttestation();
                Assert.AreEqual(((X509Attestation)individualEnrollment.Attestation).GetPrimaryX509CertificateInfo().SHA1Thumbprint, x509Attestation.GetPrimaryX509CertificateInfo().SHA1Thumbprint);
                Assert.AreEqual(((X509Attestation)individualEnrollment.Attestation).GetSecondaryX509CertificateInfo().SHA1Thumbprint, x509Attestation.GetSecondaryX509CertificateInfo().SHA1Thumbprint);
            }
            else
            {
                Assert.AreEqual(AttestationMechanismType.Tpm, attestationMechanism.Type);
                TpmAttestation tpmAttestation = (TpmAttestation)attestationMechanism.GetAttestation();
                Assert.AreEqual(((TpmAttestation)individualEnrollment.Attestation).EndorsementKey, tpmAttestation.EndorsementKey);
                Assert.AreEqual(((TpmAttestation)individualEnrollment.Attestation).StorageRootKey, tpmAttestation.StorageRootKey);
            }
        }

        public async Task ProvisioningServiceClient_GetEnrollmentGroupAttestation(AttestationMechanismType attestationType)
        {
            using ProvisioningServiceClient provisioningServiceClient = ProvisioningServiceClient.CreateFromConnectionString(TestConfiguration.Provisioning.ConnectionString);
            string groupId = AttestationTypeToString(attestationType) + "-" + Guid.NewGuid();
            EnrollmentGroup enrollmentGroup = await CreateEnrollmentGroup(provisioningServiceClient, attestationType, groupId, null, AllocationPolicy.Static, null, null, null);

            AttestationMechanism attestationMechanism = await provisioningServiceClient.GetEnrollmentGroupAttestationAsync(enrollmentGroup.EnrollmentGroupId);

            // Note that tpm is not a supported attestation type for group enrollments
            if (attestationType == AttestationMechanismType.SymmetricKey)
            {
                Assert.AreEqual(AttestationMechanismType.SymmetricKey, attestationMechanism.Type);
                SymmetricKeyAttestation symmetricKeyAttestation = (SymmetricKeyAttestation)attestationMechanism.GetAttestation();
                Assert.AreEqual(((SymmetricKeyAttestation)enrollmentGroup.Attestation).PrimaryKey, symmetricKeyAttestation.PrimaryKey);
                Assert.AreEqual(((SymmetricKeyAttestation)enrollmentGroup.Attestation).SecondaryKey, symmetricKeyAttestation.SecondaryKey);
            }
            else if (attestationType == AttestationMechanismType.X509)
            {
                Assert.AreEqual(AttestationMechanismType.X509, attestationMechanism.Type);
                X509Attestation x509Attestation = (X509Attestation)attestationMechanism.GetAttestation();
                Assert.AreEqual(((X509Attestation)enrollmentGroup.Attestation).GetPrimaryX509CertificateInfo().SHA1Thumbprint, x509Attestation.GetPrimaryX509CertificateInfo().SHA1Thumbprint);
                Assert.AreEqual(((X509Attestation)enrollmentGroup.Attestation).GetSecondaryX509CertificateInfo().SHA1Thumbprint, x509Attestation.GetSecondaryX509CertificateInfo().SHA1Thumbprint);
            }
        }

        /// <summary>
        /// Attempts to query all enrollments using a provisioning service client instance
        /// </summary>
        /// <param name="proxyServerAddress">The address of the proxy to be used, or null/empty if no proxy should be used</param>
        /// <returns>If the query succeeded, otherwise this method will throw</returns>
        private async Task ProvisioningServiceClient_IndividualEnrollments_Query_Ok(string proxyServerAddress)
        {
            using ProvisioningServiceClient provisioningServiceClient = CreateProvisioningService(proxyServerAddress);
            var querySpecification = new QuerySpecification("SELECT * FROM enrollments");
            using (Query query = provisioningServiceClient.CreateIndividualEnrollmentQuery(querySpecification))
            {
                while (query.HasNext())
                {
                    QueryResult queryResult = await query.NextAsync().ConfigureAwait(false);
                    Assert.AreEqual(queryResult.Type, QueryResultType.Enrollment);
                }
            }
        }

        public static async Task ProvisioningServiceClient_IndividualEnrollments_Create_Ok(string proxyServerAddress, AttestationMechanismType attestationType)
        {
            await ProvisioningServiceClient_IndividualEnrollments_Create_Ok(proxyServerAddress, attestationType, null, AllocationPolicy.Hashed, null, null).ConfigureAwait(false);
        }

        public static async Task ProvisioningServiceClient_IndividualEnrollments_Create_Ok(string proxyServerAddress, AttestationMechanismType attestationType, ReprovisionPolicy reprovisionPolicy, AllocationPolicy allocationPolicy, CustomAllocationDefinition customAllocationDefinition, ICollection<string> iotHubsToProvisionTo)
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

        public static async Task ProvisioningServiceClient_GroupEnrollments_Create_Ok(string proxyServerAddress, AttestationMechanismType attestationType)
        {
            await ProvisioningServiceClient_GroupEnrollments_Create_Ok(proxyServerAddress, attestationType, null, AllocationPolicy.Hashed, null, null).ConfigureAwait(false);
        }

        public static async Task ProvisioningServiceClient_GroupEnrollments_Create_Ok(string proxyServerAddress, AttestationMechanismType attestationType, ReprovisionPolicy reprovisionPolicy, AllocationPolicy allocationPolicy, CustomAllocationDefinition customAllocationDefinition, ICollection<string> iothubs)
        {
            string groupId = s_devicePrefix + AttestationTypeToString(attestationType) + "-" + Guid.NewGuid();
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

                try
                {
                    await provisioningServiceClient.DeleteEnrollmentGroupAsync(enrollmentGroup.EnrollmentGroupId).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Cleanup of enrollment group failed due to {ex}");
                }
            }
        }

        public static async Task<IndividualEnrollment> CreateIndividualEnrollment(ProvisioningServiceClient provisioningServiceClient, AttestationMechanismType attestationType, ReprovisionPolicy reprovisionPolicy, AllocationPolicy allocationPolicy, CustomAllocationDefinition customAllocationDefinition, ICollection<string> iotHubsToProvisionTo, DeviceCapabilities capabilities)
        {
            string registrationId = AttestationTypeToString(attestationType) + "-registration-id-" + Guid.NewGuid();
            Attestation attestation;
            IndividualEnrollment individualEnrollment;
            switch (attestationType)
            {
                case AttestationMechanismType.Tpm:
                    using (var tpmSim = new SecurityProviderTpmSimulator(registrationId))
                    {
                        string base64Ek = Convert.ToBase64String(tpmSim.GetEndorsementKey());
                        using ProvisioningServiceClient provisioningService = ProvisioningServiceClient.CreateFromConnectionString(TestConfiguration.Provisioning.ConnectionString);
                        individualEnrollment = new IndividualEnrollment(registrationId, new TpmAttestation(base64Ek))
                        {
                            Capabilities = capabilities,
                            AllocationPolicy = allocationPolicy,
                            ReprovisionPolicy = reprovisionPolicy,
                            CustomAllocationDefinition = customAllocationDefinition,
                            IotHubs = iotHubsToProvisionTo
                        };

                        IndividualEnrollment enrollment = await provisioningService.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment).ConfigureAwait(false);
                        attestation = new TpmAttestation(base64Ek);
                        enrollment.Attestation = attestation;
                        return await provisioningService.CreateOrUpdateIndividualEnrollmentAsync(enrollment).ConfigureAwait(false);
                    }
                case AttestationMechanismType.SymmetricKey:
                    string primaryKey = CryptoKeyGenerator.GenerateKey(32);
                    string secondaryKey = CryptoKeyGenerator.GenerateKey(32);
                    attestation = new SymmetricKeyAttestation(primaryKey, secondaryKey);
                    break;

                case AttestationMechanismType.X509:
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

        public static async Task<EnrollmentGroup> CreateEnrollmentGroup(ProvisioningServiceClient provisioningServiceClient, AttestationMechanismType attestationType, string groupId, ReprovisionPolicy reprovisionPolicy, AllocationPolicy allocationPolicy, CustomAllocationDefinition customAllocationDefinition, ICollection<string> iothubs, DeviceCapabilities capabilities)
        {
            Attestation attestation;
            switch (attestationType)
            {
                case AttestationMechanismType.Tpm:
                    throw new NotSupportedException("Group enrollments do not support tpm attestation");
                case AttestationMechanismType.SymmetricKey:
                    string primaryKey = CryptoKeyGenerator.GenerateKey(32);
                    string secondaryKey = CryptoKeyGenerator.GenerateKey(32);
                    attestation = new SymmetricKeyAttestation(primaryKey, secondaryKey);
                    break;

                case AttestationMechanismType.X509:
                default:
                    throw new NotSupportedException("Test code has not been written for testing this attestation type yet");
            }

            var enrollmentGroup = new EnrollmentGroup(groupId, attestation)
            {
                Capabilities = capabilities,
                ReprovisionPolicy = reprovisionPolicy,
                AllocationPolicy = allocationPolicy,
                CustomAllocationDefinition = customAllocationDefinition,
                IotHubs = iothubs,
            };

            return await provisioningServiceClient.CreateOrUpdateEnrollmentGroupAsync(enrollmentGroup).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the provisioning service client instance
        /// </summary>
        /// <param name="proxyServerAddress">The address of the proxy to be used, or null/empty if no proxy will be used</param>
        /// <returns>the provisioning service client instance</returns>
        public static ProvisioningServiceClient CreateProvisioningService(string proxyServerAddress)
        {
            var transportSettings = new Devices.Provisioning.Service.HttpTransportSettings();

            if (!string.IsNullOrWhiteSpace(proxyServerAddress))
            {
                transportSettings.Proxy = new WebProxy(proxyServerAddress);
            }

            return ProvisioningServiceClient.CreateFromConnectionString(TestConfiguration.Provisioning.ConnectionString, transportSettings);
        }

        /// <summary>
        /// Returns the registrationId compliant name for the provided attestation type
        /// </summary>
        public static string AttestationTypeToString(AttestationMechanismType attestationType)
        {
            switch (attestationType)
            {
                case AttestationMechanismType.Tpm:
                    return "tpm";

                case AttestationMechanismType.SymmetricKey:
                    return "symmetrickey";

                case AttestationMechanismType.X509:
                    return "x509";

                default:
                    throw new NotSupportedException("Test code has not been written for testing this attestation type yet");
            }
        }
    }
}
