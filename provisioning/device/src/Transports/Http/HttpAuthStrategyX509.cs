// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Devices.Authentication;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    internal class HttpAuthStrategyX509 : HttpAuthStrategy
    {
        private AuthenticationProviderX509 _authentication;
        private X509Certificate2 _certificate;

        public HttpAuthStrategyX509(AuthenticationProviderX509 authentication)
        {
            _authentication = authentication;
        }

        public override DeviceProvisioningServiceRuntimeClient CreateClient(Uri uri, HttpClientHandler httpClientHandler)
        {
            _certificate = _authentication.GetAuthenticationCertificate();

            return new DeviceProvisioningServiceRuntimeClient(
                uri,
                new CertificateChainCredentials(new[] { _certificate }),
                httpClientHandler,
                new ApiVersionDelegatingHandler());
        }

        public override DeviceRegistration CreateDeviceRegistration()
        {
            Debug.Assert(_certificate != null);
            Debug.Assert(_authentication.GetRegistrationID() == _certificate.GetNameInfo(X509NameType.DnsName, false));

            return new DeviceRegistration(_authentication.GetRegistrationID());
        }

        public override void SaveCredentials(RegistrationOperationStatus status)
        {
            // no-op.
        }
    }
}
