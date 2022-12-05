// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    internal class IndividualEnrollmentX509Sample
    {
        private readonly string _registrationId;
        private readonly ProvisioningServiceClient _provisioningServiceClient;
        private readonly X509Certificate2 _issuerCertificate;

        // Optional parameters
        private readonly string _deviceId;

        private const ProvisioningStatus OptionalProvisioningStatus = ProvisioningStatus.Enabled;
        private readonly InitialTwinCapabilities _optionalEdgeCapabilityEnabled = new() { IsIotEdge = true };
        private readonly InitialTwinCapabilities _optionalEdgeCapabilityDisabled = new() { IsIotEdge = false };

        public IndividualEnrollmentX509Sample(ProvisioningServiceClient provisioningServiceClient, X509Certificate2 issuerCertificate, string deviceId, string registrationId)
        {
            _provisioningServiceClient = provisioningServiceClient;
            _issuerCertificate = issuerCertificate;
            _deviceId = deviceId;
            _registrationId = registrationId;
        }

        public async Task RunSampleAsync()
        {
            await QueryIndividualEnrollmentsAsync();
            await CreateIndividualEnrollmentX509Async();
            await UpdateIndividualEnrollmentAsync();
            await DeleteIndividualEnrollmentAsync();
        }

        public async Task QueryIndividualEnrollmentsAsync()
        {
            Console.WriteLine("Creating a query for enrollments...");
            Query query = _provisioningServiceClient.IndividualEnrollments.CreateQuery("SELECT * FROM enrollments");
            while (query.HasNext())
            {
                Console.WriteLine("Querying the next page of enrollments...");
                QueryResult queryResult = await query.NextAsync();
                IEnumerable<object> items = queryResult.Items;
                foreach (IndividualEnrollment enrollment in items.Cast<IndividualEnrollment>())
                {
                    Console.WriteLine($"Individual enrollment '{enrollment.RegistrationId}'");
                }
            }
        }

        public async Task CreateIndividualEnrollmentX509Async()
        {
            Console.WriteLine($"Creating an individual enrollment '{_registrationId}'...");
            X509Attestation x509 = X509Attestation.CreateFromClientCertificates(_issuerCertificate);
            var individualEnrollment = new IndividualEnrollment(_registrationId, x509)
            {
                // The following properties are optional:
                DeviceId = _deviceId,
                ProvisioningStatus = OptionalProvisioningStatus,
                Capabilities = _optionalEdgeCapabilityEnabled,
                InitialTwinState = new InitialTwin
                {
                    Tags = null,
                    DesiredProperties =
                    {
                        ["Brand"] = "Contoso",
                        ["Model"] = "SSC4",
                        ["Color"] = "White",
                    },
                },
            };

            IndividualEnrollment individualEnrollmentResult = await _provisioningServiceClient.IndividualEnrollments.CreateOrUpdateAsync(individualEnrollment);
            Console.WriteLine($"Successfully created the individual enrollment '{individualEnrollmentResult.RegistrationId}'.");
        }

        public async Task<IndividualEnrollment> GetIndividualEnrollmentInfoAsync()
        {
            Console.WriteLine("Getting the individual enrollment information...");
            IndividualEnrollment getResult =
                await _provisioningServiceClient.IndividualEnrollments.GetAsync(_registrationId);

            return getResult;
        }

        public async Task UpdateIndividualEnrollmentAsync()
        {
            IndividualEnrollment individualEnrollment = await GetIndividualEnrollmentInfoAsync();
            Console.WriteLine($"Initial device twin state is {individualEnrollment.InitialTwinState}.");
            Console.WriteLine($"IoT Edge device set to '{individualEnrollment.Capabilities.IsIotEdge}'.");
            individualEnrollment.InitialTwinState.DesiredProperties["Color"] = "Yellow";
            individualEnrollment.Capabilities = _optionalEdgeCapabilityDisabled;

            Console.WriteLine($"Updating desired properties and capabilities of the individual enrollment '{individualEnrollment.RegistrationId}'...");
            IndividualEnrollment individualEnrollmentResult =
                await _provisioningServiceClient.IndividualEnrollments.CreateOrUpdateAsync(individualEnrollment);
            Console.WriteLine($"Updated initial device twin state is {individualEnrollmentResult.InitialTwinState}.");
            Console.WriteLine($"Updated IoT Edge device to '{individualEnrollmentResult.Capabilities.IsIotEdge}'.");
            Console.WriteLine($"Successfully updated the individual enrollment '{_registrationId}'.");
        }

        public async Task DeleteIndividualEnrollmentAsync()
        {
            Console.WriteLine($"Deleting the individual enrollment '{_registrationId}'...");
            await _provisioningServiceClient.IndividualEnrollments.DeleteAsync(_registrationId);
            Console.WriteLine($"Successfully deleted the individual enrollment '{_registrationId}'.");
        }
    }
}
