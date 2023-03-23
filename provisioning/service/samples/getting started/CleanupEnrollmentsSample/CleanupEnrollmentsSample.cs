// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    internal class CleanupEnrollmentsSample
    {
        private readonly ProvisioningServiceClient _provisioningServiceClient;
        private static int s_individualEnrollmentsDeleted;
        private static int s_enrollmentGroupsDeleted;

        private readonly List<string> _individualEnrollmentsToBeRetained = new()
        {
            "Save_iothubx509device1",
            "Save_SymmetricKeySampleIndividualEnrollment"
        };

        private readonly List<string> _groupEnrollmentsToBeRetained = new()
        {
            "Save_group-certificate-x509",
            "Save_Group1"
        };

        public CleanupEnrollmentsSample(ProvisioningServiceClient provisioningServiceClient)
        {
            _provisioningServiceClient = provisioningServiceClient;
            s_individualEnrollmentsDeleted = 0;
            s_enrollmentGroupsDeleted = 0;
        }

        public async Task RunSampleAsync()
        {
            await QueryAndDeleteIndividualEnrollmentsAsync();
            Console.WriteLine($"Individual enrollments deleted: {s_individualEnrollmentsDeleted}");
            await QueryAndDeleteEnrollmentGroupsAsync();
            Console.WriteLine($"Enrollment groups deleted: {s_enrollmentGroupsDeleted}");
        }

        private async Task QueryAndDeleteIndividualEnrollmentsAsync()
        {
            Console.WriteLine("Creating a query for enrollments...");
            AsyncPageable<IndividualEnrollment> query = _provisioningServiceClient.IndividualEnrollments.CreateQuery("SELECT * FROM enrollments");
            var individualEnrollments = new List<IndividualEnrollment>();
            await foreach (IndividualEnrollment enrollment in query)
            {
                Console.WriteLine("Querying the next enrollments...");
                if (!_individualEnrollmentsToBeRetained.Contains(enrollment.RegistrationId, StringComparer.OrdinalIgnoreCase))
                {
                    individualEnrollments.Add(enrollment);
                    Console.WriteLine($"Individual enrollment to be deleted: {enrollment.RegistrationId}");
                    s_individualEnrollmentsDeleted++;
                }

                await Task.Delay(1000);
            }

            if (individualEnrollments.Count > 0)
            {
                await DeleteBulkIndividualEnrollmentsAsync(individualEnrollments);
            }
        }

        private async Task QueryAndDeleteEnrollmentGroupsAsync()
        {
            Console.WriteLine("Creating a query for enrollment groups...");
            AsyncPageable<EnrollmentGroup> query = _provisioningServiceClient.EnrollmentGroups.CreateQuery("SELECT * FROM enrollmentGroups");
            await foreach (EnrollmentGroup enrollment in query)
            {
                Console.WriteLine("Querying the next enrollment groups...");
                if (!_groupEnrollmentsToBeRetained.Contains(enrollment.Id, StringComparer.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Enrollment group to be deleted: {enrollment.Id}");
                    s_enrollmentGroupsDeleted++;
                    await _provisioningServiceClient.EnrollmentGroups.DeleteAsync(enrollment.Id);
                }
            }
        }

        private async Task DeleteBulkIndividualEnrollmentsAsync(List<IndividualEnrollment> individualEnrollments)
        {
            Console.WriteLine("Deleting the set of individualEnrollments...");
            BulkEnrollmentOperationResult bulkEnrollmentOperationResult = await _provisioningServiceClient
                .IndividualEnrollments
                .RunBulkOperationAsync(BulkOperationMode.Delete, individualEnrollments);
            Console.WriteLine(JsonConvert.SerializeObject(bulkEnrollmentOperationResult));
        }
    }
}
