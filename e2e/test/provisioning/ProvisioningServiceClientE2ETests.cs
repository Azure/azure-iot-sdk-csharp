// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Provisioning
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("DPS")]
    public partial class ProvisioningServiceClientE2ETests : E2EMsTestBase
    {
        private static readonly string s_proxyServerAddress = TestConfiguration.IotHub.ProxyServerAddress;
        private static readonly string s_devicePrefix = $"{nameof(ProvisioningServiceClientE2ETests)}_";

        private static readonly ProvisioningServiceExponentialBackoffRetryPolicy s_provisioningServiceRetryPolicy = new(20, TimeSpan.FromSeconds(3), true);

        public enum EnrollmentType
        {
            Individual,
            Group,
        }

        public static async Task DeleteCreatedEnrollmentAsync(
            EnrollmentType? enrollmentType,
            string registrationId,
            string groupId)
        {
            using ProvisioningServiceClient provisioningServiceClient = CreateProvisioningService();

            try
            {
                if (enrollmentType == EnrollmentType.Individual)
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
                else if (enrollmentType == EnrollmentType.Group)
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

            if (!string.IsNullOrWhiteSpace(proxyServerAddress))
            {
                options.ProvisioningServiceHttpSettings.Proxy = new WebProxy(proxyServerAddress);
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