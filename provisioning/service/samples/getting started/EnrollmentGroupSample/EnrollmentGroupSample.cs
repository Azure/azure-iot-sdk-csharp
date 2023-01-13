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
        private static readonly string s_enrollmentGroupId = $"EnrollmentGroupSample-{Guid.NewGuid()}";
        private readonly ProvisioningServiceClient _provisioningServiceClient;

        public EnrollmentGroupSample(ProvisioningServiceClient provisioningServiceClient)
        {
            _provisioningServiceClient = provisioningServiceClient;
        }

        public async Task RunSampleAsync()
        {
            await CreateEnrollmentGroupAsync();
            await GetEnrollmentGroupInfoAsync();
            await QueryEnrollmentGroupAsync();
            await DeleteEnrollmentGroupAsync();
        }

        public async Task CreateEnrollmentGroupAsync()
        {
            Console.WriteLine("Creating a new enrollment group...");
            Attestation attestation = new SymmetricKeyAttestation(null, null); // let the service generate keys
            var group = new EnrollmentGroup(s_enrollmentGroupId, attestation);

            group = await _provisioningServiceClient.CreateOrUpdateEnrollmentGroupAsync(group);
            Console.WriteLine($"Created {group.EnrollmentGroupId}: {JsonConvert.SerializeObject(group)}");
        }

        public async Task GetEnrollmentGroupInfoAsync()
        {
            Console.WriteLine("Getting the enrollment group information...");
            EnrollmentGroup group = await _provisioningServiceClient.GetEnrollmentGroupAsync(s_enrollmentGroupId);
            Console.WriteLine($"Got {group.EnrollmentGroupId}: {JsonConvert.SerializeObject(group)}");
        }

        public async Task QueryEnrollmentGroupAsync()
        {
            var querySpecification = new QuerySpecification("SELECT * FROM enrollmentGroups");
            Console.WriteLine($"Running a query for enrollment groups: {querySpecification.Query}");
            using Query query = _provisioningServiceClient.CreateEnrollmentGroupQuery(querySpecification);
            while (query.HasNext())
            {
                QueryResult queryResult = await query.NextAsync();
                foreach (EnrollmentGroup group in queryResult.Items.Cast<EnrollmentGroup>())
                {
                    Console.WriteLine($"Found enrollment group {group.EnrollmentGroupId} is {group.ProvisioningStatus}.");
                    await EnumerateRegistrationsInGroupAsync(querySpecification, group);
                }
            }
        }

        private async Task EnumerateRegistrationsInGroupAsync(QuerySpecification querySpecification, EnrollmentGroup group)
        {
            Console.WriteLine($"Registrations within group {group.EnrollmentGroupId}:");
            using Query query = _provisioningServiceClient.CreateEnrollmentGroupRegistrationStateQuery(
                querySpecification,
                group.EnrollmentGroupId);

            while (query.HasNext())
            {
                QueryResult queryResult = await query.NextAsync();
                foreach (DeviceRegistrationState registration in queryResult.Items.Cast<DeviceRegistrationState>())
                {
                    Console.WriteLine($"\t{registration.RegistrationId} for {registration.DeviceId} is {registration.Status}.");
                    if (registration.ErrorCode.HasValue)
                    {
                        Console.WriteLine($"\t\tWith error ({registration.ErrorCode.Value}): {registration.ErrorMessage}");
                    }
                }
            }
        }

        private async Task DeleteEnrollmentGroupAsync()
        {
            Console.WriteLine("Deleting the enrollmentGroup...");
            await _provisioningServiceClient.DeleteEnrollmentGroupAsync(s_enrollmentGroupId);
            Console.WriteLine($"Enrollment group {s_enrollmentGroupId} deleted.");
        }
    }
}
