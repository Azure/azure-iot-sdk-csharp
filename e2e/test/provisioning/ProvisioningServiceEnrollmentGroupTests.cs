// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using FluentAssertions;
using FluentAssertions.Specialized;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Provisioning
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("DPS")]
    public class ProvisioningServiceEnrollmentGroupTests : E2EMsTestBase
    {
        private static readonly string s_devicePrefix = $"{nameof(ProvisioningServiceEnrollmentGroupTests)}_";
        private static readonly ProvisioningServiceExponentialBackoffRetryPolicy s_provisioningServiceRetryPolicy = new(20, TimeSpan.FromSeconds(3), true);

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
        public async Task ProvisioningServiceClient_EnrollmentGroups_SymmetricKey_ForceUpdate_Ok()
        {
            await ProvisioningServiceClient_GroupEnrollments_CreateOrUpdate_Ok(AttestationMechanismType.SymmetricKey, true, true).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_EnrollmentGroups_SymmetricKey_Get_Ok()
        {
            await ProvisioningServiceClient_GetEnrollmentGroupAttestation(AttestationMechanismType.SymmetricKey);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_EnrollmentGroups_SymmetricKey_WithReprovisioningFields_Create_Ok()
        {
            // This webhook won't actually work for reprovisioning, but this test is only testing that the field is accepted by the service
            var customAllocationDefinition = new CustomAllocationDefinition { ApiVersion = "2019-03-31", WebhookUrl = new Uri("https://www.microsoft.com") };
            var reprovisionPolicy = new ReprovisionPolicy { MigrateDeviceData = false, UpdateHubAssignment = true };

            await ProvisioningServiceClient_GroupEnrollments_CreateOrUpdate_Ok(
                AttestationMechanismType.SymmetricKey,
                reprovisionPolicy,
                AllocationPolicy.GeoLatency,
                customAllocationDefinition,
                false,
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
                .RunBulkOperationAsync(BulkOperationMode.Create, createEnrollmentGroupList)
                .ConfigureAwait(false);

            createBulkEnrollmentResult.IsSuccessful.Should().BeTrue();

            // update two enrollments in bulk
            EnrollmentGroup retrievedEnrollment1 = await provisioningServiceClient.EnrollmentGroups.GetAsync(registrationId1).ConfigureAwait(false);
            retrievedEnrollment1.Capabilities = new InitialTwinCapabilities
            {
                IsIotEdge = true,
            };
            EnrollmentGroup retrievedEnrollment2 = await provisioningServiceClient.EnrollmentGroups.GetAsync(registrationId2).ConfigureAwait(false);
            retrievedEnrollment2.Capabilities = new InitialTwinCapabilities
            {
                IsIotEdge = true,
            };
            var updateEnrollmentGroupsList = new List<EnrollmentGroup> { retrievedEnrollment1, retrievedEnrollment2 };

            BulkEnrollmentOperationResult updateBulkEnrollmentResult = await provisioningServiceClient
                .EnrollmentGroups
                .RunBulkOperationAsync(BulkOperationMode.Update, updateEnrollmentGroupsList)
                .ConfigureAwait(false);

            updateBulkEnrollmentResult.IsSuccessful.Should().BeTrue();

            // delete two enrollments in bulk
            retrievedEnrollment1 = await provisioningServiceClient.EnrollmentGroups.GetAsync(registrationId1).ConfigureAwait(false);
            retrievedEnrollment2 = await provisioningServiceClient.EnrollmentGroups.GetAsync(registrationId2).ConfigureAwait(false);
            var deleteEnrollmentGroupsList = new List<EnrollmentGroup> { retrievedEnrollment1, retrievedEnrollment2 };

            BulkEnrollmentOperationResult deleteBulkEnrollmentResult = await provisioningServiceClient
                .EnrollmentGroups
                .RunBulkOperationAsync(BulkOperationMode.Delete, deleteEnrollmentGroupsList)
                .ConfigureAwait(false);

            deleteBulkEnrollmentResult.IsSuccessful.Should().BeTrue();
        }

        [TestMethod]
        public async Task ProvisioningServiceClient_EnrollmentGroups_Query_Ok()
        {
            using var provisioningServiceClient = new ProvisioningServiceClient(TestConfiguration.Provisioning.ConnectionString);

            // Create an enrollment group so that the query is guaranteed to return at least one entry
            string enrollmentGroupId = Guid.NewGuid().ToString();
            var enrollmentGroup = new EnrollmentGroup(enrollmentGroupId, new SymmetricKeyAttestation());
            await provisioningServiceClient.EnrollmentGroups
                .CreateOrUpdateAsync(enrollmentGroup).ConfigureAwait(false);

            int maxCount = 5;
            int currentCount = 0;

            try
            {
                string queryString = "SELECT * FROM enrollmentGroups";
                IAsyncEnumerable<EnrollmentGroup> query = provisioningServiceClient.EnrollmentGroups.CreateQuery(queryString);
                await foreach (EnrollmentGroup enrollment in query)
                {
                    // Just checking that the returned type was, in fact, an enrollment group and that deserialization
                    // of the always-present field works.
                    enrollment.Id.Should().NotBeNull();

                    // Don't want to query all the enrollment groups. Just query a few.
                    if (++currentCount >= maxCount)
                    {
                        return;
                    }
                }
            }
            finally
            {
                try
                {
                    await provisioningServiceClient.EnrollmentGroups
                        .DeleteAsync(enrollmentGroupId).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    // Failed to cleanup after the test, but don't fail the test because of this
                    VerboseTestLogger.WriteLine($"Failed to clean up enrollment group due to {e}");
                }
            }
        }

        private static async Task ProvisioningServiceClient_GetEnrollmentGroupAttestation(AttestationMechanismType attestationType)
        {
            string groupId = AttestationTypeToString(attestationType) + "-" + Guid.NewGuid();
            using var provisioningServiceClient = new ProvisioningServiceClient(TestConfiguration.Provisioning.ConnectionString);

            EnrollmentGroup enrollmentGroup = null;

            try
            {
                enrollmentGroup = await CreateEnrollmentGroupAsync(provisioningServiceClient, attestationType, groupId, null, AllocationPolicy.Static, null);

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
                    await DeleteCreatedEnrollmentAsync(provisioningServiceClient, enrollmentGroup.Id);
                }
            }
        }

        private static async Task ProvisioningServiceClient_GroupEnrollments_CreateOrUpdate_Ok(AttestationMechanismType attestationType, bool update = default, bool forceUpdate = default)
        {
            await ProvisioningServiceClient_GroupEnrollments_CreateOrUpdate_Ok(
                attestationType,
                null,
                AllocationPolicy.Hashed,
                null,
                update,
                forceUpdate).ConfigureAwait(false);
        }

        private static async Task ProvisioningServiceClient_GroupEnrollments_CreateOrUpdate_Ok(
            AttestationMechanismType attestationType,
            ReprovisionPolicy reprovisionPolicy,
            AllocationPolicy allocationPolicy,
            CustomAllocationDefinition customAllocationDefinition,
            bool update,
            bool forceUpdate)
        {
            string groupId = s_devicePrefix + AttestationTypeToString(attestationType) + "-" + Guid.NewGuid();
            using var provisioningServiceClient = new ProvisioningServiceClient(TestConfiguration.Provisioning.ConnectionString);

            EnrollmentGroup createdEnrollmentGroup = null;

            try
            {
                createdEnrollmentGroup = await CreateEnrollmentGroupAsync(
                        provisioningServiceClient,
                        attestationType,
                        groupId,
                        reprovisionPolicy,
                        allocationPolicy,
                        customAllocationDefinition)
                    .ConfigureAwait(false);

                EnrollmentGroup retrievedEnrollmentGroup = null;
                await RetryOperationHelper
                    .RunWithProvisioningServiceRetryAsync(
                        async () =>
                        {
                            retrievedEnrollmentGroup = await provisioningServiceClient.EnrollmentGroups.GetAsync(createdEnrollmentGroup.Id).ConfigureAwait(false);
                        },
                        s_provisioningServiceRetryPolicy,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                if (retrievedEnrollmentGroup == null)
                {
                    throw new ArgumentException($"The enrollment group with group Id {createdEnrollmentGroup.Id} could not retrieved, exiting test.");
                }

                retrievedEnrollmentGroup.ProvisioningStatus.Should().Be(ProvisioningStatus.Enabled);

                if (reprovisionPolicy != null)
                {
                    retrievedEnrollmentGroup.ReprovisionPolicy.UpdateHubAssignment.Should().Be(reprovisionPolicy.UpdateHubAssignment);
                    retrievedEnrollmentGroup.ReprovisionPolicy.MigrateDeviceData.Should().Be(reprovisionPolicy.MigrateDeviceData);
                }

                if (customAllocationDefinition != null)
                {
                    retrievedEnrollmentGroup.CustomAllocationDefinition.WebhookUrl.Should().Be(customAllocationDefinition.WebhookUrl);
                    retrievedEnrollmentGroup.CustomAllocationDefinition.ApiVersion.Should().Be(customAllocationDefinition.ApiVersion);
                }

                //allocation policy is never null
                retrievedEnrollmentGroup.AllocationPolicy.Should().Be(allocationPolicy);

                if (update)
                {
                    retrievedEnrollmentGroup.Capabilities = new InitialTwinCapabilities { IsIotEdge = true };

                    if (forceUpdate)
                    {
                        retrievedEnrollmentGroup.ETag = ETag.All;
                    }

                    EnrollmentGroup updatedEnrollmentGroup = null;
                    await RetryOperationHelper
                        .RunWithProvisioningServiceRetryAsync(
                            async () =>
                            {
                                updatedEnrollmentGroup = await provisioningServiceClient.EnrollmentGroups.CreateOrUpdateAsync(retrievedEnrollmentGroup).ConfigureAwait(false);
                            },
                            s_provisioningServiceRetryPolicy,
                            CancellationToken.None)
                        .ConfigureAwait(false);

                    if (updatedEnrollmentGroup == null)
                    {
                        throw new ArgumentException($"The individual enrollment with registration Id {retrievedEnrollmentGroup.Id} could not updated, exiting test.");
                    }

                    updatedEnrollmentGroup.ProvisioningStatus.Should().Be(ProvisioningStatus.Enabled);
                    updatedEnrollmentGroup.Id.Should().Be(retrievedEnrollmentGroup.Id);
                }
            }
            finally
            {
                if (createdEnrollmentGroup != null)
                {
                    await DeleteCreatedEnrollmentAsync(provisioningServiceClient, createdEnrollmentGroup.Id).ConfigureAwait(false);
                }
            }
        }

        private static async Task<EnrollmentGroup> CreateEnrollmentGroupAsync(
            ProvisioningServiceClient provisioningServiceClient,
            AttestationMechanismType attestationType,
            string groupId,
            ReprovisionPolicy reprovisionPolicy,
            AllocationPolicy allocationPolicy,
            CustomAllocationDefinition customAllocationDefinition)
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
                ReprovisionPolicy = reprovisionPolicy,
                AllocationPolicy = allocationPolicy,
                CustomAllocationDefinition = customAllocationDefinition,
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

        /// <summary>
        /// Returns the registrationId compliant name for the provided attestation type
        /// </summary>
        private static string AttestationTypeToString(AttestationMechanismType attestationType)
        {
            return attestationType switch
            {
                AttestationMechanismType.SymmetricKey => "symmetrickey",
                AttestationMechanismType.X509 => "x509",
                _ => throw new NotSupportedException("Test code has not been written for testing this attestation type yet"),
            };
        }

        private static async Task DeleteCreatedEnrollmentAsync(
            ProvisioningServiceClient provisioningServiceClient,
            string groupId)
        {
            try
            {
                await RetryOperationHelper
                    .RunWithProvisioningServiceRetryAsync(
                        async () =>
                        {
                            await provisioningServiceClient.EnrollmentGroups.DeleteAsync(groupId).ConfigureAwait(false);
                        },
                        s_provisioningServiceRetryPolicy)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                VerboseTestLogger.WriteLine($"Cleanup of enrollment failed due to {ex}.");
            }
        }
    }
}