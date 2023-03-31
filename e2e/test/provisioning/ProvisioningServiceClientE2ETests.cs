﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.Provisioning.Security.Samples;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Provisioning
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("DPS")]
    public class ProvisioningServiceClientE2ETests : E2EMsTestBase
    {
        private static readonly string s_proxyServerAddress = TestConfiguration.IotHub.ProxyServerAddress;
        private static readonly string s_devicePrefix = $"{nameof(ProvisioningServiceClientE2ETests)}_";

        private static readonly HashSet<Type> s_retryableExceptions = new HashSet<Type> { typeof(ProvisioningServiceClientHttpException) };
        private static readonly IRetryPolicy s_provisioningServiceRetryPolicy = new ProvisioningServiceRetryPolicy();

        public enum EnrollmentType
        {
            Individual,
            Group,
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [TestCategory("Proxy")]
        public async Task ProvisioningServiceClient_IndividualEnrollments_Query_HttpWithProxy_Ok()
        {
            await ProvisioningServiceClient_IndividualEnrollments_Query_Ok(s_proxyServerAddress).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [TestCategory("Proxy")]
        public async Task ProvisioningServiceClient_Tpm_IndividualEnrollments_Create_HttpWithProxy_Ok()
        {
            await ProvisioningServiceClient_IndividualEnrollments_Create_Ok(s_proxyServerAddress, AttestationMechanismType.Tpm).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [TestCategory("Proxy")]
        public async Task ProvisioningServiceClient_SymmetricKey_IndividualEnrollments_Create_HttpWithProxy_Ok()
        {
            await ProvisioningServiceClient_IndividualEnrollments_Create_Ok(s_proxyServerAddress, AttestationMechanismType.SymmetricKey).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_Tpm_IndividualEnrollments_Create_HttpWithoutProxy_Ok()
        {
            await ProvisioningServiceClient_IndividualEnrollments_Create_Ok("", AttestationMechanismType.Tpm).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_SymmetricKey_IndividualEnrollments_Create_HttpWithoutProxy_Ok()
        {
            await ProvisioningServiceClient_IndividualEnrollments_Create_Ok("", AttestationMechanismType.SymmetricKey).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [TestCategory("Proxy")]
        public async Task ProvisioningServiceClient_SymmetricKey_GroupEnrollments_Create_HttpWithProxy_Ok()
        {
            await ProvisioningServiceClient_GroupEnrollments_Create_Ok(s_proxyServerAddress, AttestationMechanismType.SymmetricKey).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_SymmetricKey_GroupEnrollments_Create_Http_Ok()
        {
            await ProvisioningServiceClient_GroupEnrollments_Create_Ok("", AttestationMechanismType.SymmetricKey).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_SymmetricKey_GroupEnrollments_Create_Http_Ok_WithReprovisioningFields()
        {
            //This webhook won't actually work for reprovisioning, but this test is only testing that the field is accepted by the service
            var customAllocationDefinition = new CustomAllocationDefinition { ApiVersion = "2019-03-31", WebhookUrl = "https://www.microsoft.com" };
            var reprovisionPolicy = new ReprovisionPolicy { MigrateDeviceData = false, UpdateHubAssignment = true };
            AllocationPolicy allocationPolicy = AllocationPolicy.GeoLatency;

            await ProvisioningServiceClient_GroupEnrollments_Create_Ok(
                "",
                AttestationMechanismType.SymmetricKey,
                reprovisionPolicy,
                allocationPolicy,
                customAllocationDefinition,
                null).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_SymmetricKey_IndividualEnrollment_Create_Http_Ok_WithReprovisioningFields()
        {
            //This webhook won't actually work for reprovisioning, but this test is only testing that the field is accepted by the service
            var customAllocationDefinition = new CustomAllocationDefinition() { ApiVersion = "2019-03-31", WebhookUrl = "https://www.microsoft.com" };
            var reprovisionPolicy = new ReprovisionPolicy() { MigrateDeviceData = false, UpdateHubAssignment = true };
            AllocationPolicy allocationPolicy = AllocationPolicy.GeoLatency;

            await ProvisioningServiceClient_IndividualEnrollments_Create_Ok(
                "",
                AttestationMechanismType.SymmetricKey,
                reprovisionPolicy,
                allocationPolicy,
                customAllocationDefinition,
                null).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_GetIndividualEnrollmentAttestation_SymmetricKey()
        {
            await ProvisioningServiceClient_GetIndividualEnrollmentAttestation(AttestationMechanismType.SymmetricKey);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_GetIndividualEnrollmentAttestation_Tpm()
        {
            await ProvisioningServiceClient_GetIndividualEnrollmentAttestation(AttestationMechanismType.Tpm);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_GetEnrollmentGroupAttestation_SymmetricKey()
        {
            await ProvisioningServiceClient_GetEnrollmentGroupAttestation(AttestationMechanismType.SymmetricKey);
        }

        public async Task ProvisioningServiceClient_GetIndividualEnrollmentAttestation(AttestationMechanismType attestationType)
        {
            using var provisioningServiceClient = ProvisioningServiceClient.CreateFromConnectionString(TestConfiguration.Provisioning.ConnectionString);
            string registrationId = AttestationTypeToString(attestationType) + "-" + Guid.NewGuid();

            IndividualEnrollment individualEnrollment = null;

            try
            {
                individualEnrollment = await CreateIndividualEnrollmentAsync(
                        provisioningServiceClient,
                        registrationId,
                        attestationType,
                        null,
                        null,
                        AllocationPolicy.Static,
                        null,
                        null,
                        null)
                    .ConfigureAwait(false);

                AttestationMechanism attestationMechanism = null;
                await RetryOperationHelper
                    .RetryOperationsAsync(
                        async () =>
                        {
                            attestationMechanism = await provisioningServiceClient.GetIndividualEnrollmentAttestationAsync(individualEnrollment.RegistrationId);
                        },
                        s_provisioningServiceRetryPolicy,
                        s_retryableExceptions)
                    .ConfigureAwait(false);

                if (attestationMechanism == null)
                {
                    throw new ArgumentException($"The attestation mechanism for enrollment with registration Id {individualEnrollment.RegistrationId} could not retrieved, exiting test.");
                }

                if (attestationType == AttestationMechanismType.SymmetricKey)
                {
                    attestationMechanism.Type.Should().Be(AttestationMechanismType.SymmetricKey);

                    var symmetricKeyAttestation = (SymmetricKeyAttestation)attestationMechanism.GetAttestation();
                    symmetricKeyAttestation.PrimaryKey.Should().Be(((SymmetricKeyAttestation)individualEnrollment.Attestation).PrimaryKey);
                    symmetricKeyAttestation.SecondaryKey.Should().Be(((SymmetricKeyAttestation)individualEnrollment.Attestation).SecondaryKey);
                }
                else
                {
                    attestationMechanism.Type.Should().Be(AttestationMechanismType.Tpm);

                    var tpmAttestation = (TpmAttestation)attestationMechanism.GetAttestation();
                    tpmAttestation.EndorsementKey.Should().Be(((TpmAttestation)individualEnrollment.Attestation).EndorsementKey);
                    tpmAttestation.StorageRootKey.Should().Be(((TpmAttestation)individualEnrollment.Attestation).StorageRootKey);
                }
            }
            finally
            {
                if (individualEnrollment != null)
                {
                    // No need to dispose x509 certificates here, not testing x509 based enrollments
                    await DeleteCreatedEnrollmentAsync(EnrollmentType.Individual, individualEnrollment.RegistrationId, null);
                }
            }
        }

        public async Task ProvisioningServiceClient_GetEnrollmentGroupAttestation(AttestationMechanismType attestationType)
        {
            using var provisioningServiceClient = ProvisioningServiceClient.CreateFromConnectionString(TestConfiguration.Provisioning.ConnectionString);
            string groupId = AttestationTypeToString(attestationType) + "-" + Guid.NewGuid();

            EnrollmentGroup enrollmentGroup = null;

            try
            {
                enrollmentGroup = await CreateEnrollmentGroupAsync(
                        provisioningServiceClient,
                        attestationType,
                        groupId,
                        null,
                        AllocationPolicy.Static,
                        null,
                        null,
                        null)
                    .ConfigureAwait(false);

                AttestationMechanism attestationMechanism = null;
                await RetryOperationHelper
                    .RetryOperationsAsync(
                        async () =>
                        {
                            attestationMechanism = await provisioningServiceClient.GetEnrollmentGroupAttestationAsync(enrollmentGroup.EnrollmentGroupId);
                        },
                        s_provisioningServiceRetryPolicy,
                        s_retryableExceptions)
                    .ConfigureAwait(false);

                if (attestationMechanism == null)
                {
                    throw new ArgumentException($"The attestation mechanism for enrollment with group Id {enrollmentGroup.EnrollmentGroupId} could not retrieved, exiting test.");
                }

                // Note that tpm is not a supported attestation type for group enrollments
                if (attestationType == AttestationMechanismType.SymmetricKey)
                {
                    attestationMechanism.Type.Should().Be(AttestationMechanismType.SymmetricKey);

                    var symmetricKeyAttestation = (SymmetricKeyAttestation)attestationMechanism.GetAttestation();
                    symmetricKeyAttestation.PrimaryKey.Should().Be(((SymmetricKeyAttestation)enrollmentGroup.Attestation).PrimaryKey);
                    symmetricKeyAttestation.SecondaryKey.Should().Be(((SymmetricKeyAttestation)enrollmentGroup.Attestation).SecondaryKey);
                }
            }
            finally
            {
                if (enrollmentGroup != null)
                {
                    // No need to dispose x509 certificates here, not testing x509 based enrollments
                    await DeleteCreatedEnrollmentAsync(EnrollmentType.Group, null, enrollmentGroup.EnrollmentGroupId);
                }
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
            using Query query = provisioningServiceClient.CreateIndividualEnrollmentQuery(querySpecification);
            while (query.HasNext())
            {
                QueryResult queryResult = await query.NextAsync().ConfigureAwait(false);
                Assert.AreEqual(queryResult.Type, QueryResultType.Enrollment);
            }
        }

        public async Task ProvisioningServiceClient_IndividualEnrollments_Create_Ok(string proxyServerAddress, AttestationMechanismType attestationType)
        {
            await ProvisioningServiceClient_IndividualEnrollments_Create_Ok(proxyServerAddress, attestationType, null, AllocationPolicy.Hashed, null, null).ConfigureAwait(false);
        }

        public async Task ProvisioningServiceClient_IndividualEnrollments_Create_Ok(
            string proxyServerAddress,
            AttestationMechanismType attestationType,
            ReprovisionPolicy reprovisionPolicy,
            AllocationPolicy allocationPolicy,
            CustomAllocationDefinition customAllocationDefinition,
            ICollection<string> iotHubsToProvisionTo)
        {
            using (ProvisioningServiceClient provisioningServiceClient = CreateProvisioningService(proxyServerAddress))
            {
                string registrationId = AttestationTypeToString(attestationType) + "-" + Guid.NewGuid();

                IndividualEnrollment individualEnrollment = null;

                try
                {
                    individualEnrollment = await CreateIndividualEnrollmentAsync(
                        provisioningServiceClient,
                        registrationId,
                        attestationType,
                        null,
                        reprovisionPolicy,
                        allocationPolicy,
                        customAllocationDefinition,
                        iotHubsToProvisionTo,
                        null).ConfigureAwait(false);

                    IndividualEnrollment individualEnrollmentResult = null;
                    await RetryOperationHelper
                        .RetryOperationsAsync(
                            async () =>
                            {
                                individualEnrollmentResult = await provisioningServiceClient.GetIndividualEnrollmentAsync(individualEnrollment.RegistrationId).ConfigureAwait(false);
                            },
                            s_provisioningServiceRetryPolicy,
                            s_retryableExceptions)
                        .ConfigureAwait(false);

                    if (individualEnrollmentResult == null)
                    {
                        throw new ArgumentException($"The individual enrollment with registration Id {individualEnrollment.RegistrationId} could not retrieved, exiting test.");
                    }

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
                }
                finally
                {
                    if (individualEnrollment != null)
                    {
                        // No need to dispose x509 certificates here, not testing x509 based enrollments
                        await DeleteCreatedEnrollmentAsync(EnrollmentType.Individual, individualEnrollment.RegistrationId, null);
                    }
                }
            }
        }

        public async Task ProvisioningServiceClient_GroupEnrollments_Create_Ok(string proxyServerAddress, AttestationMechanismType attestationType)
        {
            await ProvisioningServiceClient_GroupEnrollments_Create_Ok(proxyServerAddress, attestationType, null, AllocationPolicy.Hashed, null, null).ConfigureAwait(false);
        }

        public async Task ProvisioningServiceClient_GroupEnrollments_Create_Ok(
            string proxyServerAddress,
            AttestationMechanismType attestationType,
            ReprovisionPolicy reprovisionPolicy,
            AllocationPolicy allocationPolicy,
            CustomAllocationDefinition customAllocationDefinition,
            ICollection<string> iothubs)
        {
            string groupId = s_devicePrefix + AttestationTypeToString(attestationType) + "-" + Guid.NewGuid();
            using (ProvisioningServiceClient provisioningServiceClient = CreateProvisioningService(proxyServerAddress))
            {
                EnrollmentGroup enrollmentGroup = null;

                try
                {
                    enrollmentGroup = await CreateEnrollmentGroupAsync(
                        provisioningServiceClient,
                        attestationType,
                        groupId,
                        reprovisionPolicy,
                        allocationPolicy,
                        customAllocationDefinition,
                        iothubs,
                        null).ConfigureAwait(false);

                    EnrollmentGroup enrollmentGroupResult = null;
                    await RetryOperationHelper
                        .RetryOperationsAsync(
                            async () =>
                            {
                                enrollmentGroupResult = await provisioningServiceClient.GetEnrollmentGroupAsync(enrollmentGroup.EnrollmentGroupId).ConfigureAwait(false);
                            },
                            s_provisioningServiceRetryPolicy,
                            s_retryableExceptions)
                        .ConfigureAwait(false);

                    if (enrollmentGroupResult == null)
                    {
                        throw new ArgumentException($"The enrollment group with group Id {enrollmentGroup.EnrollmentGroupId} could not retrieved, exiting test.");
                    }

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
                }
                finally
                {
                    if (enrollmentGroup != null)
                    {
                        // No need to dispose x509 certificates here, not testing x509 based enrollments
                        await DeleteCreatedEnrollmentAsync(EnrollmentType.Group, "", enrollmentGroup.EnrollmentGroupId).ConfigureAwait(false);
                    }
                }
            }
        }

        public static async Task<IndividualEnrollment> CreateIndividualEnrollmentAsync(
            ProvisioningServiceClient provisioningServiceClient,
            string registrationId,
            AttestationMechanismType attestationType,
            X509Certificate2 authenticationCertificate,
            ReprovisionPolicy reprovisionPolicy,
            AllocationPolicy allocationPolicy,
            CustomAllocationDefinition customAllocationDefinition,
            ICollection<string> iotHubsToProvisionTo,
            DeviceCapabilities capabilities)
        {
            Attestation attestation;
            IndividualEnrollment individualEnrollment;
            IndividualEnrollment createdEnrollment = null;
            switch (attestationType)
            {
                case AttestationMechanismType.Tpm:
                    using (var tpmSim = new SecurityProviderTpmSimulator(registrationId))
                    {
                        string base64Ek = Convert.ToBase64String(tpmSim.GetEndorsementKey());
                        individualEnrollment = new IndividualEnrollment(registrationId, new TpmAttestation(base64Ek))
                        {
                            Capabilities = capabilities,
                            AllocationPolicy = allocationPolicy,
                            ReprovisionPolicy = reprovisionPolicy,
                            CustomAllocationDefinition = customAllocationDefinition,
                            IotHubs = iotHubsToProvisionTo
                        };

                        IndividualEnrollment temporaryCreatedEnrollment = null;
                        await RetryOperationHelper
                            .RetryOperationsAsync(
                                async () =>
                                {
                                    temporaryCreatedEnrollment = await provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment).ConfigureAwait(false);
                                },
                                s_provisioningServiceRetryPolicy,
                                s_retryableExceptions)
                            .ConfigureAwait(false);

                        if (temporaryCreatedEnrollment == null)
                        {
                            throw new ArgumentException($"The enrollment entry with registration Id {registrationId} could not be created, exiting test.");
                        }

                        attestation = new TpmAttestation(base64Ek);
                        temporaryCreatedEnrollment.Attestation = attestation;

                        await RetryOperationHelper
                            .RetryOperationsAsync(
                                async () =>
                                {
                                    createdEnrollment = await provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(temporaryCreatedEnrollment).ConfigureAwait(false);
                                },
                                s_provisioningServiceRetryPolicy,
                                s_retryableExceptions)
                            .ConfigureAwait(false);

                        if (createdEnrollment == null)
                        {
                            throw new ArgumentException($"The enrollment entry with registration Id {registrationId} could not be updated, exiting test.");
                        }

                        return createdEnrollment;
                    }

                case AttestationMechanismType.SymmetricKey:
                    string primaryKey = CryptoKeyGenerator.GenerateKey(32);
                    string secondaryKey = CryptoKeyGenerator.GenerateKey(32);
                    attestation = new SymmetricKeyAttestation(primaryKey, secondaryKey);
                    break;

                case AttestationMechanismType.X509:
                    attestation = X509Attestation.CreateFromClientCertificates(authenticationCertificate);
                    break;

                default:
                    throw new NotSupportedException("Test code has not been written for testing this attestation type yet");
            }

            individualEnrollment = new IndividualEnrollment(registrationId, attestation)
            {
                Capabilities = capabilities,
                AllocationPolicy = allocationPolicy,
                ReprovisionPolicy = reprovisionPolicy,
                CustomAllocationDefinition = customAllocationDefinition,
                IotHubs = iotHubsToProvisionTo,
            };

            await RetryOperationHelper
                .RetryOperationsAsync(
                    async () =>
                    {
                        createdEnrollment = await provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment).ConfigureAwait(false);
                    },
                    s_provisioningServiceRetryPolicy,
                    s_retryableExceptions)
                .ConfigureAwait(false);

            if (createdEnrollment == null)
            {
                throw new ArgumentException($"The enrollment entry with registration Id {registrationId} could not be created, exiting test.");
            }

            return createdEnrollment;
        }

        public static async Task<EnrollmentGroup> CreateEnrollmentGroupAsync(
            ProvisioningServiceClient provisioningServiceClient,
            AttestationMechanismType attestationType,
            string groupId,
            ReprovisionPolicy reprovisionPolicy,
            AllocationPolicy allocationPolicy,
            CustomAllocationDefinition customAllocationDefinition,
            ICollection<string> iothubs,
            DeviceCapabilities capabilities)
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

            EnrollmentGroup createdEnrollmentGroup = null;
            await RetryOperationHelper
               .RetryOperationsAsync(
                   async () =>
                   {
                       createdEnrollmentGroup = await provisioningServiceClient.CreateOrUpdateEnrollmentGroupAsync(enrollmentGroup).ConfigureAwait(false);
                   },
                   s_provisioningServiceRetryPolicy,
                   s_retryableExceptions)
               .ConfigureAwait(false);

            if (createdEnrollmentGroup == null)
            {
                throw new ArgumentException($"The enrollment entry with group Id {groupId} could not be created, exiting test.");
            }

            return createdEnrollmentGroup;
        }

        public static async Task DeleteCreatedEnrollmentAsync(
            EnrollmentType enrollmentType,
            string registrationId,
            string groupId)
        {
            using ProvisioningServiceClient dpsClient = CreateProvisioningService();

            try
            {
                if (enrollmentType == EnrollmentType.Individual)
                {
                    await RetryOperationHelper
                        .RetryOperationsAsync(
                            async () =>
                            {
                                await dpsClient.DeleteIndividualEnrollmentAsync(registrationId).ConfigureAwait(false);
                            },
                            s_provisioningServiceRetryPolicy,
                            s_retryableExceptions)
                        .ConfigureAwait(false);
                }
                else if (enrollmentType == EnrollmentType.Group)
                {
                    await RetryOperationHelper
                        .RetryOperationsAsync(
                            async () =>
                            {
                                await dpsClient.DeleteEnrollmentGroupAsync(groupId).ConfigureAwait(false);
                            },
                            s_provisioningServiceRetryPolicy,
                            s_retryableExceptions)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                VerboseTestLogger.WriteLine($"Cleanup of enrollment failed due to {ex}.");
            }
        }

        /// <summary>
        /// Creates the provisioning service client instance
        /// </summary>
        /// <param name="proxyServerAddress">The address of the proxy to be used, or null/empty if no proxy will be used</param>
        /// <returns>the provisioning service client instance</returns>
        public static ProvisioningServiceClient CreateProvisioningService(string proxyServerAddress = null)
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
            return attestationType switch
            {
                AttestationMechanismType.Tpm => "tpm",
                AttestationMechanismType.SymmetricKey => "symmetrickey",
                AttestationMechanismType.X509 => "x509",
                _ => throw new NotSupportedException("Test code has not been written for testing this attestation type yet"),
            };
        }
    }
}
