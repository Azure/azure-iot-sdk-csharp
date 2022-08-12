// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using Microsoft.Azure.Devices.Authentication;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    internal class HttpAuthStrategyTpm : HttpAuthStrategy
    {
        private AuthenticationProviderTpm _authentication;

        public HttpAuthStrategyTpm(AuthenticationProviderTpm authentication)
        {
            _authentication = authentication;
        }

        public override DeviceProvisioningServiceRuntimeClient CreateClient(Uri uri, HttpClientHandler httpClientHandler)
        {
            var serviceCredentials = new TpmCredentials();
            var tpmDelegatingHandler = new TpmDelegatingHandler(_authentication);
            var apiVersionDelegatingHandler = new ApiVersionDelegatingHandler();

            var dpsClient = new DeviceProvisioningServiceRuntimeClient(
                uri,
                serviceCredentials,
                httpClientHandler,
                tpmDelegatingHandler,
                apiVersionDelegatingHandler);

            return dpsClient;
        }

        public override DeviceRegistrationHttp CreateDeviceRegistration()
        {
            byte[] ekBuffer = _authentication.GetEndorsementKey();
            byte[] srkBuffer = _authentication.GetStorageRootKey();

            string ek = Convert.ToBase64String(ekBuffer);
            string srk = Convert.ToBase64String(srkBuffer);

            return new DeviceRegistrationHttp(null, _authentication.GetRegistrationId(), new TpmAttestation(ek, srk));
        }

        public override void SaveCredentials(RegistrationOperationStatus operation)
        {
            if (operation?.RegistrationState?.Tpm?.AuthenticationKey == null)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Authentication key not found. OperationId=${operation?.OperationId}");

                throw new ProvisioningTransportException(
                    "Authentication key not found.",
                    null,
                    false);
            }

            byte[] key = Convert.FromBase64String(operation.RegistrationState.Tpm.AuthenticationKey);

            if (Logging.IsEnabled)
                Logging.DumpBuffer(this, key, nameof(operation.RegistrationState.Tpm.AuthenticationKey));

            _authentication.ActivateIdentityKey(key);
        }
    }
}
