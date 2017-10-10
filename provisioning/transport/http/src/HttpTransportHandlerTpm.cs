// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Client.Transport.Http.Models;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Http
{
    internal class HttpTransportHandlerTpm : HttpTransportHandler
    {
        private ProvisioningSecurityClientSasToken _security;

        public HttpTransportHandlerTpm(ProvisioningSecurityClientSasToken security)
        {
            _security = security;
        }

        public override ProvisioningRegistrationResult ConvertToProvisioningRegistrationResult(
            DeviceRegistrationResult result)
        {
            var status = ProvisioningRegistrationStatusType.Failed;
            Enum.TryParse(result.Status, true, out status);

            return new ProvisioningRegistrationResultTpm(
                result.RegistrationId,
                result.CreatedDateTimeUtc,
                result.AssignedHub,
                result.DeviceId,
                status,
                result.GenerationId,
                result.LastUpdatedDateTimeUtc,
                result.ErrorCode == null ? 0 : (int)result.ErrorCode,
                result.ErrorMessage,
                result.Etag,
                result.Tpm.AuthenticationKey);
        }

        public override Task<DeviceProvisioningServiceRuntimeClient> CreateClient(Uri uri)
        {
            var serviceCredentials = new TpmCredentials();
            var tpmDelegatingHandler = new TpmDelegatingHandler(_security);
            var apiVersionDelegatingHandler = new ApiVersionDelegatingHandler();

            var dpsClient = new DeviceProvisioningServiceRuntimeClient(
                uri,
                serviceCredentials,
                tpmDelegatingHandler,
                apiVersionDelegatingHandler);

            return Task.FromResult(dpsClient);
        }

        public override async Task<DeviceRegistration> CreateDeviceRegistration()
        {
            byte[] ekBuffer = await _security.GetEndorsementKeyAsync().ConfigureAwait(false);
            byte[] srkBuffer = await _security.GetStorageRootKeyAsync().ConfigureAwait(false);

            string ek = Convert.ToBase64String(ekBuffer);
            string srk = Convert.ToBase64String(srkBuffer);

            return new DeviceRegistration(_security.RegistrationID, new TpmAttestation(ek, srk));
        }
    }
}
