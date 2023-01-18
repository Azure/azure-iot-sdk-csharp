// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Specialized;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Provisioning
{
    public partial class ProvisioningServiceClientE2ETests : E2EMsTestBase
    {
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_EnrollmentGroups_SymmetricKey_Create_Ok()
        {
            await ProvisioningServiceClient_GroupEnrollments_CreateOrUpdate_Ok(AttestationMechanismType.SymmetricKey).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_EnrollmentGroups_SymmetricKey_Update_Ok()
        {
            await ProvisioningServiceClient_GroupEnrollments_CreateOrUpdate_Ok(AttestationMechanismType.SymmetricKey, true).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_EnrollmentGroups_SymmetricKey_Get_Ok()
        {
            await ProvisioningServiceClient_GetEnrollmentGroupAttestation(AttestationMechanismType.SymmetricKey);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_EnrollmentGroups_SymmetricKey_Create_WithReprovisioningFields_Ok()
        {
            // This webhook won't actually work for reprovisioning, but this test is only testing that the field is accepted by the service
            var customAllocationDefinition = new CustomAllocationDefinition { ApiVersion = "2019-03-31", WebhookUrl = new Uri("https://www.microsoft.com") };
            var reprovisionPolicy = new ReprovisionPolicy { MigrateDeviceData = false, UpdateHubAssignment = true };

            await ProvisioningServiceClient_GroupEnrollments_CreateOrUpdate_Ok(
                AttestationMechanismType.SymmetricKey,
                reprovisionPolicy,
                AllocationPolicy.GeoLatency,
                customAllocationDefinition,
                null,
                false).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_EnrollmentGroups_InvalidRegistration_Get_Fails()
        {
            using var provisioningServiceClient = new ProvisioningServiceClient(TestConfiguration.Provisioning.ConnectionString);

            // act
            Func<Task> act = async () => await provisioningServiceClient.EnrollmentGroups.GetAsync("invalid-registration-id").ConfigureAwait(false);

            // assert
            ExceptionAssertions<ProvisioningServiceException> error = await act.Should().ThrowAsync<ProvisioningServiceException>();
            error.And.StatusCode.Should().Be(HttpStatusCode.NotFound);
            error.And.ErrorCode.Should().Be(404204);
            error.And.IsTransient.Should().BeFalse();
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_EnrollmentGroups_SymmetricKey_BulkOperation_Ok()
        {
            using var provisioningServiceClient = new ProvisioningServiceClient(TestConfiguration.Provisioning.ConnectionString);

            string registrationId1 = $"{s_devicePrefix}_{Guid.NewGuid()}";
            string registrationId2 = $"{s_devicePrefix}_{Guid.NewGuid()}";

            // create two enrollments in bulk
            var enrollmentGroup1 = new EnrollmentGroup(registrationId1, new SymmetricKeyAttestation());
            var enrollmentGroup2 = new EnrollmentGroup(registrationId2, new SymmetricKeyAttestation());
            var createEnrollmentGroupList = new List<EnrollmentGroup> { enrollmentGroup1, enrollmentGroup2 };

            BulkEnrollmentOperationResult createBulkEnrollmentResult = await provisioningServiceClient
                .EnrollmentGroups
                .RunBulkOperationAsync(BulkOperationMode.Create, createEnrollmentGroupList);

            createBulkEnrollmentResult.IsSuccessful.Should().BeTrue();

            // update two enrollments in bulk
            EnrollmentGroup retrievedEnrollment1 = await provisioningServiceClient.EnrollmentGroups.GetAsync(registrationId1);
            retrievedEnrollment1.Capabilities = new InitialTwinCapabilities
            {
                IsIotEdge = true,
            };
            EnrollmentGroup retrievedEnrollment2 = await provisioningServiceClient.EnrollmentGroups.GetAsync(registrationId2);
            retrievedEnrollment2.Capabilities = new InitialTwinCapabilities
            {
                IsIotEdge = true,
            };
            var updateEnrollmentGroupsList = new List<EnrollmentGroup> { retrievedEnrollment1, retrievedEnrollment2 };

            BulkEnrollmentOperationResult updateBulkEnrollmentResult = await provisioningServiceClient
                .EnrollmentGroups
                .RunBulkOperationAsync(BulkOperationMode.Update, updateEnrollmentGroupsList);

            updateBulkEnrollmentResult.IsSuccessful.Should().BeTrue();

            // delete two enrollments in bulk
            retrievedEnrollment1 = await provisioningServiceClient.EnrollmentGroups.GetAsync(registrationId1);
            retrievedEnrollment2 = await provisioningServiceClient.EnrollmentGroups.GetAsync(registrationId2);
            var deleteEnrollmentGroupsList = new List<EnrollmentGroup> { retrievedEnrollment1, retrievedEnrollment2 };

            BulkEnrollmentOperationResult deleteBulkEnrollmentResult = await provisioningServiceClient
                .EnrollmentGroups
                .RunBulkOperationAsync(BulkOperationMode.Delete, deleteEnrollmentGroupsList);

            deleteBulkEnrollmentResult.IsSuccessful.Should().BeTrue();
        }

        public static async Task ProvisioningServiceClient_GetEnrollmentGroupAttestation(AttestationMechanismType attestationType)
        {
            using var provisioningServiceClient = new ProvisioningServiceClient(TestConfiguration.Provisioning.ConnectionString);
            string groupId = AttestationTypeToString(attestationType) + "-" + Guid.NewGuid();
            EnrollmentGroup enrollmentGroup = null;

            try
            {
                enrollmentGroup = await CreateEnrollmentGroupAsync(provisioningServiceClient, attestationType, groupId, null, AllocationPolicy.Static, null, null, null);

                AttestationMechanism attestationMechanism = null;
                await RetryOperationHelper
                    .RunWithProvisioningServiceRetryAsync(
                        async () =>
                        {
                            attestationMechanism = await provisioningServiceClient.EnrollmentGroups.GetAttestationAsync(enrollmentGroup.Id);
                        },
                        s_provisioningServiceRetryPolicy,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                if (attestationMechanism == null)
                {
                    throw new ArgumentException($"The attestation mechanism for enrollment with group Id {enrollmentGroup.Id} could not retrieved, exiting test.");
                }

                if (attestationType == AttestationMechanismType.SymmetricKey)
                {
                    attestationMechanism.Type.Should().Be(AttestationMechanismType.SymmetricKey);

                    var symmetricKeyAttestation = (SymmetricKeyAttestation)attestationMechanism.GetAttestation();
                    symmetricKeyAttestation.PrimaryKey.Should().Be(((SymmetricKeyAttestation)enrollmentGroup.Attestation).PrimaryKey);
                    symmetricKeyAttestation.SecondaryKey.Should().Be(((SymmetricKeyAttestation)enrollmentGroup.Attestation).SecondaryKey);
                }
                else if (attestationType == AttestationMechanismType.X509)
                {
                    attestationMechanism.Type.Should().Be(AttestationMechanismType.X509);

                    var x509Attestation = (X509Attestation)attestationMechanism.GetAttestation();
                    x509Attestation.GetPrimaryX509CertificateInfo().Sha1Thumbprint
                        .Should()
                        .Be(((X509Attestation)enrollmentGroup.Attestation).GetPrimaryX509CertificateInfo().Sha1Thumbprint);
                    x509Attestation.GetSecondaryX509CertificateInfo().Sha1Thumbprint
                        .Should()
                        .Be(((X509Attestation)enrollmentGroup.Attestation).GetSecondaryX509CertificateInfo().Sha1Thumbprint);
                }
            }
            finally
            {
                if (enrollmentGroup != null)
                {
                    await DeleteCreatedEnrollmentAsync(EnrollmentType.Group, null, enrollmentGroup.Id);
                }
            }
        }

        public static async Task ProvisioningServiceClient_GroupEnrollments_CreateOrUpdate_Ok(AttestationMechanismType attestationType, bool update = default)
        {
            await ProvisioningServiceClient_GroupEnrollments_CreateOrUpdate_Ok(
                attestationType,
                null,
                AllocationPolicy.Hashed,
                null,
                null,
                update).ConfigureAwait(false);
        }

        public static async Task ProvisioningServiceClient_GroupEnrollments_CreateOrUpdate_Ok(
            AttestationMechanismType attestationType,
            ReprovisionPolicy reprovisionPolicy,
            AllocationPolicy allocationPolicy,
            CustomAllocationDefinition customAllocationDefinition,
            IList<string> iothubs,
            bool update)
        {
            string groupId = s_devicePrefix + AttestationTypeToString(attestationType) + "-" + Guid.NewGuid();
            using ProvisioningServiceClient provisioningServiceClient = CreateProvisioningService();
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
                        null)
                    .ConfigureAwait(false);

                EnrollmentGroup enrollmentGroupResult = null;
                await RetryOperationHelper
                    .RunWithProvisioningServiceRetryAsync(
                        async () =>
                        {
                            enrollmentGroupResult = await provisioningServiceClient.EnrollmentGroups.GetAsync(enrollmentGroup.Id).ConfigureAwait(false);
                        },
                        s_provisioningServiceRetryPolicy,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                if (enrollmentGroupResult == null)
                {
                    throw new ArgumentException($"The enrollment group with group Id {enrollmentGroup.Id} could not retrieved, exiting test.");
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
                    await DeleteCreatedEnrollmentAsync(EnrollmentType.Group, "", enrollmentGroup.Id).ConfigureAwait(false);
                }
            }
        }

        public static async Task<EnrollmentGroup> CreateEnrollmentGroupAsync(
            ProvisioningServiceClient provisioningServiceClient,
            AttestationMechanismType attestationType,
            string groupId,
            ReprovisionPolicy reprovisionPolicy,
            AllocationPolicy allocationPolicy,
            CustomAllocationDefinition customAllocationDefinition,
            IList<string> iothubs,
            InitialTwinCapabilities capabilities)
        {
            Attestation attestation;

            switch (attestationType)
            {
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

            EnrollmentGroup createdEnrollmentGroup = null;
            await RetryOperationHelper
               .RunWithProvisioningServiceRetryAsync(
                   async () =>
                   {
                       createdEnrollmentGroup = await provisioningServiceClient.EnrollmentGroups.CreateOrUpdateAsync(enrollmentGroup).ConfigureAwait(false);
                   },
                   s_provisioningServiceRetryPolicy,
                    CancellationToken.None)
               .ConfigureAwait(false);

            return createdEnrollmentGroup
                ?? throw new ArgumentException($"The enrollment entry with group Id {groupId} could not be created, exiting test.");
        }
    }
}