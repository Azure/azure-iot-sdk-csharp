// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    public class BulkOperationSample
    {
        private readonly ProvisioningServiceClient _provisioningServiceClient;
        private const string SampleRegistrationId1 = "myvalid-registratioid-csharp-1";
        private const string SampleRegistrationId2 = "myvalid-registratioid-csharp-2";
        private const string SampleTpmEndorsementKey =
            "AToAAQALAAMAsgAgg3GXZ0SEs/gakMyNRqXXJP1S124GUgtk8qHaGzMUaaoABgCAAEMAEAgAAAAAAAEAxsj2gUS" +
            "cTk1UjuioeTlfGYZrrimExB+bScH75adUMRIi2UOMxG1kw4y+9RW/IVoMl4e620VxZad0ARX2gUqVjYO7KPVt3d" +
            "yKhZS3dkcvfBisBhP1XH9B33VqHG9SHnbnQXdBUaCgKAfxome8UmBKfe+naTsE5fkvjb/do3/dD6l4sGBwFCnKR" +
            "dln4XpM03zLpoHFao8zOwt8l/uP3qUIxmCYv9A7m69Ms+5/pCkTu/rK4mRDsfhZ0QLfbzVI6zQFOKF/rwsfBtFe" +
            "WlWtcuJMKlXdD8TXWElTzgh7JS4qhFzreL0c1mI0GCj+Aws0usZh7dLIVPnlgZcBhgy1SSDQMQ==";
        
        // Maximum number of elements per query.
        private const int QueryPageSize = 100;

        private static readonly IDictionary<string, string> s_registrationIds = new Dictionary<string, string>
        {
            { SampleRegistrationId1, SampleTpmEndorsementKey },
            { SampleRegistrationId2, SampleTpmEndorsementKey }
        };
        
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
            foreach (KeyValuePair<string, string> item in s_registrationIds)
            {
                Attestation attestation = new TpmAttestation(item.Value);
                individualEnrollments.Add(new IndividualEnrollment(item.Key, attestation));
            }

            Console.WriteLine("\nRunning the bulk operation to create the individualEnrollments...");
            BulkEnrollmentOperationResult bulkEnrollmentOperationResult =
                await _provisioningServiceClient.RunBulkEnrollmentOperationAsync(BulkOperationMode.Create, individualEnrollments);
            Console.WriteLine("\nResult of the Create bulk enrollment.");
            Console.WriteLine(bulkEnrollmentOperationResult);

            return individualEnrollments;
        }

        public async Task GetIndividualEnrollmentInfoAsync(List<IndividualEnrollment> individualEnrollments)
        {
            foreach (IndividualEnrollment individualEnrollment in individualEnrollments)
            {
                string registrationId = individualEnrollment.RegistrationId;
                Console.WriteLine($"\nGetting the {nameof(individualEnrollment)} information for {registrationId}...");
                IndividualEnrollment getResult = await _provisioningServiceClient
                    .GetIndividualEnrollmentAsync(registrationId);
                Console.WriteLine(getResult);
            }
        }
        
        public async Task DeleteIndividualEnrollmentsAsync(List<IndividualEnrollment> individualEnrollments)
        {
            Console.WriteLine("\nDeleting the set of individualEnrollments...");
            BulkEnrollmentOperationResult bulkEnrollmentOperationResult = await _provisioningServiceClient
                .RunBulkEnrollmentOperationAsync(BulkOperationMode.Delete, individualEnrollments);
            Console.WriteLine(bulkEnrollmentOperationResult);
        }

        public async Task QueryIndividualEnrollmentsAsync()
        {
            Console.WriteLine("\nCreating a query for enrollments...");
            var querySpecification = new QuerySpecification("SELECT * FROM enrollments");

            using Query query = _provisioningServiceClient.CreateIndividualEnrollmentQuery(querySpecification, QueryPageSize);
            while (query.HasNext())
            {
                Console.WriteLine("\nQuerying the next enrollments...");
                QueryResult queryResult = await query.NextAsync();
                Console.WriteLine(queryResult);
            }
        }
    }
}
