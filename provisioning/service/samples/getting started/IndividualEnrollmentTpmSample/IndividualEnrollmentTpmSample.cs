// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    internal class IndividualEnrollmentTpmSample
    {
        private readonly string _registrationId;

        private readonly string _tpmEndorsementKey;

        // Optional parameters
        private readonly string _deviceId;

        private const ProvisioningStatus OptionalProvisioningStatus = ProvisioningStatus.Enabled;
        private readonly ProvisioningTwinCapabilities _optionalEdgeCapabilityEnabled = new() { IsIotEdge = true };
        private readonly ProvisioningTwinCapabilities _optionalEdgeCapabilityDisabled = new() { IsIotEdge = false };

        private readonly ProvisioningServiceClient _provisioningServiceClient;

        public IndividualEnrollmentTpmSample(
            ProvisioningServiceClient provisioningServiceClient,
            string deviceId,
            string registrationId,
            string tpmEndorsementKey)
        {
            _provisioningServiceClient = provisioningServiceClient;
            _deviceId = deviceId;
            _registrationId = registrationId;
            _tpmEndorsementKey = tpmEndorsementKey;
        }

        public async Task RunSampleAsync()
        {
            await QueryIndividualEnrollmentsAsync();
            await CreateIndividualEnrollmentTpmAsync();
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

        public async Task CreateIndividualEnrollmentTpmAsync()
        {
            Console.WriteLine($"Creating an individual enrollment '{_registrationId}'...");
            Attestation attestation = new TpmAttestation(_tpmEndorsementKey);
            var individualEnrollment = new IndividualEnrollment(
                _registrationId,
                attestation)
            {
                // The following properties are optional:
                DeviceId = _deviceId,
                ProvisioningStatus = OptionalProvisioningStatus,
                Capabilities = _optionalEdgeCapabilityEnabled,
                InitialTwinState = new InitialTwinState
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

            IndividualEnrollment individualEnrollmentResult = await _provisioningServiceClient
                .IndividualEnrollments
                .CreateOrUpdateAsync(individualEnrollment);
            Console.WriteLine($"Successfully created the individual enrollment '{individualEnrollmentResult.RegistrationId}'.");
        }

        public async Task<IndividualEnrollment> GetIndividualEnrollmentInfoAsync()
        {
            Console.WriteLine("Getting the individualEnrollment information...");
            IndividualEnrollment getResult = await _provisioningServiceClient
                .IndividualEnrollments
                .GetAsync(_registrationId);

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
            IndividualEnrollment individualEnrollmentResult = await _provisioningServiceClient
                .IndividualEnrollments
                .CreateOrUpdateAsync(individualEnrollment);
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
