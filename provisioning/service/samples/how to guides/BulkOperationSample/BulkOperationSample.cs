// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    public class BulkOperationSample
    {
        private readonly ProvisioningServiceClient _provisioningServiceClient;
        private const string SampleRegistrationId1 = "myvalid-registratioid-csharp-1";
        private const string SampleRegistrationId2 = "myvalid-registratioid-csharp-2";

        // Maximum number of elements per query.
        private const int QueryPageSize = 100;

        private static readonly List<string> s_registrationIds = new() { SampleRegistrationId1, SampleRegistrationId2 };

        public BulkOperationSample(ProvisioningServiceClient provisioningServiceClient)
        {
            _provisioningServiceClient = provisioningServiceClient;
        }

        public async Task RunSampleAsync()
        {
            await QueryIndividualEnrollmentsAsync().ConfigureAwait(false);

            List<IndividualEnrollment> enrollments = await CreateBulkIndividualEnrollmentsAsync();
            await GetIndividualEnrollmentInfoAsync(enrollments);
            await DeleteIndividualEnrollmentsAsync(enrollments);
        }

        public async Task<List<IndividualEnrollment>> CreateBulkIndividualEnrollmentsAsync()
        {
            Console.WriteLine("\nCreating a new set of individualEnrollments...");
            var individualEnrollments = new List<IndividualEnrollment>();
            foreach (string item in s_registrationIds)
            {
                Attestation attestation = new SymmetricKeyAttestation(
                    CryptoKeyGenerator.GenerateKey(32),
                    CryptoKeyGenerator.GenerateKey(32));
                individualEnrollments.Add(new IndividualEnrollment(item, attestation));
            }

            Console.WriteLine("\nRunning the bulk operation to create the individualEnrollments...");
            BulkEnrollmentOperationResult bulkEnrollmentOperationResult = await _provisioningServiceClient
                .IndividualEnrollments
                .RunBulkOperationAsync(BulkOperationMode.Create, individualEnrollments);
            Console.WriteLine("\nResult of the create bulk enrollment.");
            Console.WriteLine(JsonConvert.SerializeObject(bulkEnrollmentOperationResult));

            return individualEnrollments;
        }

        public async Task GetIndividualEnrollmentInfoAsync(List<IndividualEnrollment> individualEnrollments)
        {
            foreach (IndividualEnrollment individualEnrollment in individualEnrollments)
            {
                string registrationId = individualEnrollment.RegistrationId;
                Console.WriteLine($"\nGetting the {nameof(individualEnrollment)} information for {registrationId}...");
                IndividualEnrollment getResult = await _provisioningServiceClient
                    .IndividualEnrollments
                    .GetAsync(registrationId);
                Console.WriteLine(getResult);
            }
        }

        public async Task DeleteIndividualEnrollmentsAsync(List<IndividualEnrollment> individualEnrollments)
        {
            Console.WriteLine("\nDeleting the set of individualEnrollments...");
            BulkEnrollmentOperationResult bulkEnrollmentOperationResult = await _provisioningServiceClient
                .IndividualEnrollments
                .RunBulkOperationAsync(BulkOperationMode.Delete, individualEnrollments);
            Console.WriteLine(JsonConvert.SerializeObject(bulkEnrollmentOperationResult));
        }

        public async Task QueryIndividualEnrollmentsAsync()
        {
            Console.WriteLine("\nCreating a query for enrollments...");

            IAsyncEnumerable<IndividualEnrollment> query = _provisioningServiceClient.IndividualEnrollments.CreateQuery("SELECT * FROM enrollments", QueryPageSize);
            await foreach (IndividualEnrollment enrollment in query)
            {
                Console.WriteLine("\nQuerying the next enrollments...");
                Console.WriteLine(enrollment);
            }
        }
    }
}
