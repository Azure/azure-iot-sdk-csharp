// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    internal class CleanupEnrollmentsSample
    {
        private readonly ProvisioningServiceClient _provisioningServiceClient;
        private readonly ILogger _logger;
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

        public CleanupEnrollmentsSample(ProvisioningServiceClient provisioningServiceClient, ILogger logger)
        {
            _provisioningServiceClient = provisioningServiceClient;
            s_individualEnrollmentsDeleted = 0;
            s_enrollmentGroupsDeleted = 0;
            _logger = logger;
        }

        public async Task RunSampleAsync()
        {
            await QueryAndDeleteIndividualEnrollmentsAsync();
            _logger.LogInformation($"Individual enrollments deleted: {s_individualEnrollmentsDeleted}");
            await QueryAndDeleteEnrollmentGroupsAsync();
            _logger.LogInformation($"Enrollment groups deleted: {s_enrollmentGroupsDeleted}");
        }

        private async Task QueryAndDeleteIndividualEnrollmentsAsync()
        {
            _logger.LogInformation("Creating a query for enrollments...");
            AsyncPageable<IndividualEnrollment> query = _provisioningServiceClient.IndividualEnrollments.CreateQuery("SELECT * FROM enrollments");
            var individualEnrollments = new List<IndividualEnrollment>();
            await foreach (IndividualEnrollment enrollment in query)
            {
                _logger.LogInformation("Querying the next enrollments...");
                if (!_individualEnrollmentsToBeRetained.Contains(enrollment.RegistrationId, StringComparer.OrdinalIgnoreCase))
                {
                    individualEnrollments.Add(enrollment);
                    _logger.LogInformation($"Individual enrollment to be deleted: {enrollment.RegistrationId}");
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
            _logger.LogInformation("Creating a query for enrollment groups...");
            AsyncPageable<EnrollmentGroup> query = _provisioningServiceClient.EnrollmentGroups.CreateQuery("SELECT * FROM enrollmentGroups");
            await foreach (EnrollmentGroup enrollment in query)
            {
                _logger.LogInformation("Querying the next enrollment groups...");
                if (!_groupEnrollmentsToBeRetained.Contains(enrollment.Id, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogInformation($"Enrollment group to be deleted: {enrollment.Id}");
                    s_enrollmentGroupsDeleted++;
                    await _provisioningServiceClient.EnrollmentGroups.DeleteAsync(enrollment.Id);
                }
            }
        }

        private async Task DeleteBulkIndividualEnrollmentsAsync(List<IndividualEnrollment> individualEnrollments)
        {
            _logger.LogInformation("Deleting the set of individualEnrollments...");
            BulkEnrollmentOperationResult bulkEnrollmentOperationResult = await _provisioningServiceClient
                .IndividualEnrollments
                .RunBulkOperationAsync(BulkOperationMode.Delete, individualEnrollments);

            if (!bulkEnrollmentOperationResult.IsSuccessful)
            {
                foreach (BulkEnrollmentOperationError error in bulkEnrollmentOperationResult.Errors)
                {
                    _logger.LogError($"Registration with Id {error.RegistrationId} failed with " +
                        $"error code {error.ErrorCode} and status {error.ErrorStatus}");
                }
            }
            else 
            {
                _logger.LogInformation("Bulk operation succeeded.");
            }
        }
    }
}
