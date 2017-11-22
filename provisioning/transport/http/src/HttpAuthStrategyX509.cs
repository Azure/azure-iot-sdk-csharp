// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Client.Transport.Models;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    internal class HttpAuthStrategyX509 : HttpAuthStrategy
    {
        private SecurityClientHsmX509 _security;
        private X509Certificate2 _certificate;

        public HttpAuthStrategyX509(SecurityClientHsmX509 security)
        {
            _security = security;
        }

        public override DeviceProvisioningServiceRuntimeClient CreateClient(Uri uri)
        {
            _certificate = _security.GetAuthenticationCertificate();

            return new DeviceProvisioningServiceRuntimeClient(
                uri,
                new CertificateChainCredentials(new[] { _certificate }),
                new ApiVersionDelegatingHandler());
        }

        public override DeviceRegistration CreateDeviceRegistration()
        {
            Debug.Assert(_certificate != null);
            Debug.Assert(_security.GetRegistrationID() == _certificate.GetNameInfo(X509NameType.DnsName, false));

            return new DeviceRegistration(_security.GetRegistrationID());
        }

        public override void SaveCredentials(RegistrationOperationStatus status)
        {
            // no-op.
        }
    }
}
