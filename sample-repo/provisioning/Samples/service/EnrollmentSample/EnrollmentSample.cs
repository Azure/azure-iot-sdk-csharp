// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    public class EnrollmentSample
    {
        private const string RegistrationId = "myvalid-registratioid-csharp";
        private const string TpmEndorsementKey =
            "AToAAQALAAMAsgAgg3GXZ0SEs/gakMyNRqXXJP1S124GUgtk8qHaGzMUaaoABgCAAEMAEAgAAAAAAAEAxsj2gUS" +
            "cTk1UjuioeTlfGYZrrimExB+bScH75adUMRIi2UOMxG1kw4y+9RW/IVoMl4e620VxZad0ARX2gUqVjYO7KPVt3d" +
            "yKhZS3dkcvfBisBhP1XH9B33VqHG9SHnbnQXdBUaCgKAfxome8UmBKfe+naTsE5fkvjb/do3/dD6l4sGBwFCnKR" +
            "dln4XpM03zLpoHFao8zOwt8l/uP3qUIxmCYv9A7m69Ms+5/pCkTu/rK4mRDsfhZ0QLfbzVI6zQFOKF/rwsfBtFe" +
            "WlWtcuJMKlXdD8TXWElTzgh7JS4qhFzreL0c1mI0GCj+Aws0usZh7dLIVPnlgZcBhgy1SSDQMQ==";

        // Optional parameters
        private const string OptionalDeviceId = "myCSharpDevice";
        private const ProvisioningStatus OptionalProvisioningStatus = ProvisioningStatus.Enabled;
        private DeviceCapabilities OptionalEdgeCapabilityEnabled = new DeviceCapabilities {IotEdge = true };
        private DeviceCapabilities OptionalEdgeCapabilityDisabled = new DeviceCapabilities { IotEdge = false };

        ProvisioningServiceClient _provisioningServiceClient;

        public EnrollmentSample(ProvisioningServiceClient provisioningServiceClient)
        {
            _provisioningServiceClient = provisioningServiceClient;
        }

        public async Task RunSampleAsync()
        {
            await QueryIndividualEnrollmentsAsync().ConfigureAwait(false);

            await CreateIndividualEnrollmentTpmAsync().ConfigureAwait(false);
            await UpdateIndividualEnrollmentAsync().ConfigureAwait(false);
            await DeleteIndividualEnrollmentAsync().ConfigureAwait(false);            
        }

        public async Task QueryIndividualEnrollmentsAsync()
        {
            Console.WriteLine("\nCreating a query for enrollments...");
            QuerySpecification querySpecification = new QuerySpecification("SELECT * FROM enrollments");
            using (Query query = _provisioningServiceClient.CreateIndividualEnrollmentQuery(querySpecification))
            {
                while (query.HasNext())
                {
                    Console.WriteLine("\nQuerying the next enrollments...");
                    QueryResult queryResult = await query.NextAsync().ConfigureAwait(false);
                    Console.WriteLine(queryResult);
                }
            }
        }

        public async Task CreateIndividualEnrollmentTpmAsync()
        {
            Console.WriteLine("\nCreating a new individualEnrollment...");
            Attestation attestation = new TpmAttestation(TpmEndorsementKey);
            IndividualEnrollment individualEnrollment =
                    new IndividualEnrollment(
                            RegistrationId,
                            attestation);

            // The following parameters are optional:
            individualEnrollment.DeviceId = OptionalDeviceId;
            individualEnrollment.ProvisioningStatus = OptionalProvisioningStatus;
            individualEnrollment.InitialTwinState = new TwinState(
                null,
                new TwinCollection()
                {
                    ["Brand"] = "Contoso",
                    ["Model"] = "SSC4",
                    ["Color"] = "White",
                });
            individualEnrollment.Capabilities = OptionalEdgeCapabilityEnabled;
            
            Console.WriteLine("\nAdding new individualEnrollment...");
            IndividualEnrollment individualEnrollmentResult =
                await _provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment).ConfigureAwait(false);
            Console.WriteLine(individualEnrollmentResult);
        }

        public async Task<IndividualEnrollment> GetIndividualEnrollmentInfoAsync()
        {
            Console.WriteLine("\nGetting the individualEnrollment information...");
            IndividualEnrollment getResult =
                await _provisioningServiceClient.GetIndividualEnrollmentAsync(RegistrationId).ConfigureAwait(false);
            Console.WriteLine(getResult);

            return getResult;
        }

        public async Task UpdateIndividualEnrollmentAsync()
        {
            var individualEnrollment = await GetIndividualEnrollmentInfoAsync().ConfigureAwait(false);
            individualEnrollment.InitialTwinState.DesiredProperties["Color"] = "Yellow";
            individualEnrollment.Capabilities = OptionalEdgeCapabilityDisabled;

            IndividualEnrollment individualEnrollmentResult =
                await _provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment).ConfigureAwait(false);
            Console.WriteLine(individualEnrollmentResult);
        }

        public async Task DeleteIndividualEnrollmentAsync()
        {
            Console.WriteLine("\nDeleting the individualEnrollment...");
            await _provisioningServiceClient.DeleteIndividualEnrollmentAsync(RegistrationId).ConfigureAwait(false);
        }
    }
}
