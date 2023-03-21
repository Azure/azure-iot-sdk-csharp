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
    public class ProvisioningServiceDeviceRegistrationTests : E2EMsTestBase
    {
        private static readonly string s_proxyServerAddress = TestConfiguration.IotHub.ProxyServerAddress;
        private static readonly string s_devicePrefix = $"{nameof(ProvisioningServiceIndividualEnrollmentTests)}_";

        private static readonly ProvisioningServiceExponentialBackoffRetryPolicy s_provisioningServiceRetryPolicy = new(20, TimeSpan.FromSeconds(3), true);

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_DeviceRegistrationState_Query_Ok()
        {
            using var provisioningServiceClient = new ProvisioningServiceClient(TestConfiguration.Provisioning.ConnectionString);

            // Create an enrollment group so that the query is guaranteed to return at least one entry
            string enrollmentGroupId = Guid.NewGuid().ToString();
            await provisioningServiceClient.EnrollmentGroups.CreateOrUpdateAsync(new EnrollmentGroup(enrollmentGroupId, new SymmetricKeyAttestation()));

            try
            {
                string queryString = "SELECT * FROM enrollmentGroups";
                IAsyncEnumerable<DeviceRegistrationState> query = provisioningServiceClient.DeviceRegistrationStates.CreateEnrollmentGroupQuery(queryString, enrollmentGroupId);
                await foreach (DeviceRegistrationState state in query)
                {
                    state.LastUpdatedOnUtc.Should().NotBeNull();
                }
            }
            finally
            {
                try
                {
                    await provisioningServiceClient.EnrollmentGroups.DeleteAsync(enrollmentGroupId);
                }
                catch (Exception e)
                {
                    // Failed to cleanup after the test, but don't fail the test because of this
                    VerboseTestLogger.WriteLine($"Failed to clean up enrollment group due to {e}");
                }
            }
        }
    }
}