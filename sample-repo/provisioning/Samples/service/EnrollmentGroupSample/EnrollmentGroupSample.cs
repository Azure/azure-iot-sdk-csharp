// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    public class EnrollmentGroupSample
    {
        private const string EnrollmentGroupId = "enrollmentgrouptest";
        ProvisioningServiceClient _provisioningServiceClient;
        X509Certificate2 _groupIssuerCertificate;

        public EnrollmentGroupSample(ProvisioningServiceClient provisioningServiceClient, X509Certificate2 groupIssuerCertificate)
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
            QuerySpecification querySpecification = new QuerySpecification("SELECT * FROM enrollmentGroups");
            using (Query query = _provisioningServiceClient.CreateEnrollmentGroupQuery(querySpecification))
            {
                while (query.HasNext())
                {
                    Console.WriteLine("\nQuerying the next enrollmentGroups...");
                    QueryResult queryResult = await query.NextAsync().ConfigureAwait(false);
                    Console.WriteLine(queryResult);

                    foreach (EnrollmentGroup group in queryResult.Items)
                    {
                        await EnumerateRegistrationsInGroup(querySpecification, group).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task EnumerateRegistrationsInGroup(QuerySpecification querySpecification, EnrollmentGroup group)
        {
            Console.WriteLine($"\nCreating a query for registrations within group '{group.EnrollmentGroupId}'...");
            using (Query registrationQuery = _provisioningServiceClient.CreateEnrollmentGroupRegistrationStateQuery(querySpecification, group.EnrollmentGroupId))
            {
                Console.WriteLine($"\nQuerying the next registrations within group '{group.EnrollmentGroupId}'...");
                QueryResult registrationQueryResult = await registrationQuery.NextAsync().ConfigureAwait(false);
                Console.WriteLine(registrationQueryResult);
            }
        }

        public async Task CreateEnrollmentGroupAsync()
        {
            Console.WriteLine("\nCreating a new enrollmentGroup...");
            Attestation attestation = X509Attestation.CreateFromRootCertificates(_groupIssuerCertificate);
            EnrollmentGroup enrollmentGroup =
                    new EnrollmentGroup(
                            EnrollmentGroupId,
                            attestation);
            Console.WriteLine(enrollmentGroup);

            Console.WriteLine("\nAdding new enrollmentGroup...");
            EnrollmentGroup enrollmentGroupResult =
                await _provisioningServiceClient.CreateOrUpdateEnrollmentGroupAsync(enrollmentGroup).ConfigureAwait(false);
            Console.WriteLine("\nEnrollmentGroup created with success.");
            Console.WriteLine(enrollmentGroupResult);
        }

        public async Task GetEnrollmentGroupInfoAsync()
        {
            Console.WriteLine("\nGetting the enrollmentGroup information...");
            EnrollmentGroup getResult =
                await _provisioningServiceClient.GetEnrollmentGroupAsync(EnrollmentGroupId).ConfigureAwait(false);
            Console.WriteLine(getResult);
        }

        public async Task DeleteEnrollmentGroupAsync()
        {
            Console.WriteLine("\nDeleting the enrollmentGroup...");
            await _provisioningServiceClient.DeleteEnrollmentGroupAsync(EnrollmentGroupId).ConfigureAwait(false);
        }
    }
}
