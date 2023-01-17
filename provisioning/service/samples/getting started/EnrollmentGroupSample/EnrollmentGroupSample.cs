// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    internal class EnrollmentGroupSample
    {
        private static readonly string s_enrollmentGroupId = $"enrollmentgrouptest-{Guid.NewGuid()}";
        private readonly ProvisioningServiceClient _provisioningServiceClient;

        public EnrollmentGroupSample(ProvisioningServiceClient provisioningServiceClient)
        {
            _provisioningServiceClient = provisioningServiceClient;
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
            Console.WriteLine($"Creating an enrollment group: {JsonConvert.SerializeObject(enrollmentGroup)}");

            EnrollmentGroup group = await _provisioningServiceClient.EnrollmentGroups.CreateOrUpdateAsync(enrollmentGroup);
            Console.WriteLine($"Created {group.Id}: {JsonConvert.SerializeObject(group)}");
        }

        public async Task GetEnrollmentGroupInfoAsync()
        {
            Console.WriteLine("Getting the enrollment group information...");
            EnrollmentGroup group = await _provisioningServiceClient.EnrollmentGroups.GetAsync(s_enrollmentGroupId);
            Console.WriteLine($"Got {group.Id}: {JsonConvert.SerializeObject(group)}");
        }

        public async Task QueryEnrollmentGroupAsync()
        {
            string queryText = "SELECT * FROM enrollmentGroups";
            Console.WriteLine($"Running a query for enrollment groups: {queryText}");
            Query query = _provisioningServiceClient.EnrollmentGroups.CreateQuery(queryText);

            while (query.HasNext())
            {
                QueryResult queryResult = await query.NextAsync();

                foreach (EnrollmentGroup group in queryResult.Items.Cast<EnrollmentGroup>())
                {
                    Console.WriteLine($"Found enrollment group {group.Id} is {group.ProvisioningStatus}.");
                    await EnumerateRegistrationsInGroupAsync(queryText, group);
                }
            }
        }

        private async Task EnumerateRegistrationsInGroupAsync(string queryText, EnrollmentGroup group)
        {
            Console.WriteLine($"Registrations within group {group.Id}:");
            Query registrationQuery = _provisioningServiceClient.DeviceRegistrationStates.CreateEnrollmentGroupQuery(queryText, group.Id);

            while (registrationQuery.HasNext())
            {
                QueryResult registrationQueryResult = await registrationQuery.NextAsync();

                foreach (DeviceRegistrationState registration in registrationQueryResult.Items.Cast<DeviceRegistrationState>())
                {
                    Console.WriteLine($"\t{registration.RegistrationId} for {registration.DeviceId} is {registration.Status}.");
                    if (registration.ErrorCode.HasValue)
                    {
                        Console.WriteLine($"\t\tWith error ({registration.ErrorCode.Value}): {registration.ErrorMessage}");
                    }
                }
            }
        }

        public async Task DeleteEnrollmentGroupAsync()
        {
            Console.WriteLine($"Deleting the enrollment group {s_enrollmentGroupId}...");
            await _provisioningServiceClient.EnrollmentGroups.DeleteAsync(s_enrollmentGroupId);
        }
    }
}
