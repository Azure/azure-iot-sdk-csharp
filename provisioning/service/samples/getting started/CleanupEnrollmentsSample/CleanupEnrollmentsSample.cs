﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    internal class CleanupEnrollmentsSample
    {
        // Maximum number of elements per query - DPS has a limit of 10.
        private const int QueryPageSize = 10;
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
            var querySpecification = new QuerySpecification("SELECT * FROM enrollments");
            using Query query = _provisioningServiceClient.CreateIndividualEnrollmentQuery(querySpecification, QueryPageSize);
            while (query.HasNext())
            {
                Console.WriteLine("Querying the next enrollments...");
                QueryResult queryResult = await query.NextAsync();
                IEnumerable<object> items = queryResult.Items;
                var individualEnrollments = new List<IndividualEnrollment>();
                foreach (IndividualEnrollment enrollment in items.Cast<IndividualEnrollment>())
                {
                    if (!_individualEnrollmentsToBeRetained.Contains(enrollment.RegistrationId, StringComparer.OrdinalIgnoreCase))
                    {
                        individualEnrollments.Add(enrollment);
                        Console.WriteLine($"Individual enrollment to be deleted: {enrollment.RegistrationId}");
                        s_individualEnrollmentsDeleted++;
                    }
                }
                if (individualEnrollments.Count > 0)
                {
                    await DeleteBulkIndividualEnrollmentsAsync(individualEnrollments);
                }

                await Task.Delay(1000);
            }
        }

        private async Task QueryAndDeleteEnrollmentGroupsAsync()
        {
            Console.WriteLine("Creating a query for enrollment groups...");
            var querySpecification = new QuerySpecification("SELECT * FROM enrollmentGroups");
            using Query query = _provisioningServiceClient.CreateEnrollmentGroupQuery(querySpecification, QueryPageSize);
            while (query.HasNext())
            {
                Console.WriteLine("Querying the next enrollment groups...");
                QueryResult queryResult = await query.NextAsync();
                IEnumerable<object> items = queryResult.Items;
                foreach (EnrollmentGroup enrollment in items.Cast<EnrollmentGroup>())
                {
                    if (!_groupEnrollmentsToBeRetained.Contains(enrollment.EnrollmentGroupId, StringComparer.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"Enrollment group to be deleted: {enrollment.EnrollmentGroupId}");
                        s_enrollmentGroupsDeleted++;
                        await _provisioningServiceClient.DeleteEnrollmentGroupAsync(enrollment.EnrollmentGroupId);
                    }
                }
            }
        }

        private async Task DeleteBulkIndividualEnrollmentsAsync(List<IndividualEnrollment> individualEnrollments)
        {
            Console.WriteLine("Deleting the set of individualEnrollments...");
            BulkEnrollmentOperationResult bulkEnrollmentOperationResult = await _provisioningServiceClient
                .RunBulkEnrollmentOperationAsync(BulkOperationMode.Delete, individualEnrollments);
            Console.WriteLine(bulkEnrollmentOperationResult);
        }
    }
}
