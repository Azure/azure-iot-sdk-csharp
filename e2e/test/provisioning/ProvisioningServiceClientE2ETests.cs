// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.Provisioning.Service;
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

        private static readonly IIotHubClientRetryPolicy s_provisioningServiceRetryPolicy = new ProvisioningServiceRetryPolicy();

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
        public async Task ProvisioningServiceClient_SymmetricKey_IndividualEnrollments_Create_HttpWithProxy_Ok()
        {
            await ProvisioningServiceClient_IndividualEnrollments_Create_Ok(s_proxyServerAddress, AttestationMechanismType.SymmetricKey).ConfigureAwait(false);
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
        public async Task ProvisioningServiceClient_GetEnrollmentGroupAttestation_SymmetricKey()
        {
            await ProvisioningServiceClient_GetEnrollmentGroupAttestation(AttestationMechanismType.SymmetricKey);
        }

        [TestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_GetIndividualEnrollmentAsync_Fails()
        {
            using var provisioningServiceClient = new ProvisioningServiceClient(TestConfiguration.Provisioning.ConnectionString);

            // act
            Func<Task> act = async () => await provisioningServiceClient.IndividualEnrollments.GetAsync("invalid-registration-id").ConfigureAwait(false);

            // assert
            var error = await act.Should().ThrowAsync<ProvisioningServiceException>();
            error.And.StatusCode.Should().Be(HttpStatusCode.NotFound);
            error.And.ErrorCode.Should().Be(404201);
            error.And.IsTransient.Should().BeFalse();
        }

        [TestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_GetEnrollmentGroupAsync_Fails()
        {
            using var provisioningServiceClient = new ProvisioningServiceClient(TestConfiguration.Provisioning.ConnectionString);

            // act
            Func<Task> act = async () => await provisioningServiceClient.EnrollmentGroups.GetAsync("invalid-registration-id").ConfigureAwait(false);

            // assert
            var error = await act.Should().ThrowAsync<ProvisioningServiceException>();
            error.And.StatusCode.Should().Be(HttpStatusCode.NotFound);
            error.And.ErrorCode.Should().Be(404204);
            error.And.IsTransient.Should().BeFalse();
        }

        public async Task ProvisioningServiceClient_GetIndividualEnrollmentAttestation(AttestationMechanismType attestationType)
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
                        null,
                        null,
                        null)
                    .ConfigureAwait(false);

                AttestationMechanism attestationMechanism = null;
                await RetryOperationHelper
                    .RunWithHubClientRetryAsync(
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
                    await DeleteCreatedEnrollmentAsync(EnrollmentType.Individual, individualEnrollment.RegistrationId, null);
                }
            }
        }

        public async Task ProvisioningServiceClient_GetEnrollmentGroupAttestation(AttestationMechanismType attestationType)
        {
            using var provisioningServiceClient = new ProvisioningServiceClient(TestConfiguration.Provisioning.ConnectionString);
            string groupId = AttestationTypeToString(attestationType) + "-" + Guid.NewGuid();
            EnrollmentGroup enrollmentGroup = null;

            try
            {
                enrollmentGroup = await CreateEnrollmentGroupAsync(provisioningServiceClient, attestationType, groupId, null, AllocationPolicy.Static, null, null, null);

                AttestationMechanism attestationMechanism = null;
                await RetryOperationHelper
                    .RunWithHubClientRetryAsync(
                        async () =>
                        {
                            attestationMechanism = await provisioningServiceClient.EnrollmentGroups.GetAttestationAsync(enrollmentGroup.EnrollmentGroupId);
                        },
                        s_provisioningServiceRetryPolicy,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                if (attestationMechanism == null)
                {
                    throw new ArgumentException($"The attestation mechanism for enrollment with group Id {enrollmentGroup.EnrollmentGroupId} could not retrieved, exiting test.");
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
                    await DeleteCreatedEnrollmentAsync(EnrollmentType.Group, null, enrollmentGroup.EnrollmentGroupId);
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
            using ProvisioningServiceClient provisioningServiceClient = CreateProvisioningService(proxyServerAddress);
            var queryString = "SELECT * FROM enrollments";
            Query query = provisioningServiceClient.IndividualEnrollments.CreateQuery(queryString);
            while (query.HasNext())
            {
                QueryResult queryResult = await query.NextAsync().ConfigureAwait(false);
                Assert.AreEqual(queryResult.QueryType, QueryResultType.Enrollment);
            }
        }

        public async Task ProvisioningServiceClient_IndividualEnrollments_Create_Ok(string proxyServerAddress, AttestationMechanismType attestationType)
        {
            await ProvisioningServiceClient_IndividualEnrollments_Create_Ok(
                    proxyServerAddress,
                    attestationType,
                    null,
                    AllocationPolicy.Hashed,
                    null,
                    null)
                .ConfigureAwait(false);
        }

        public async Task ProvisioningServiceClient_IndividualEnrollments_Create_Ok(
            string proxyServerAddress,
            AttestationMechanismType attestationType,
            ReprovisionPolicy reprovisionPolicy,
            AllocationPolicy allocationPolicy,
            CustomAllocationDefinition customAllocationDefinition,
            IList<string> iotHubsToProvisionTo)
        {
            using ProvisioningServiceClient provisioningServiceClient = CreateProvisioningService(proxyServerAddress);
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
                    .RunWithHubClientRetryAsync(
                        async () =>
                        {
                            individualEnrollmentResult = await provisioningServiceClient.IndividualEnrollments.GetAsync(individualEnrollment.RegistrationId).ConfigureAwait(false);
                        },
                        s_provisioningServiceRetryPolicy,
                        CancellationToken.None)
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
                    await DeleteCreatedEnrollmentAsync(EnrollmentType.Individual, individualEnrollment.RegistrationId, null);
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
            IList<string> iothubs)
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
                            null)
                        .ConfigureAwait(false);

                    EnrollmentGroup enrollmentGroupResult = null;
                    await RetryOperationHelper
                        .RunWithHubClientRetryAsync(
                            async () =>
                            {
                                enrollmentGroupResult = await provisioningServiceClient.EnrollmentGroups.GetAsync(enrollmentGroup.EnrollmentGroupId).ConfigureAwait(false);
                            },
                            s_provisioningServiceRetryPolicy,
                            CancellationToken.None)
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
            IList<string> iotHubsToProvisionTo,
            Devices.Provisioning.Service.ProvisioningClientCapabilities capabilities)
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
                Capabilities = capabilities,
                AllocationPolicy = allocationPolicy,
                ReprovisionPolicy = reprovisionPolicy,
                CustomAllocationDefinition = customAllocationDefinition,
                IotHubs = iotHubsToProvisionTo,
            };

            await RetryOperationHelper
                .RunWithHubClientRetryAsync(
                    async () =>
                    {
                        createdEnrollment = await provisioningServiceClient.IndividualEnrollments
                            .CreateOrUpdateAsync(individualEnrollment)
                            .ConfigureAwait(false);
                    },
                    s_provisioningServiceRetryPolicy,
                    CancellationToken.None)
                .ConfigureAwait(false);

            if (createdEnrollment == null)
            {
                throw new ArgumentException($"The enrollment entry with registration Id {registrationId} could not be created; exiting test.");
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
            IList<string> iothubs,
            Devices.Provisioning.Service.ProvisioningClientCapabilities capabilities)
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
               .RunWithHubClientRetryAsync(
                   async () =>
                   {
                       createdEnrollmentGroup = await provisioningServiceClient.EnrollmentGroups.CreateOrUpdateAsync(enrollmentGroup).ConfigureAwait(false);
                   },
                   s_provisioningServiceRetryPolicy,
                    CancellationToken.None)
               .ConfigureAwait(false);

            if (createdEnrollmentGroup == null)
            {
                throw new ArgumentException($"The enrollment entry with group Id {groupId} could not be created, exiting test.");
            }

            return createdEnrollmentGroup;
        }

        public static async Task DeleteCreatedEnrollmentAsync(
            EnrollmentType? enrollmentType,
            string registrationId,
            string groupId)
        {
            using ProvisioningServiceClient dpsClient = CreateProvisioningService();

            try
            {
                if (enrollmentType == EnrollmentType.Individual)
                {
                    await RetryOperationHelper
                        .RunWithHubClientRetryAsync(
                            async () =>
                            {
                                await dpsClient.IndividualEnrollments.DeleteAsync(registrationId).ConfigureAwait(false);
                            },
                            s_provisioningServiceRetryPolicy)
                        .ConfigureAwait(false);
                }
                else if (enrollmentType == EnrollmentType.Group)
                {
                    await RetryOperationHelper
                        .RunWithHubClientRetryAsync(
                            async () =>
                            {
                                await dpsClient.EnrollmentGroups.DeleteAsync(groupId).ConfigureAwait(false);
                            },
                            s_provisioningServiceRetryPolicy)
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
            var options = new ProvisioningServiceClientOptions();
            var transportSettings = options.ProvisioningServiceHttpSettings;

            if (!string.IsNullOrWhiteSpace(proxyServerAddress))
            {
                transportSettings.Proxy = new WebProxy(proxyServerAddress);
            }

            return new ProvisioningServiceClient(TestConfiguration.Provisioning.ConnectionString, options);
        }

        /// <summary>
        /// Returns the registrationId compliant name for the provided attestation type
        /// </summary>
        public static string AttestationTypeToString(AttestationMechanismType attestationType)
        {
            return attestationType switch
            {
                AttestationMechanismType.SymmetricKey => "symmetrickey",
                AttestationMechanismType.X509 => "x509",
                _ => throw new NotSupportedException("Test code has not been written for testing this attestation type yet"),
            };
        }
    }
}
