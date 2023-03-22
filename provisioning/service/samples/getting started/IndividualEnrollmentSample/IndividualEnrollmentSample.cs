// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Azure;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    internal class IndividualEnrollmentSample
    {
        private readonly string _registrationId;
        private readonly ProvisioningServiceClient _provisioningServiceClient;

        // Optional parameters
        private readonly string _deviceId;

        private const ProvisioningStatus OptionalProvisioningStatus = ProvisioningStatus.Enabled;
        private readonly InitialTwinCapabilities _optionalEdgeCapabilityEnabled = new() { IsIotEdge = true };
        private readonly InitialTwinCapabilities _optionalEdgeCapabilityDisabled = new() { IsIotEdge = false };

        public IndividualEnrollmentSample(ProvisioningServiceClient provisioningServiceClient, string deviceId, string registrationId)
        {
            _provisioningServiceClient = provisioningServiceClient;
            _deviceId = deviceId;
            _registrationId = registrationId;
        }

        public async Task RunSampleAsync()
        {
            await CreateIndividualEnrollmentX509Async();
            await UpdateIndividualEnrollmentAsync();
            await QueryIndividualEnrollmentsAsync();
            await DeleteIndividualEnrollmentAsync();
        }

        public async Task CreateIndividualEnrollmentX509Async()
        {
            Console.WriteLine($"Creating an individual enrollment '{_registrationId}'...");
            var individualEnrollment = new IndividualEnrollment(_registrationId, new SymmetricKeyAttestation())
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

        public async Task UpdateIndividualEnrollmentAsync()
        {
            IndividualEnrollment individualEnrollment = await GetIndividualEnrollmentInfoAsync();
            Console.WriteLine($"Initial device twin state is {individualEnrollment.InitialTwinState}.");
            Console.WriteLine($"IoT Edge device set to '{individualEnrollment.Capabilities.IsIotEdge}'.");
            individualEnrollment.InitialTwinState.DesiredProperties["Color"] = "Yellow";
            individualEnrollment.Capabilities = _optionalEdgeCapabilityDisabled;

            Console.WriteLine($"Updating desired properties and capabilities of the individual enrollment '{individualEnrollment.RegistrationId}'...");
            IndividualEnrollment individualEnrollmentResult = await _provisioningServiceClient.IndividualEnrollments.CreateOrUpdateAsync(individualEnrollment);
            Console.WriteLine($"Updated initial device twin state is {individualEnrollmentResult.InitialTwinState}.");
            Console.WriteLine($"Updated IoT Edge device to '{individualEnrollmentResult.Capabilities.IsIotEdge}'.");
            Console.WriteLine($"Successfully updated the individual enrollment '{_registrationId}'.");
        }

        public async Task<IndividualEnrollment> GetIndividualEnrollmentInfoAsync()
        {
            Console.WriteLine("Getting the individual enrollment information...");
            IndividualEnrollment getResult = await _provisioningServiceClient.IndividualEnrollments.GetAsync(_registrationId);

            return getResult;
        }

        public async Task QueryIndividualEnrollmentsAsync()
        {
            const string queryText = "SELECT * FROM enrollments";
            AsyncPageable<IndividualEnrollment> query = _provisioningServiceClient.IndividualEnrollments.CreateQuery(queryText);
            Console.WriteLine($"Querying for individual enrollments: {queryText}");
            await foreach (IndividualEnrollment enrollment in query)
            {
                Console.WriteLine($"Individual enrollment '{enrollment.RegistrationId}'");
            }
        }

        public async Task DeleteIndividualEnrollmentAsync()
        {
            await _provisioningServiceClient.IndividualEnrollments.DeleteAsync(_registrationId);
            Console.WriteLine($"Deleted the individual enrollment '{_registrationId}'.");
        }
    }
}
