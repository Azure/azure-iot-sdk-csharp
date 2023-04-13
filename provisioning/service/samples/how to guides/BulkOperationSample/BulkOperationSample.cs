// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    public class BulkOperationSample
    {
        private readonly ProvisioningServiceClient _provisioningServiceClient;
        private readonly ILogger _logger;
        private const string SampleRegistrationId1 = "myvalid-registratioid-csharp-1";
        private const string SampleRegistrationId2 = "myvalid-registratioid-csharp-2";

        private static readonly List<string> s_registrationIds = new() { SampleRegistrationId1, SampleRegistrationId2 };

        public BulkOperationSample(ProvisioningServiceClient provisioningServiceClient, ILogger logger)
        {
            _provisioningServiceClient = provisioningServiceClient;
            _logger = logger;
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
            _logger.LogInformation("\nCreating a new set of individualEnrollments...");
            var individualEnrollments = new List<IndividualEnrollment>();
            foreach (string item in s_registrationIds)
            {
                Attestation attestation = new SymmetricKeyAttestation(
                    CryptoKeyGenerator.GenerateKey(32),
                    CryptoKeyGenerator.GenerateKey(32));
                individualEnrollments.Add(new IndividualEnrollment(item, attestation));
            }

            _logger.LogInformation("\nRunning the bulk operation to create the individualEnrollments...");
            BulkEnrollmentOperationResult bulkEnrollmentOperationResult = await _provisioningServiceClient
                .IndividualEnrollments
                .RunBulkOperationAsync(BulkOperationMode.Create, individualEnrollments);
            _logger.LogInformation("\nResult of the create bulk enrollment.");
            _logger.LogInformation(JsonConvert.SerializeObject(bulkEnrollmentOperationResult));

            return individualEnrollments;
        }

        public async Task GetIndividualEnrollmentInfoAsync(List<IndividualEnrollment> individualEnrollments)
        {
            foreach (IndividualEnrollment individualEnrollment in individualEnrollments)
            {
                string registrationId = individualEnrollment.RegistrationId;
                _logger.LogInformation($"\nGetting the {nameof(individualEnrollment)} information for {registrationId}...");
                IndividualEnrollment getResult = await _provisioningServiceClient
                    .IndividualEnrollments
                    .GetAsync(registrationId);
                _logger.LogInformation($"Current provisioning status: {getResult.ProvisioningStatus}");
            }
        }

        public async Task DeleteIndividualEnrollmentsAsync(List<IndividualEnrollment> individualEnrollments)
        {
            _logger.LogInformation("\nDeleting the set of individualEnrollments...");
            BulkEnrollmentOperationResult bulkEnrollmentOperationResult = await _provisioningServiceClient
                .IndividualEnrollments
                .RunBulkOperationAsync(BulkOperationMode.Delete, individualEnrollments);
            _logger.LogInformation(JsonConvert.SerializeObject(bulkEnrollmentOperationResult));
        }

        public async Task QueryIndividualEnrollmentsAsync()
        {
            _logger.LogInformation("\nCreating a query for enrollments...");

            IAsyncEnumerable<IndividualEnrollment> query = _provisioningServiceClient.IndividualEnrollments.CreateQuery("SELECT * FROM enrollments");
            await foreach (IndividualEnrollment enrollment in query)
            {
                _logger.LogInformation("\nQuerying the next enrollments...");
                _logger.LogInformation($"Current provisioning status: {enrollment.ProvisioningStatus}");
            }
        }
    }
}
