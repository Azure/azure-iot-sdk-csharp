// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Client.Transport.Http.Models;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Http
{
    internal class HttpTransportHandlerX509 : HttpTransportHandler
    {
        private ProvisioningSecurityClientX509Certificate _security;
        private X509Certificate2 _certificate;

        public HttpTransportHandlerX509(ProvisioningSecurityClientX509Certificate security)
        {
            _security = security;
        }

        public override ProvisioningRegistrationResult ConvertToProvisioningRegistrationResult(
            DeviceRegistrationResult result)
        {
            var status = ProvisioningRegistrationStatusType.Failed;
            Enum.TryParse(result.Status, true, out status);

            return new ProvisioningRegistrationResultX509Certificate(
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
                result.X509.EnrollmentGroupId);
        }

        public async override Task<DeviceProvisioningServiceRuntimeClient> CreateClient(Uri uri)
        {
            _certificate = await _security.GetAuthenticationCertificate().ConfigureAwait(false);

            return new DeviceProvisioningServiceRuntimeClient(
                uri,
                new CertificateChainCredentials(new[] { _certificate }),
                new ApiVersionDelegatingHandler());
        }

        public override Task<DeviceRegistration> CreateDeviceRegistration()
        {
            Debug.Assert(_certificate != null);
            Debug.Assert(_security.RegistrationID == _certificate.GetNameInfo(X509NameType.DnsName, false));

            return Task.FromResult(new DeviceRegistration(_security.RegistrationID));
        }
    }
}
