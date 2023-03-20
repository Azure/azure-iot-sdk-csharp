// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
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
    public class ProvisioningServiceIndividualEnrollmentTests : E2EMsTestBase
    {
        private static readonly string s_proxyServerAddress = TestConfiguration.IotHub.ProxyServerAddress;
        private static readonly string s_devicePrefix = $"{nameof(ProvisioningServiceIndividualEnrollmentTests)}_";

        private static readonly ProvisioningServiceExponentialBackoffRetryPolicy s_provisioningServiceRetryPolicy = new(20, TimeSpan.FromSeconds(3), true);

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [TestCategory("Proxy")]
        public async Task ProvisioningServiceClient_IndividualEnrollment_Query_WithProxy_Ok()
        {
            await ProvisioningServiceClient_IndividualEnrollments_Query_Ok(s_proxyServerAddress).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_IndividualEnrollment_SymmetricKey_Create_Ok()
        {
            await ProvisioningServiceClient_IndividualEnrollments_CreateOrUpdate_Ok(AttestationMechanismType.SymmetricKey).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_IndividualEnrollment_SymmetricKey_Update_Ok()
        {
            await ProvisioningServiceClient_IndividualEnrollments_CreateOrUpdate_Ok(AttestationMechanismType.SymmetricKey, true).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_IndividualEnrollment_SymmetricKey_ForceUpdate_Ok()
        {
            await ProvisioningServiceClient_IndividualEnrollments_CreateOrUpdate_Ok(AttestationMechanismType.SymmetricKey, true, true).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_IndividualEnrollment_SymmetricKey_Get_Ok()
        {
            await ProvisioningServiceClient_GetIndividualEnrollmentAttestation(AttestationMechanismType.SymmetricKey);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_IndividualEnrollment_SymmetricKey_WithReprovisioningFields_Create_Ok_()
        {
            //This webhook won't actually work for reprovisioning, but this test is only testing that the field is accepted by the service
            var customAllocationDefinition = new CustomAllocationDefinition { ApiVersion = "2019-03-31", WebhookUrl = new Uri("https://www.microsoft.com") };
            var reprovisionPolicy = new ReprovisionPolicy { MigrateDeviceData = false, UpdateHubAssignment = true };

            await ProvisioningServiceClient_IndividualEnrollments_CreateOrUpdate_Ok(
                AttestationMechanismType.SymmetricKey,
                reprovisionPolicy,
                AllocationPolicy.GeoLatency,
                customAllocationDefinition,
                false,
                false).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_IndividualEnrollment_InvalidRegistrationId_Get_Fails()
        {
            using var provisioningServiceClient = new ProvisioningServiceClient(TestConfiguration.Provisioning.ConnectionString);

            // act
            Func<Task> act = async () => await provisioningServiceClient.IndividualEnrollments.GetAsync("invalid-registration-id").ConfigureAwait(false);

            // assert
            ExceptionAssertions<ProvisioningServiceException> error = await act.Should().ThrowAsync<ProvisioningServiceException>();
            error.And.StatusCode.Should().Be(HttpStatusCode.NotFound);
            error.And.ErrorCode.Should().Be(404201);
            error.And.IsTransient.Should().BeFalse();
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_IndividualEnrollment_SymmetricKey_BulkOperation_Ok()
        {
            using var provisioningServiceClient = new ProvisioningServiceClient(TestConfiguration.Provisioning.ConnectionString);

            string registrationId1 = $"{s_devicePrefix}_{Guid.NewGuid()}";
            string registrationId2 = $"{s_devicePrefix}_{Guid.NewGuid()}";

            // create two enrollments in bulk
            var individualEnrollment1 = new IndividualEnrollment(registrationId1, new SymmetricKeyAttestation());
            var individualEnrollment2 = new IndividualEnrollment(registrationId2, new SymmetricKeyAttestation());
            var createIndividualEnrollmentList = new List<IndividualEnrollment> { individualEnrollment1, individualEnrollment2 };

            BulkEnrollmentOperationResult createBulkEnrollmentResult = await provisioningServiceClient
                .IndividualEnrollments
                .RunBulkOperationAsync(BulkOperationMode.Create, createIndividualEnrollmentList)
                .ConfigureAwait(false);

            createBulkEnrollmentResult.IsSuccessful.Should().BeTrue();

            // update two enrollments in bulk
            IndividualEnrollment retrievedEnrollment1 = await provisioningServiceClient.IndividualEnrollments.GetAsync(registrationId1).ConfigureAwait(false);
            retrievedEnrollment1.Capabilities = new InitialTwinCapabilities
            {
                IsIotEdge = true,
            };
            IndividualEnrollment retrievedEnrollment2 = await provisioningServiceClient.IndividualEnrollments.GetAsync(registrationId2).ConfigureAwait(false);
            retrievedEnrollment2.Capabilities = new InitialTwinCapabilities
            {
                IsIotEdge = true,
            };
            var updateIndividualEnrollmentList = new List<IndividualEnrollment> { retrievedEnrollment1, retrievedEnrollment2 };

            BulkEnrollmentOperationResult updateBulkEnrollmentResult = await provisioningServiceClient
                .IndividualEnrollments
                .RunBulkOperationAsync(BulkOperationMode.Update, updateIndividualEnrollmentList)
                .ConfigureAwait(false);

            updateBulkEnrollmentResult.IsSuccessful.Should().BeTrue();

            // delete two enrollments in bulk
            retrievedEnrollment1 = await provisioningServiceClient.IndividualEnrollments.GetAsync(registrationId1).ConfigureAwait(false);
            retrievedEnrollment2 = await provisioningServiceClient.IndividualEnrollments.GetAsync(registrationId2).ConfigureAwait(false);
            var deleteIndividualEnrollmentList = new List<IndividualEnrollment> { retrievedEnrollment1, retrievedEnrollment2 };

            BulkEnrollmentOperationResult deleteBulkEnrollmentResult = await provisioningServiceClient
                .IndividualEnrollments
                .RunBulkOperationAsync(BulkOperationMode.Delete, deleteIndividualEnrollmentList)
                .ConfigureAwait(false);

            deleteBulkEnrollmentResult.IsSuccessful.Should().BeTrue();
        }

        private static async Task ProvisioningServiceClient_GetIndividualEnrollmentAttestation(AttestationMechanismType attestationType)
        {
            using var provisioningServiceClient = new ProvisioningServiceClient(TestConfiguration.Provisioning.ConnectionString);
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
                        null)
                    .ConfigureAwait(false);

                AttestationMechanism attestationMechanism = null;
                await RetryOperationHelper
                    .RunWithProvisioningServiceRetryAsync(
                        async () =>
                        {
                            attestationMechanism = await provisioningServiceClient.IndividualEnrollments.GetAttestationAsync(individualEnrollment.RegistrationId);
                        },
                        s_provisioningServiceRetryPolicy,
                        CancellationToken.None)
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
                else if (attestationType == AttestationMechanismType.X509)
                {
                    attestationMechanism.Type.Should().Be(AttestationMechanismType.X509);

                    var x509Attestation = (X509Attestation)attestationMechanism.GetAttestation();
                    x509Attestation.GetPrimaryX509CertificateInfo().Sha1Thumbprint.Should().Be(((X509Attestation)individualEnrollment.Attestation).GetPrimaryX509CertificateInfo().Sha1Thumbprint);
                    x509Attestation.GetSecondaryX509CertificateInfo().Sha1Thumbprint.Should().Be(((X509Attestation)individualEnrollment.Attestation).GetSecondaryX509CertificateInfo().Sha1Thumbprint);
                }
            }
            finally
            {
                if (individualEnrollment != null)
                {
                    await DeleteCreatedEnrollmentAsync(provisioningServiceClient, individualEnrollment.RegistrationId);
                }
            }
        }

        /// <summary>
        /// Attempts to query all enrollments using a provisioning service client instance
        /// </summary>
        /// <param name="proxyServerAddress">The address of the proxy to be used, or null/empty if no proxy should be used</param>
        /// <returns>If the query succeeded, otherwise this method will throw</returns>
        private static async Task ProvisioningServiceClient_IndividualEnrollments_Query_Ok(string proxyServerAddress)
        {
            var options = new ProvisioningServiceClientOptions();

            if (!string.IsNullOrWhiteSpace(proxyServerAddress))
            {
                options.ProvisioningServiceHttpSettings.Proxy = new WebProxy(proxyServerAddress);
            }
            using var provisioningServiceClient = new ProvisioningServiceClient(TestConfiguration.Provisioning.ConnectionString, options);

            string queryString = "SELECT * FROM enrollments";
            IAsyncEnumerable<IndividualEnrollment> query = provisioningServiceClient.IndividualEnrollments.CreateQuery(queryString);
            await foreach (IndividualEnrollment enrollment in query)
            {
                // Just checking that the returned type was, in fact, an individual enrollment and that deserialization
                // of the always-present fields works.
                enrollment.RegistrationId.Should().NotBeNull();
                enrollment.Attestation.Should().NotBeNull();
                enrollment.AllocationPolicy.Should().NotBeNull();
                enrollment.ReprovisionPolicy.Should().NotBeNull();
            }
        }

        private static async Task ProvisioningServiceClient_IndividualEnrollments_CreateOrUpdate_Ok(AttestationMechanismType attestationType, bool update = default, bool forceUpdate = default)
        {
            await ProvisioningServiceClient_IndividualEnrollments_CreateOrUpdate_Ok(
                    attestationType,
                    null,
                    AllocationPolicy.Hashed,
                    null,
                    update,
                    forceUpdate)
                .ConfigureAwait(false);
        }

        private static async Task ProvisioningServiceClient_IndividualEnrollments_CreateOrUpdate_Ok(
           AttestationMechanismType attestationType,
           ReprovisionPolicy reprovisionPolicy,
           AllocationPolicy allocationPolicy,
           CustomAllocationDefinition customAllocationDefinition,
           bool update,
           bool forceUpdate)
        {
            using var provisioningServiceClient = new ProvisioningServiceClient(TestConfiguration.Provisioning.ConnectionString);
            string registrationId = AttestationTypeToString(attestationType) + "-" + Guid.NewGuid();

            IndividualEnrollment createdIndividualEnrollment = null;

            try
            {
                createdIndividualEnrollment = await CreateIndividualEnrollmentAsync(
                        provisioningServiceClient,
                        registrationId,
                        attestationType,
                        null,
                        reprovisionPolicy,
                        allocationPolicy,
                        customAllocationDefinition)
                    .ConfigureAwait(false);

                IndividualEnrollment retrievedIndividualEnrollment = null;
                await RetryOperationHelper
                    .RunWithProvisioningServiceRetryAsync(
                        async () =>
                        {
                            retrievedIndividualEnrollment = await provisioningServiceClient.IndividualEnrollments.GetAsync(createdIndividualEnrollment.RegistrationId).ConfigureAwait(false);
                        },
                        s_provisioningServiceRetryPolicy,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                if (retrievedIndividualEnrollment == null)
                {
                    throw new ArgumentException($"The individual enrollment with registration Id {createdIndividualEnrollment.RegistrationId} could not retrieved, exiting test.");
                }

                retrievedIndividualEnrollment.ProvisioningStatus.Should().Be(ProvisioningStatus.Enabled);

                if (reprovisionPolicy != null)
                {
                    retrievedIndividualEnrollment.ReprovisionPolicy.UpdateHubAssignment.Should().Be(reprovisionPolicy.UpdateHubAssignment);
                    retrievedIndividualEnrollment.ReprovisionPolicy.MigrateDeviceData.Should().Be(reprovisionPolicy.MigrateDeviceData);
                }

                if (customAllocationDefinition != null)
                {
                    retrievedIndividualEnrollment.CustomAllocationDefinition.WebhookUrl.Should().Be(customAllocationDefinition.WebhookUrl);
                    retrievedIndividualEnrollment.CustomAllocationDefinition.ApiVersion.Should().Be(customAllocationDefinition.ApiVersion);
                }

                //allocation policy is never null
                retrievedIndividualEnrollment.AllocationPolicy.Should().Be(allocationPolicy);

                if (update)
                {
                    retrievedIndividualEnrollment.Capabilities = new InitialTwinCapabilities { IsIotEdge = true };

                    if (forceUpdate)
                    {
                        retrievedIndividualEnrollment.ETag = ETag.All;
                    }

                    IndividualEnrollment updatedIndividualEnrollment = null;
                    await RetryOperationHelper
                        .RunWithProvisioningServiceRetryAsync(
                            async () =>
                            {
                                updatedIndividualEnrollment = await provisioningServiceClient.IndividualEnrollments.CreateOrUpdateAsync(retrievedIndividualEnrollment).ConfigureAwait(false);
                            },
                            s_provisioningServiceRetryPolicy,
                            CancellationToken.None)
                        .ConfigureAwait(false);

                    if (updatedIndividualEnrollment == null)
                    {
                        throw new ArgumentException($"The individual enrollment with registration Id {retrievedIndividualEnrollment.RegistrationId} could not updated, exiting test.");
                    }

                    updatedIndividualEnrollment.ProvisioningStatus.Should().Be(ProvisioningStatus.Enabled);
                    updatedIndividualEnrollment.RegistrationId.Should().Be(retrievedIndividualEnrollment.RegistrationId);
                }
            }
            finally
            {
                if (createdIndividualEnrollment != null)
                {
                    await DeleteCreatedEnrollmentAsync(provisioningServiceClient, createdIndividualEnrollment.RegistrationId);
                }
            }
        }

        private static async Task<IndividualEnrollment> CreateIndividualEnrollmentAsync(
            ProvisioningServiceClient provisioningServiceClient,
            string registrationId,
            AttestationMechanismType attestationType,
            X509Certificate2 authenticationCertificate,
            ReprovisionPolicy reprovisionPolicy,
            AllocationPolicy allocationPolicy,
            CustomAllocationDefinition customAllocationDefinition)
        {
            Attestation attestation;
            IndividualEnrollment individualEnrollment;
            IndividualEnrollment createdEnrollment = null;

            switch (attestationType)
            {
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
                AllocationPolicy = allocationPolicy,
                ReprovisionPolicy = reprovisionPolicy,
                CustomAllocationDefinition = customAllocationDefinition,
            };

            await RetryOperationHelper
                .RunWithProvisioningServiceRetryAsync(
                    async () =>
                    {
                        createdEnrollment = await provisioningServiceClient.IndividualEnrollments
                            .CreateOrUpdateAsync(individualEnrollment)
                            .ConfigureAwait(false);
                    },
                    s_provisioningServiceRetryPolicy,
                    CancellationToken.None)
                .ConfigureAwait(false);

            createdEnrollment.Should().NotBeNull($"The enrollment entry with registration Id {registrationId} could not be created; exiting test.");
            return createdEnrollment;
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

        public static async Task DeleteCreatedEnrollmentAsync(
            ProvisioningServiceClient provisioningServiceClient,
            string registrationId)
        {
            try
            {
                await RetryOperationHelper
                    .RunWithProvisioningServiceRetryAsync(
                        async () =>
                        {
                            await provisioningServiceClient.IndividualEnrollments.DeleteAsync(registrationId).ConfigureAwait(false);
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