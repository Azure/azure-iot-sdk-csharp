// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    public class CleanupEnrollmentsSample
    {
        private ProvisioningServiceClient _provisioningServiceClient;
        // Maximum number of elements per query - DPS has a limit of 10.
        private const int QueryPageSize = 10;
        private static int _individualEnrollmentsDeleted;
        private static int _enrollmentGroupsDeleted;
        private List<string> individualEnrollmentsToBeRetained =
            new List<string>{
                "iothubx509device1"
            };
        private List<string> groupEnrollmentsToBeRetained =
            new List<string>{
                "group-certificate-x509",
                "group1"
            };

        public CleanupEnrollmentsSample(ProvisioningServiceClient provisioningServiceClient)
        {
            _provisioningServiceClient = provisioningServiceClient;
            _individualEnrollmentsDeleted = 0;
            _enrollmentGroupsDeleted = 0;
        }

        public async Task RunSampleAsync()
        {
            await QueryAndDeleteIndividualEnrollments().ConfigureAwait(false);
            Console.WriteLine($"Individual Enrollments Deleted: {_individualEnrollmentsDeleted}");
            await QueryAndDeleteEnrollmentGroups().ConfigureAwait(false);
            Console.WriteLine($"Enrollment Groups Deleted: {_enrollmentGroupsDeleted}");
        }

        private async Task QueryAndDeleteIndividualEnrollments()
        {
            Console.WriteLine("\nCreating a query for enrollments...");
            QuerySpecification querySpecification = new QuerySpecification("SELECT * FROM enrollments");
            using (Query query = _provisioningServiceClient.CreateIndividualEnrollmentQuery(querySpecification, QueryPageSize))
            {
                while (query.HasNext())
                {
                    Console.WriteLine("\nQuerying the next enrollments...");
                    QueryResult queryResult = await query.NextAsync().ConfigureAwait(false);
                    var items = queryResult.Items;
                    List<IndividualEnrollment> individualEnrollments = new List<IndividualEnrollment>();
                    foreach (IndividualEnrollment enrollment in items)
                    {
                        if (!individualEnrollmentsToBeRetained.Contains(enrollment.RegistrationId, StringComparer.OrdinalIgnoreCase))
                        {
                            individualEnrollments.Add(enrollment);
                            Console.WriteLine($"Individual Enrollment to be deleted: {enrollment.RegistrationId}");
                            _individualEnrollmentsDeleted++;
                        }
                    }
                    if (individualEnrollments.Count > 0)
                    {
                        await DeleteBulkIndividualEnrollments(individualEnrollments).ConfigureAwait(false);
                    }

                    await Task.Delay(1000).ConfigureAwait(false);
                }
            }
        }

        private async Task QueryAndDeleteEnrollmentGroups()
        {
            Console.WriteLine("\nCreating a query for enrollmentGroups...");
            QuerySpecification querySpecification = new QuerySpecification("SELECT * FROM enrollmentGroups");
            using (Query query = _provisioningServiceClient.CreateEnrollmentGroupQuery(querySpecification, QueryPageSize))
            {
                while (query.HasNext())
                {
                    Console.WriteLine("\nQuerying the next enrollmentGroups...");
                    QueryResult queryResult = await query.NextAsync().ConfigureAwait(false);
                    var items = queryResult.Items;
                    foreach (EnrollmentGroup enrollment in items)
                    {
                        if (!groupEnrollmentsToBeRetained.Contains(enrollment.EnrollmentGroupId, StringComparer.OrdinalIgnoreCase))
                        {
                            Console.WriteLine($"EnrollmentGroup to be deleted: {enrollment.EnrollmentGroupId}");
                            _enrollmentGroupsDeleted++;
                            await _provisioningServiceClient.DeleteEnrollmentGroupAsync(enrollment.EnrollmentGroupId).ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        private async Task DeleteBulkIndividualEnrollments(List<IndividualEnrollment> individualEnrollments)
        {
            Console.WriteLine("\nDeleting the set of individualEnrollments...");
            BulkEnrollmentOperationResult bulkEnrollmentOperationResult =
                await _provisioningServiceClient.RunBulkEnrollmentOperationAsync(BulkOperationMode.Delete, individualEnrollments).ConfigureAwait(false);
            Console.WriteLine(bulkEnrollmentOperationResult);
        }
    }
}
