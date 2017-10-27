// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Client.Transport.Models;
using Microsoft.Azure.Devices.Shared;
using System;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    internal class HttpAuthStrategyTpm : HttpAuthStrategy
    {
        private SecurityClientHsmTpm _security;

        public HttpAuthStrategyTpm(SecurityClientHsmTpm security)
        {
            _security = security;
        }

        public override DeviceProvisioningServiceRuntimeClient CreateClient(Uri uri)
        {
            var serviceCredentials = new TpmCredentials();
            var tpmDelegatingHandler = new TpmDelegatingHandler(_security);
            var apiVersionDelegatingHandler = new ApiVersionDelegatingHandler();

            var dpsClient = new DeviceProvisioningServiceRuntimeClient(
                uri,
                serviceCredentials,
                tpmDelegatingHandler,
                apiVersionDelegatingHandler);

            return dpsClient;
        }

        public override DeviceRegistration CreateDeviceRegistration()
        {
            byte[] ekBuffer = _security.GetEndorsementKey();
            byte[] srkBuffer = _security.GetStorageRootKey();

            string ek = Convert.ToBase64String(ekBuffer);
            string srk = Convert.ToBase64String(srkBuffer);

            return new DeviceRegistration(_security.GetRegistrationID(), new TpmAttestation(ek, srk));
        }

        public override void SaveCredentials(RegistrationOperationStatus operation)
        {
            if (operation?.RegistrationStatus?.Tpm?.AuthenticationKey == null)
            {
                if (Logging.IsEnabled) Logging.Error(
                    this,
                    $"Authentication key not found. OperationId=${operation?.OperationId}");

                throw new ProvisioningTransportException(
                    "Authentication key not found.", 
                    false, 
                    operation?.OperationId, 
                    null);
            }

            byte[] key = Convert.FromBase64String(operation.RegistrationStatus.Tpm.AuthenticationKey);
            if (Logging.IsEnabled) Logging.DumpBuffer(this, key, nameof(operation.RegistrationStatus.Tpm.AuthenticationKey));

            _security.ActivateSymmetricIdentity(key);
        }
    }
}
