// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using Microsoft.Azure.Devices.Provisioning.Client.Transport.Models;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    internal class HttpAuthStrategySymmetricKey : HttpAuthStrategy
    {
        private SecurityProviderSymmetricKey _security;

        public HttpAuthStrategySymmetricKey(SecurityProviderSymmetricKey security)
        {
            _security = security;
        }

        public override DeviceProvisioningServiceRuntimeClient CreateClient(Uri uri, HttpClientHandler httpClientHandler)
        {
            var serviceCredentials = new SymmetricKeyCredentials(_security.GetPrimaryKey());
            var dpsClient = new DeviceProvisioningServiceRuntimeClient(
                uri,
                serviceCredentials,
                httpClientHandler,
                new ApiVersionDelegatingHandler());

            dpsClient.HttpClient.Timeout = TimeoutConstant;

            return dpsClient;
        }

        public override DeviceRegistration CreateDeviceRegistration()
        {
            return new DeviceRegistration(_security.GetRegistrationID());
        }

        public override void SaveCredentials(RegistrationOperationStatus status)
        {

        }
    }
}