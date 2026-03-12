// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    internal class EnrollmentGroupSample
    {
        private static readonly string s_enrollmentGroupId = $"enrollmentgrouptest-{Guid.NewGuid()}";
        private readonly ProvisioningServiceClient _provisioningServiceClient;
        private readonly ILogger _logger;

        public EnrollmentGroupSample(ProvisioningServiceClient provisioningServiceClient, ILogger logger)
        {
            _provisioningServiceClient = provisioningServiceClient;
            _logger = logger;
        }

        public async Task RunSampleAsync()
        {
            await CreateEnrollmentGroupAsync().ConfigureAwait(false);
            await GetEnrollmentGroupInfoAsync().ConfigureAwait(false);
            await QueryEnrollmentGroupAsync().ConfigureAwait(false);
            await DeleteEnrollmentGroupAsync().ConfigureAwait(false);
        }

        public async Task CreateEnrollmentGroupAsync()
        {
            Attestation attestation = new SymmetricKeyAttestation();
            var enrollmentGroup = new EnrollmentGroup(s_enrollmentGroupId, attestation);
            _logger.LogInformation($"Creating an enrollment group: {JsonConvert.SerializeObject(enrollmentGroup)}");

            EnrollmentGroup group = await _provisioningServiceClient.EnrollmentGroups.CreateOrUpdateAsync(enrollmentGroup);
            _logger.LogInformation($"Created {group.Id}: {JsonConvert.SerializeObject(group)}");
        }

        public async Task GetEnrollmentGroupInfoAsync()
        {
            _logger.LogInformation("Getting the enrollment group information...");
            EnrollmentGroup group = await _provisioningServiceClient.EnrollmentGroups.GetAsync(s_enrollmentGroupId);
            _logger.LogInformation($"Got {group.Id}: {JsonConvert.SerializeObject(group)}");
        }

        public async Task QueryEnrollmentGroupAsync()
        {
            string queryText = "SELECT * FROM enrollmentGroups";
            _logger.LogInformation($"Running a query for enrollment groups: {queryText}");
            AsyncPageable<EnrollmentGroup> query = _provisioningServiceClient.EnrollmentGroups.CreateQuery(queryText);

            await foreach (EnrollmentGroup enrollmentGroup in query)
            {
                _logger.LogInformation($"Found enrollment group {enrollmentGroup.Id} is {enrollmentGroup.ProvisioningStatus}.");
                await EnumerateRegistrationsInGroupAsync(queryText, enrollmentGroup);
            }
        }

        private async Task EnumerateRegistrationsInGroupAsync(string queryText, EnrollmentGroup group)
        {
            _logger.LogInformation($"Registrations within group {group.Id}:");
            AsyncPageable<DeviceRegistrationState> registrationQuery = _provisioningServiceClient.DeviceRegistrationStates.CreateEnrollmentGroupQuery(queryText, group.Id);

            await foreach (DeviceRegistrationState registration in registrationQuery)
            {
                _logger.LogInformation($"\t{registration.RegistrationId} for {registration.DeviceId} is {registration.Status}.");
                if (registration.ErrorCode.HasValue)
                {
                    _logger.LogError($"\t\tWith error ({registration.ErrorCode.Value}): {registration.ErrorMessage}");
                }
            }
        }

        public async Task DeleteEnrollmentGroupAsync()
        {
            _logger.LogInformation($"Deleting the enrollment group {s_enrollmentGroupId}...");
            await _provisioningServiceClient.EnrollmentGroups.DeleteAsync(s_enrollmentGroupId);
        }
    }
}
