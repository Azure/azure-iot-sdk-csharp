// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    internal class IndividualEnrollmentX509Sample
    {
        private string _registrationId;
        private readonly ProvisioningServiceClient _provisioningServiceClient;
        private readonly X509Certificate2 _issuerCertificate;

        // Optional parameters
        private string _deviceId;

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
                QueryResult queryResult = await query.NextAsync();
                Console.WriteLine(queryResult);
            }
        }

        public async Task CreateIndividualEnrollmentX509Async()
        {
            Console.WriteLine("\nCreating a new individualEnrollment...");
            X509Attestation _x509 = X509Attestation.CreateFromClientCertificates(_issuerCertificate);
            var individualEnrollment = new IndividualEnrollment(
                _registrationId, _x509)
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
            Console.WriteLine(individualEnrollment);

            Console.WriteLine("\nAdding new individual enrollment");
            IndividualEnrollment individualEnrollmentResult = await _provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment);
            Console.WriteLine("\nIndividual Enrollment created with success.");
            Console.WriteLine(individualEnrollmentResult);
        }

        public async Task<IndividualEnrollment> GetIndividualEnrollmentInfoAsync()
        {
            Console.WriteLine("\nGetting the individualEnrollment information...");
            IndividualEnrollment getResult =
                await _provisioningServiceClient.GetIndividualEnrollmentAsync(_registrationId);
            Console.WriteLine(getResult);

            return getResult;
        }

        public async Task UpdateIndividualEnrollmentAsync()
        {
            IndividualEnrollment individualEnrollment = await GetIndividualEnrollmentInfoAsync();
            individualEnrollment.InitialTwinState.DesiredProperties["Color"] = "Yellow";
            individualEnrollment.Capabilities = _optionalEdgeCapabilityDisabled;

            IndividualEnrollment individualEnrollmentResult =
                await _provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment);
            Console.WriteLine(individualEnrollmentResult);
        }

        public async Task DeleteIndividualEnrollmentAsync()
        {
            Console.WriteLine("\nDeleting the individualEnrollment...");
            await _provisioningServiceClient.DeleteIndividualEnrollmentAsync(_registrationId);
        }
    }
}
