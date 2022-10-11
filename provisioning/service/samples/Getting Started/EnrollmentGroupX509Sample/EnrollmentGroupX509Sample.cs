// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    internal class EnrollmentGroupX509Sample
    {
        private const string EnrollmentGroupId = "enrollmentgrouptest";
        private readonly ProvisioningServiceClient _provisioningServiceClient;
        private readonly X509Certificate2 _groupIssuerCertificate;

        public EnrollmentGroupX509Sample(ProvisioningServiceClient provisioningServiceClient, X509Certificate2 groupIssuerCertificate)
        {
            _provisioningServiceClient = provisioningServiceClient;
            _groupIssuerCertificate = groupIssuerCertificate;
        }

        public async Task RunSampleAsync()
        {
            await QueryEnrollmentGroupAsync().ConfigureAwait(false);
            await CreateEnrollmentGroupAsync().ConfigureAwait(false);
            await GetEnrollmentGroupInfoAsync().ConfigureAwait(false);
            await DeleteEnrollmentGroupAsync().ConfigureAwait(false);
        }

        public async Task QueryEnrollmentGroupAsync()
        {
            Console.WriteLine("\nCreating a query for enrollmentGroups...");
            string queryText = "SELECT * FROM enrollmentGroups";
            Query query = _provisioningServiceClient.EnrollmentGroups.CreateQuery(queryText);
            while (query.HasNext())
            {
                Console.WriteLine("\nQuerying the next enrollmentGroups...");
                QueryResult queryResult = await query.NextAsync().ConfigureAwait(false);
                Console.WriteLine(queryResult);

                foreach (EnrollmentGroup group in queryResult.Items.Cast<EnrollmentGroup>())
                {
                    await EnumerateRegistrationsInGroupAsync(queryText, group);
                }
            }
        }

        private async Task EnumerateRegistrationsInGroupAsync(string queryText, EnrollmentGroup group)
        {
            Console.WriteLine($"\nCreating a query for registrations within group '{group.EnrollmentGroupId}'...");
            Query registrationQuery = _provisioningServiceClient.DeviceRegistrationStates.CreateQuery(queryText, group.EnrollmentGroupId);
            Console.WriteLine($"\nQuerying the next registrations within group '{group.EnrollmentGroupId}'...");
            QueryResult registrationQueryResult = await registrationQuery.NextAsync();
        }

        public async Task CreateEnrollmentGroupAsync()
        {
            Console.WriteLine("\nCreating a new enrollmentGroup...");
            Attestation attestation = X509Attestation.CreateFromRootCertificates(_groupIssuerCertificate);
            var enrollmentGroup = new EnrollmentGroup(EnrollmentGroupId, attestation);
            Console.WriteLine(enrollmentGroup);

            Console.WriteLine("\nAdding new enrollmentGroup...");
            EnrollmentGroup enrollmentGroupResult = await _provisioningServiceClient.EnrollmentGroups.CreateOrUpdateAsync(enrollmentGroup);
            Console.WriteLine("\nEnrollmentGroup created with success.");
            Console.WriteLine(enrollmentGroupResult);
        }

        public async Task GetEnrollmentGroupInfoAsync()
        {
            Console.WriteLine("\nGetting the enrollmentGroup information...");
            EnrollmentGroup getResult = await _provisioningServiceClient.EnrollmentGroups.GetAsync(EnrollmentGroupId);
            Console.WriteLine(getResult);
        }

        public async Task DeleteEnrollmentGroupAsync()
        {
            Console.WriteLine("\nDeleting the enrollmentGroup...");
            await _provisioningServiceClient.EnrollmentGroups.DeleteAsync(EnrollmentGroupId);
        }
    }
}
