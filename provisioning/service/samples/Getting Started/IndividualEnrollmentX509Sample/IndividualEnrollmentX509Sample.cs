// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
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
        private readonly DeviceCapabilities _optionalEdgeCapabilityEnabled = new() { IotEdge = true };
        private readonly DeviceCapabilities _optionalEdgeCapabilityDisabled = new() { IotEdge = false };

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
            Console.WriteLine("\nCreating a query for enrollments...");
            Query query = _provisioningServiceClient.CreateIndividualEnrollmentQuery("SELECT * FROM enrollments");
            while (query.HasNext())
            {
                Console.WriteLine("\nQuerying the next enrollments...");
                QueryResult queryResult = await query.NextAsync();
                IEnumerable<object> items = queryResult.Items;
                foreach (IndividualEnrollment enrollment in items.Cast<IndividualEnrollment>())
                {
                    Console.WriteLine($"Individual enrollment found: {enrollment.RegistrationId}");
                }
            }
        }

        public async Task CreateIndividualEnrollmentX509Async()
        {
            Console.WriteLine("\nCreating a new individualEnrollment...");
            X509Attestation x509 = X509Attestation.CreateFromClientCertificates(_issuerCertificate);
            var individualEnrollment = new IndividualEnrollment(
                _registrationId, x509)
            {
                //The following parameters are optional:
                DeviceId = _deviceId,
                ProvisioningStatus = OptionalProvisioningStatus,
                Capabilities = _optionalEdgeCapabilityEnabled,
                InitialTwinState = new TwinState(
                    tags: null,
                    desiredProperties: new TwinCollection()
                    {
                        ["Brand"] = "Contoso",
                        ["Model"] = "SSC4",
                        ["Color"] = "White",
                    })
            };

            IndividualEnrollment individualEnrollmentResult = await _provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment);
            Console.WriteLine($"\nIndividual Enrollment {individualEnrollmentResult.RegistrationId} created with success.");
        }

        public async Task<IndividualEnrollment> GetIndividualEnrollmentInfoAsync()
        {
            Console.WriteLine("\nGetting the individualEnrollment information...");
            IndividualEnrollment getResult =
                await _provisioningServiceClient.GetIndividualEnrollmentAsync(_registrationId);

            return getResult;
        }

        public async Task UpdateIndividualEnrollmentAsync()
        {
            IndividualEnrollment individualEnrollment = await GetIndividualEnrollmentInfoAsync();
            Console.WriteLine($"Initial device twin state is {individualEnrollment.InitialTwinState}");
            Console.WriteLine($"IoT edge device set to {individualEnrollment.Capabilities.IotEdge}");
            individualEnrollment.InitialTwinState.DesiredProperties["Color"] = "Yellow";
            individualEnrollment.Capabilities = _optionalEdgeCapabilityDisabled;

            Console.WriteLine($"\nUpdating desired properties and capabilities of individual enrollment {individualEnrollment.RegistrationId}");
            IndividualEnrollment individualEnrollmentResult =
                await _provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment);
            Console.WriteLine($"Updated initial device twin state  is {individualEnrollmentResult.InitialTwinState}");
            Console.WriteLine($"Updated IoT edge device to {individualEnrollmentResult.Capabilities.IotEdge}");
            Console.WriteLine($"\nIndividual Enrollment updated with success.");
        }

        public async Task DeleteIndividualEnrollmentAsync()
        {
            Console.WriteLine($"\nDeleting the individualEnrollment...");
            await _provisioningServiceClient.DeleteIndividualEnrollmentAsync(_registrationId);
            Console.WriteLine($"\nIndividual Enrollment deleted with success.");
        }
    }
}
