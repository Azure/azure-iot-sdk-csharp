// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    internal class IndividualEnrollmentSample
    {
        private readonly string _registrationId;
        private readonly ProvisioningServiceClient _provisioningServiceClient;
        private readonly ILogger _logger;

        // Optional parameters
        private readonly string _deviceId;

        private const ProvisioningStatus OptionalProvisioningStatus = ProvisioningStatus.Enabled;
        private readonly InitialTwinCapabilities _optionalEdgeCapabilityEnabled = new() { IsIotEdge = true };
        private readonly InitialTwinCapabilities _optionalEdgeCapabilityDisabled = new() { IsIotEdge = false };

        public IndividualEnrollmentSample(ProvisioningServiceClient provisioningServiceClient, string deviceId, string registrationId, ILogger logger)
        {
            _provisioningServiceClient = provisioningServiceClient;
            _deviceId = deviceId;
            _registrationId = registrationId;
            _logger = logger;
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
            _logger.LogInformation($"Creating an individual enrollment '{_registrationId}'...");
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
            _logger.LogInformation($"Successfully created the individual enrollment '{individualEnrollmentResult.RegistrationId}'.");
        }

        public async Task UpdateIndividualEnrollmentAsync()
        {
            IndividualEnrollment individualEnrollment = await GetIndividualEnrollmentInfoAsync();
            _logger.LogInformation($"Initial device twin state is {individualEnrollment.InitialTwinState}.");
            _logger.LogInformation($"IoT Edge device set to '{individualEnrollment.Capabilities.IsIotEdge}'.");
            individualEnrollment.InitialTwinState.DesiredProperties["Color"] = "Yellow";
            individualEnrollment.Capabilities = _optionalEdgeCapabilityDisabled;

            _logger.LogInformation($"Updating desired properties and capabilities of the individual enrollment '{individualEnrollment.RegistrationId}'...");
            IndividualEnrollment individualEnrollmentResult = await _provisioningServiceClient.IndividualEnrollments.CreateOrUpdateAsync(individualEnrollment);
            _logger.LogInformation($"Updated initial device twin state is {individualEnrollmentResult.InitialTwinState}.");
            _logger.LogInformation($"Updated IoT Edge device to '{individualEnrollmentResult.Capabilities.IsIotEdge}'.");
            _logger.LogInformation($"Successfully updated the individual enrollment '{_registrationId}'.");
        }

        public async Task<IndividualEnrollment> GetIndividualEnrollmentInfoAsync()
        {
            _logger.LogInformation("Getting the individual enrollment information...");
            IndividualEnrollment getResult = await _provisioningServiceClient.IndividualEnrollments.GetAsync(_registrationId);
            return getResult;
        }

        public async Task QueryIndividualEnrollmentsAsync()
        {
            const string queryText = "SELECT * FROM enrollments";
            AsyncPageable<IndividualEnrollment> query = _provisioningServiceClient.IndividualEnrollments.CreateQuery(queryText);
            _logger.LogInformation($"Querying for individual enrollments: {queryText}");
            await foreach (IndividualEnrollment enrollment in query)
            {
                _logger.LogInformation($"Individual enrollment '{enrollment.RegistrationId}'");
            }
        }

        public async Task DeleteIndividualEnrollmentAsync()
        {
            await _provisioningServiceClient.IndividualEnrollments.DeleteAsync(_registrationId);
            _logger.LogInformation($"Deleted the individual enrollment '{_registrationId}'.");
        }
    }
}
