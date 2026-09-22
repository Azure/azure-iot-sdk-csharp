// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    internal class EnrollmentGroupSample
    {
        private static readonly string s_enrollmentGroupId = $"EnrollmentGroupSample-{Guid.NewGuid()}";
        private readonly ProvisioningServiceClient _provisioningServiceClient;
        private readonly Parameters _parameters;

        public EnrollmentGroupSample(ProvisioningServiceClient provisioningServiceClient, Parameters parameters)
        {
            _provisioningServiceClient = provisioningServiceClient;
            _parameters = parameters;
        }

        public async Task RunSampleAsync()
        {
            await CreateEnrollmentGroupAsync();
            await GetEnrollmentGroupInfoAsync();
            await QueryEnrollmentGroupAsync();
            await DeleteEnrollmentGroupAsync();
        }

        public async Task CreateEnrollmentGroupAsync()
        {
            Console.WriteLine("Creating a new enrollment group...");
            Attestation attestation = new SymmetricKeyAttestation(null, null); // let the service generate keys
            var group = new EnrollmentGroup(s_enrollmentGroupId, attestation);

            // The following fields are available starting with service API version 2026-11-01. They are only
            // set when a 2026 (or later) ServiceVersion is selected AND a value is supplied, so the default
            // 2019-03-31 path is unaffected and never sends them. Select 2026-11-02-preview to exercise these
            // fields against the preview today. The values reference resources that must already exist in your
            // provisioning service.
            ServiceVersion serviceVersion = _parameters.GetServiceVersion();
            if (serviceVersion != ServiceVersion.V2019_03_31)
            {
                if (!string.IsNullOrWhiteSpace(_parameters.NamespaceName))
                {
                    group.NamespaceName = _parameters.NamespaceName;
                }
                if (!string.IsNullOrWhiteSpace(_parameters.CertificateAuthorityName))
                {
                    group.CertificateAuthorityName = _parameters.CertificateAuthorityName;
                }
                if (!string.IsNullOrWhiteSpace(_parameters.CertificatePolicyName))
                {
                    group.CertificatePolicyName = _parameters.CertificatePolicyName;
                }
            }

            // deviceTypeRefs is preview-only (2026-11-02-preview) and at most one item is supported. It is
            // only serialized when the preview ServiceVersion is selected.
            if (serviceVersion == ServiceVersion.V2026_11_02_Preview
                && !string.IsNullOrWhiteSpace(_parameters.DeviceTypeRef))
            {
                group.DeviceTypeRefs = new List<string> { _parameters.DeviceTypeRef };
            }

            group = await _provisioningServiceClient.CreateOrUpdateEnrollmentGroupAsync(group);
            Console.WriteLine($"Created {group.EnrollmentGroupId}: {JsonConvert.SerializeObject(group)}");
        }

        public async Task GetEnrollmentGroupInfoAsync()
        {
            Console.WriteLine("Getting the enrollment group information...");
            EnrollmentGroup group = await _provisioningServiceClient.GetEnrollmentGroupAsync(s_enrollmentGroupId);
            Console.WriteLine($"Got {group.EnrollmentGroupId}: {JsonConvert.SerializeObject(group)}");
        }

        public async Task QueryEnrollmentGroupAsync()
        {
            var querySpecification = new QuerySpecification("SELECT * FROM enrollmentGroups");
            Console.WriteLine($"Running a query for enrollment groups: {querySpecification.Query}");
            using Query query = _provisioningServiceClient.CreateEnrollmentGroupQuery(querySpecification);
            while (query.HasNext())
            {
                QueryResult queryResult = await query.NextAsync();
                foreach (EnrollmentGroup group in queryResult.Items.Cast<EnrollmentGroup>())
                {
                    Console.WriteLine($"Found enrollment group {group.EnrollmentGroupId} is {group.ProvisioningStatus}.");
                    await EnumerateRegistrationsInGroupAsync(querySpecification, group);
                }
            }
        }

        private async Task EnumerateRegistrationsInGroupAsync(QuerySpecification querySpecification, EnrollmentGroup group)
        {
            Console.WriteLine($"Registrations within group {group.EnrollmentGroupId}:");
            using Query query = _provisioningServiceClient.CreateEnrollmentGroupRegistrationStateQuery(
                querySpecification,
                group.EnrollmentGroupId);

            while (query.HasNext())
            {
                QueryResult queryResult = await query.NextAsync();
                foreach (DeviceRegistrationState registration in queryResult.Items.Cast<DeviceRegistrationState>())
                {
                    Console.WriteLine($"\t{registration.RegistrationId} for {registration.DeviceId} is {registration.Status}.");

                    // ConnectionProfile is a read-only, response-only field populated by the service starting
                    // with the 2026-11-02-preview API version. It is extensible: known values include "classic"
                    // and "mqttV5", and unknown future values are tolerated as-is. A missing/null value
                    // semantically resolves to "classic".
                    Console.WriteLine($"\t\tConnection profile: {registration.ConnectionProfile ?? "classic (default)"}");

                    if (registration.ErrorCode.HasValue)
                    {
                        Console.WriteLine($"\t\tWith error ({registration.ErrorCode.Value}): {registration.ErrorMessage}");
                    }
                }
            }
        }

        private async Task DeleteEnrollmentGroupAsync()
        {
            Console.WriteLine("Deleting the enrollmentGroup...");
            await _provisioningServiceClient.DeleteEnrollmentGroupAsync(s_enrollmentGroupId);
            Console.WriteLine($"Enrollment group {s_enrollmentGroupId} deleted.");
        }
    }
}
